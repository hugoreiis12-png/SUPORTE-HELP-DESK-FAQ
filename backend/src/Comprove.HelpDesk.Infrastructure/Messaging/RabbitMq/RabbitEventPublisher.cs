using System.Text.Json;
using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Contracts.Integration;
using RabbitMQ.Client;

namespace Comprove.HelpDesk.Infrastructure.Messaging.RabbitMq;

// Publica no exchange helpdesk.topic com publisher confirms: falha de confirmação vira erro para o relay, que reagenda.
public sealed class RabbitEventPublisher(RabbitConnectionHolder connections) : IEventPublisher, IAsyncDisposable
{
    private readonly SemaphoreSlim _channelGate = new(1, 1);
    private IChannel? _channel;

    public async Task PublishAsync(OutboxMessage message, CancellationToken ct)
    {
        var channel = await GetChannelAsync(ct);
        var envelope = message.Envelope;
        var properties = new BasicProperties
        {
            Persistent = true,
            MessageId = envelope.EventId,
            Type = envelope.EventType,
            CorrelationId = envelope.CorrelationId,
            ContentType = "application/json",
        };
        var body = JsonSerializer.SerializeToUtf8Bytes(envelope);
        await channel.BasicPublishAsync(
            RabbitTopology.TopicExchange,
            message.RoutingKey,
            mandatory: RabbitTopology.HasConsumer(message.RoutingKey),
            properties,
            body,
            ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        await _channelGate.WaitAsync(ct);
        try
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            var connection = await connections.GetAsync();
            var options = new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true);
            _channel = await connection.CreateChannelAsync(options, ct);
            return _channel;
        }
        finally
        {
            _channelGate.Release();
        }
    }
}
