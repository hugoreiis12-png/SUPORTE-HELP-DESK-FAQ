namespace Comprove.HelpDesk.Application.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken ct);
}

