using System.Text.Json;
using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Domain.Exceptions;

namespace Comprove.HelpDesk.Worker.Consumers;

// B15: falha passageira é repetida com backoff; falha de regra ou de formato vai direto para a DLQ.
public static class MessageRetryPolicy
{
    public const int MaxAttempts = 6;
    private const int MaxDelaySeconds = 30;

    public static bool ShouldRetry(Exception ex, int attempt) => attempt < MaxAttempts && !IsPermanent(ex);

    public static TimeSpan DelayAfter(int attempt) =>
        TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt - 1), MaxDelaySeconds));

    private static bool IsPermanent(Exception ex) =>
        ex is DomainRuleViolationException or TicketNotFoundException or WhatsAppRejectedException
            or JsonException or InvalidDataException;
}
