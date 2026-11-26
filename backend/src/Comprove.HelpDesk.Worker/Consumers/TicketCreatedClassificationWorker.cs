using Comprove.HelpDesk.Application.UseCases;
using Comprove.HelpDesk.Contracts.Events;
using Comprove.HelpDesk.Contracts.Integration;
using Comprove.HelpDesk.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Comprove.HelpDesk.Worker.Consumers;

// B16: helpdesk.ticket.created → classificação determinística.
public sealed class TicketCreatedClassificationWorker(
    IServiceScopeFactory scopes,
    RabbitConnectionHolder connections,
    ILogger<TicketCreatedClassificationWorker> logger) : RabbitQueueWorker(scopes, connections, logger)
{
    protected override string QueueName => RabbitTopology.ClassificationQueue;

    protected override string ConsumerName => nameof(TicketCreatedClassificationWorker);

    protected override Task ProcessAsync(IServiceProvider services, EventEnvelope envelope, CancellationToken ct)
    {
        var payload = EnvelopeReader.ReadPayload<TicketCreatedPayload>(envelope);
        return services.GetRequiredService<ClassifyTicketUseCase>()
            .ExecuteAsync(EnvelopeReader.SystemContext(envelope), payload.TicketId, ct);
    }
}
