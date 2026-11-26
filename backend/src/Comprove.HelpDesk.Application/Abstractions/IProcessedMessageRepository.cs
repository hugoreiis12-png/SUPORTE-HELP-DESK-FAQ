namespace Comprove.HelpDesk.Application.Abstractions;

public interface IProcessedMessageRepository
{
    // Inbox: true se este consumidor ainda não processou o event_id. Deve rodar dentro da transação do efeito.
    Task<bool> TryClaimAsync(string eventId, string consumer, DateTimeOffset processedAt, CancellationToken ct);
}
