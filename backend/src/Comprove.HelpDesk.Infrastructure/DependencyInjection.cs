using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Services;
using Comprove.HelpDesk.Infrastructure.Cache;
using Comprove.HelpDesk.Infrastructure.Configuration;
using Comprove.HelpDesk.Infrastructure.Integration.Comprove;
using Comprove.HelpDesk.Infrastructure.Integration.OpenWa;
using Comprove.HelpDesk.Infrastructure.Messaging.RabbitMq;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo;
using Comprove.HelpDesk.Infrastructure.Services;
using Comprove.HelpDesk.Infrastructure.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Comprove.HelpDesk.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHelpDeskInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddValidatedOptions(services, configuration);
        AddMongo(services, configuration);
        AddRepositories(services);
        AddMessaging(services);
        AddIntegrations(services);
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ITicketNotifier, NoOpTicketNotifier>();
        services.AddSingleton<IObjectStorage, GridFsObjectStorage>();
        services.AddSingleton<AttachmentLinkSigner>();
        services.AddSingleton(sp => ToAttachmentLimits(sp.GetRequiredService<IOptions<StorageOptions>>().Value));
        return services;
    }

    private static void AddValidatedOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MongoOptions>().Bind(configuration.GetSection(MongoOptions.Section)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<RabbitMqOptions>().Bind(configuration.GetSection(RabbitMqOptions.Section)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<SlaOptions>().Bind(configuration.GetSection(SlaOptions.Section)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<StorageOptions>().Bind(configuration.GetSection(StorageOptions.Section)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<ComproveOptions>().Bind(configuration.GetSection(ComproveOptions.Section)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<OpenWaOptions>().Bind(configuration.GetSection(OpenWaOptions.Section)).ValidateDataAnnotations().ValidateOnStart();
    }

    private static void AddMongo(IServiceCollection services, IConfiguration configuration)
    {
        MongoSerialization.EnsureConfigured();
        var connectionString = configuration.GetConnectionString("HelpDeskMongo")
            ?? throw new InvalidOperationException("ConnectionStrings:HelpDeskMongo ausente.");
        services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
        services.AddSingleton<MongoContext>();
        services.AddScoped<MongoSessionContext>();
        services.AddScoped<ITransactionRunner, MongoTransactionRunner>();
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ITicketHistoryRepository, TicketHistoryRepository>();
        services.AddScoped<ITicketMessageRepository, TicketMessageRepository>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<IFaqRepository, FaqRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();
        services.AddScoped<IProtocolSequence, ProtocolSequenceRepository>();
        services.AddScoped<ITeamRoster, TeamRoster>();
        services.AddMemoryCache();
        services.AddScoped<MongoSlaPolicyProvider>();
        services.AddScoped<ISlaPolicyProvider>(sp => new CachedSlaPolicyProvider(
            sp.GetRequiredService<MongoSlaPolicyProvider>(),
            sp.GetRequiredService<IMemoryCache>(),
            sp.GetRequiredService<IOptions<SlaOptions>>().Value.RulesVersion));
    }

    private static void AddMessaging(IServiceCollection services)
    {
        services.AddSingleton<RabbitConnectionHolder>();
        services.AddSingleton<IEventPublisher, RabbitEventPublisher>();
    }

    private static void AddIntegrations(IServiceCollection services)
    {
        services.AddHttpClient<IComproveIntegration, ComproveHttpIntegration>((sp, client) =>
        {
            var baseUrl = sp.GetRequiredService<IOptions<ComproveOptions>>().Value.BaseUrl;
            client.BaseAddress = string.IsNullOrWhiteSpace(baseUrl) ? null : new Uri(baseUrl);
        }).AddStandardResilienceHandler();
        AddWhatsApp(services);
    }

    // Sem repetição automática do HttpClient: quem repete é o Worker, que distingue falha definitiva.
    private static void AddWhatsApp(IServiceCollection services) =>
        services.AddHttpClient<IWhatsAppGateway, OpenWaGateway>((sp, client) =>
        {
            var baseUrl = sp.GetRequiredService<IOptions<OpenWaOptions>>().Value.BaseUrl;
            client.BaseAddress = string.IsNullOrWhiteSpace(baseUrl) ? null : new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(20);
        }).RemoveAllLoggers();

    private static AttachmentLimits ToAttachmentLimits(StorageOptions storage) =>
        new(storage.MaxAttachmentBytes, TimeSpan.FromMinutes(storage.UploadUrlTtlMinutes));
}
