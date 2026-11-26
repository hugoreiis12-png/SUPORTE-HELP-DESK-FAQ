using Comprove.HelpDesk.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Comprove.HelpDesk.Infrastructure.Messaging.RabbitMq;

// Uma conexão compartilhada, criada sob demanda. A biblioteca reconecta; falha na criação não fica guardada.
public sealed class RabbitConnectionHolder(IOptions<RabbitMqOptions> options)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetAsync()
    {
        await _gate.WaitAsync();
        try
        {
            return _connection ??= await Create(options.Value);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static Task<IConnection> Create(RabbitMqOptions settings) =>
        new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            VirtualHost = settings.VirtualHost,
            UserName = settings.UserName,
            Password = settings.Password,
            AutomaticRecoveryEnabled = true,
        }.CreateConnectionAsync();
}
