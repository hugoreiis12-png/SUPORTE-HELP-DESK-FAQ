namespace Comprove.HelpDesk.Application.Abstractions;

public sealed record WhatsAppMessage(string ChatId, string Text, string IdempotencyKey);

// Porta de saída para WhatsApp. Falha definitiva lança WhatsAppRejectedException; as demais podem ser repetidas.
public interface IWhatsAppGateway
{
    // Devolve o identificador do contato no WhatsApp, ou null quando o número não tem conta.
    Task<string?> FindChatIdAsync(string phone, CancellationToken ct);

    Task SendTextAsync(WhatsAppMessage message, CancellationToken ct);
}
