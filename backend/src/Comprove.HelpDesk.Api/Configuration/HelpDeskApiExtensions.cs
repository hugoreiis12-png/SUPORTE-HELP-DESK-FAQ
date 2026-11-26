using Comprove.HelpDesk.Api.Auth;
using Comprove.HelpDesk.Api.Health;
using Comprove.HelpDesk.Api.SignalR;
using Comprove.HelpDesk.Api.Validation;
using Comprove.HelpDesk.Application.Abstractions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Comprove.HelpDesk.Api.Configuration;

public static class HelpDeskApiExtensions
{
    // Chamar DEPOIS de AddHelpDeskInfrastructure: o notificador SignalR substitui o no-op.
    public static IServiceCollection AddHelpDeskApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddSignalR();
        services.AddValidatorsFromAssemblyContaining<OpenTicketRequestValidator>();
        services.AddBubbleAuthentication(configuration);
        AddFrontCors(services, configuration);
        services.AddHealthChecks()
            .AddCheck<MongoHealthCheck>("mongodb")
            .AddCheck<RabbitHealthCheck>("rabbitmq");
        services.AddSingleton<ITicketNotifier, SignalRTicketNotifier>();
        return services;
    }

    // O JWT vai no header Authorization (sem cookie), então não há credenciais no CORS.
    private static void AddFrontCors(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(HelpDeskCorsOptions.Section);
        services.AddOptions<HelpDeskCorsOptions>().Bind(section).ValidateDataAnnotations().ValidateOnStart();
        var origins = section.Get<HelpDeskCorsOptions>()?.AllowedOrigins ?? [];
        services.AddCors(options => options.AddPolicy(
            HelpDeskCorsOptions.PolicyName,
            policy => policy.WithOrigins(origins).WithMethods("GET", "POST", "PUT", "PATCH").WithHeaders("Authorization", "Content-Type")));
    }
}
