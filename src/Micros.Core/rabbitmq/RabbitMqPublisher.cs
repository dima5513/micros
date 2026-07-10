using System.Text.Encodings.Web;
using System.Text.Json;
using Micros.Core.logging;
using RabbitMQ.Client;

namespace Micros.Core.rabbitmq;

public class RabbitMqPublisher(RabbitMqConnection connection) : IRabbitMqPublisher, IAsyncDisposable
{
    private IChannel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
        return PublishRawAsync(exchange, routingKey, body, cancellationToken);
    }

    public async Task PublishRawAsync(
        string exchange,
        string routingKey,
        byte[] body,
        CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _channel ??= await connection.CreateChannelAsync(cancellationToken);

            await _channel.ExchangeDeclareAsync(
                exchange: exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            var props = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json"
            };

            var correlationId = CorrelationIdContext.Current;

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                props.Headers = new Dictionary<string, object?>
                {
                    [CorrelationId.HeaderName] = correlationId
                };
            }

            await _channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: props,
                mandatory: false,
                body: body,
                cancellationToken: cancellationToken
            );
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.CloseAsync();
        _lock.Dispose();
    }
}