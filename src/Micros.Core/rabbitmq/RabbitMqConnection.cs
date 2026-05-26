using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Micros.Core.rabbitmq;

public class RabbitMqConnection : IAsyncDisposable
{
    private IConnection? _connection;
    private readonly ConnectionFactory _factory;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options)
    {
        _factory = new ConnectionFactory
        {
            HostName = options.Value.Hostname,
            Port = options.Value.Port,
            UserName = options.Value.Username,
            Password = options.Value.Password,
        };
    }

    public async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken)
    {
        _connection ??= await _factory.CreateConnectionAsync(cancellationToken: cancellationToken);
        return await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.CloseAsync();
    }
}