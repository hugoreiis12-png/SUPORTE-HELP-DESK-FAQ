using Comprove.HelpDesk.Application.UseCases;
using Comprove.HelpDesk.Contracts.Events;
using Comprove.HelpDesk.Contracts.Integration;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Comprove.HelpDesk.Worker.Consumers;

// B17: consome mudanças de status; age somente em EmTriagem. Evita laço com os próprios eventos de dispatch.
public sealed class TicketClassifiedDispatchWorker(
    IServiceScopeFactory scopes,
    RabbitConnectionHolder connections,
    ILogger<TicketClassifiedDispatchWorker> logger) : RabbitQueueWorker(scopes, connections, logger)
{
    protected override string QueueName => RabbitTopology.DispatchQueue;

    protected override string ConsumerName => nameof(TicketClassifiedDispatchWorker);

    protected override Task ProcessAsync(IServiceProvider services, EventEnvelope envelope, CancellationToken ct)
    {
        var payload = EnvelopeReader.ReadPayload<TicketStatusChangedPayload>(envelope);
        if (!string.Equals(payload.ToStatus, nameof(TicketStatus.EmTriagem), StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        return services.GetRequiredService<DispatchTicketUseCase>()
            .ExecuteAsync(EnvelopeReader.SystemContext(envelope), payload.TicketId, ct);
    }
}
