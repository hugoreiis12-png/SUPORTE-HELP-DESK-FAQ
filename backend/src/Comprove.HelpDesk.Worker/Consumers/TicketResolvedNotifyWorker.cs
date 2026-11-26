using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.UseCases;
using Comprove.HelpDesk.Contracts.Events;
using Comprove.HelpDesk.Contracts.Integration;
using Comprove.HelpDesk.Infrastructure.Configuration;
using Comprove.HelpDesk.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Comprove.HelpDesk.Worker.Consumers;

// B18: reações à resolução: sincronização do FAQ (desligada até haver endpoint) e aviso ao solicitante.
public sealed class TicketResolvedNotifyWorker(
    IServiceScopeFactory scopes,
    RabbitConnectionHolder connections,
    ILogger<TicketResolvedNotifyWorker> logger) : RabbitQueueWorker(scopes, connections, logger)
{
    private readonly ILogger<TicketResolvedNotifyWorker> _logger = logger;
    private const string FaqSyncKind = "faq/sync";

    protected override string QueueName => RabbitTopology.ResolvedNotifyQueue;

    protected override string ConsumerName => nameof(TicketResolvedNotifyWorker);

    // O envio fica fora da transação do inbox: repetir uma transação reenviaria a mensagem.
    protected override async Task ProcessOnceAsync(IServiceProvider services, EventEnvelope envelope, CancellationToken ct)
    {
        await base.ProcessOnceAsync(services, envelope, ct);
        await NotifyRequesterAsync(services, envelope, ct);
    }

    private async Task NotifyRequesterAsync(IServiceProvider services, EventEnvelope envelope, CancellationToken ct)
    {
        if (!services.GetRequiredService<IOptions<OpenWaOptions>>().Value.Enabled)
        {
            return;
        }

        var payload = EnvelopeReader.ReadPayload<TicketResolvedPayload>(envelope);
        var outcome = await services.GetRequiredService<NotifyResolutionUseCase>()
            .ExecuteAsync(EnvelopeReader.SystemContext(envelope), payload.TicketId, envelope.EventId.ToString(), ct);
        _logger.LogInformation("Aviso de resolução do chamado {TicketId}: {Outcome}.", payload.TicketId, outcome);
    }

    protected override Task ProcessAsync(IServiceProvider services, EventEnvelope envelope, CancellationToken ct)
    {
        if (!services.GetRequiredService<IOptions<ComproveOptions>>().Value.FaqSyncEnabled)
        {
            return Task.CompletedTask;
        }

        var payload = EnvelopeReader.ReadPayload<TicketResolvedPayload>(envelope);
        var query = new ComproveQuery(FaqSyncKind, payload.TicketId, envelope.TenantId, envelope.CorrelationId);
        return services.GetRequiredService<IComproveIntegration>().QueryAsync(query, ct);
    }
}
