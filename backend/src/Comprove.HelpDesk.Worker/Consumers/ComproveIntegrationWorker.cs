using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Contracts.Events;
using Comprove.HelpDesk.Contracts.Integration;
using Comprove.HelpDesk.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Comprove.HelpDesk.Worker.Consumers;

// B19: consultas ao Comprove via ACL. Falha não altera o estado do ticket (vai para DLQ).
public sealed class ComproveIntegrationWorker(
    IServiceScopeFactory scopes,
    RabbitConnectionHolder connections,
    ILogger<ComproveIntegrationWorker> logger) : RabbitQueueWorker(scopes, connections, logger)
{
    protected override string QueueName => RabbitTopology.IntegrationQueue;

    protected override string ConsumerName => nameof(ComproveIntegrationWorker);

    protected override Task ProcessAsync(IServiceProvider services, EventEnvelope envelope, CancellationToken ct)
    {
        var payload = EnvelopeReader.ReadPayload<IntegrationQueryPayload>(envelope);
        var query = new ComproveQuery(payload.QueryKind, payload.TicketId, envelope.TenantId, envelope.CorrelationId);
        return services.GetRequiredService<IComproveIntegration>().QueryAsync(query, ct);
    }
}
