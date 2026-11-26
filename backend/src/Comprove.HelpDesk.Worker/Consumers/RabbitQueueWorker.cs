using System.Text.Json;
using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Contracts.Events;
using Comprove.HelpDesk.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Comprove.HelpDesk.Worker.Consumers;

// Base dos consumidores. Inbox (B15): claim + efeito na mesma transação. Ack só após commit; falha passageira repete, o resto vai para a DLQ.
public abstract class RabbitQueueWorker(
    IServiceScopeFactory scopes,
    RabbitConnectionHolder connections,
    ILogger logger) : BackgroundService
{
    private const int Prefetch = 10;

    protected abstract string QueueName { get; }

    protected abstract string ConsumerName { get; }

    protected abstract Task ProcessAsync(IServiceProvider services, EventEnvelope envelope, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await connections.GetAsync();
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.BasicQosAsync(0, Prefetch, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, args) => OnMessageAsync(channel, args, stoppingToken);
        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    protected virtual async Task ProcessOnceAsync(IServiceProvider services, EventEnvelope envelope, CancellationToken ct)
    {
        var transactions = services.GetRequiredService<ITransactionRunner>();
        var processed = services.GetRequiredService<IProcessedMessageRepository>();
        var clock = services.GetRequiredService<IClock>();

        await transactions.RunAsync(async token =>
        {
            if (!await processed.TryClaimAsync(envelope.EventId, ConsumerName, clock.UtcNow, token))
            {
                return;
            }

            await ProcessAsync(services, envelope, token);
        }, ct);
    }

    private async Task OnMessageAsync(IChannel channel, BasicDeliverEventArgs args, CancellationToken ct)
    {
        if (await TryHandleAsync(args.Body.ToArray(), ct))
        {
            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: ct);
            return;
        }

        await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
    }

    // Não loga o corpo da mensagem: pode conter dados do chamado.
    private async Task<bool> TryHandleAsync(byte[] body, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await HandleAsync(body, ct);
                return true;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested && MessageRetryPolicy.ShouldRetry(ex, attempt))
            {
                logger.LogWarning(ex, "Falha em {Queue}; tentativa {Attempt} será repetida.", QueueName, attempt);
                await Task.Delay(MessageRetryPolicy.DelayAfter(attempt), ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogError(ex, "Falha em {Queue}; mensagem enviada para DLQ.", QueueName);
                return false;
            }
        }
    }

    private async Task HandleAsync(byte[] body, CancellationToken ct)
    {
        var envelope = JsonSerializer.Deserialize<EventEnvelope>(body) ?? throw new InvalidDataException("Envelope vazio.");
        await using var scope = scopes.CreateAsyncScope();
        await ProcessOnceAsync(scope.ServiceProvider, envelope, ct);
    }
}
