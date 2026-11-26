using Comprove.HelpDesk.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Comprove.HelpDesk.Worker.Outbox;

// Publicador do outbox (ADR-004). Polling com backoff exponencial. Duplicidade é absorvida pelo inbox.
public sealed class OutboxRelayService(
    IServiceScopeFactory scopes,
    ILogger<OutboxRelayService> logger) : BackgroundService
{
    private const int BatchSize = 50;
    private const int MaxBackoffSeconds = 300;
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan FailureDelay = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pause = await RelayOrPauseAsync(stoppingToken);
            await Task.Delay(pause, stoppingToken);
        }
    }

    // Banco fora do ar não derruba o Worker: registra e tenta de novo.
    private async Task<TimeSpan> RelayOrPauseAsync(CancellationToken ct)
    {
        try
        {
            return await RelayBatchAsync(ct) == 0 ? IdleDelay : TimeSpan.Zero;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Falha ao processar o outbox; nova tentativa em {Delay}.", FailureDelay);
            return FailureDelay;
        }
    }

    private async Task<int> RelayBatchAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var outbox = services.GetRequiredService<IOutboxRepository>();
        var clock = services.GetRequiredService<IClock>();
        var publisher = services.GetRequiredService<IEventPublisher>();

        var batch = await outbox.LoadPendingAsync(BatchSize, clock.UtcNow, ct);
        foreach (var record in batch)
        {
            await PublishOneAsync(outbox, publisher, clock, record, ct);
        }

        return batch.Count;
    }

    private async Task PublishOneAsync(
        IOutboxRepository outbox,
        IEventPublisher publisher,
        IClock clock,
        OutboxRecord record,
        CancellationToken ct)
    {
        try
        {
            await publisher.PublishAsync(record.Message, ct);
            await outbox.MarkPublishedAsync(record.EventId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var attempts = record.Attempts + 1;
            var backoff = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempts), MaxBackoffSeconds));
            logger.LogWarning(ex, "Falha ao publicar evento {EventId}; tentativa {Attempt}.", record.EventId, attempts);
            await outbox.MarkFailedAsync(record.EventId, attempts, clock.UtcNow + backoff, ex.Message, ct);
        }
    }
}
