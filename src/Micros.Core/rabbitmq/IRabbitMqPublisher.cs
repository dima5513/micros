namespace Micros.Core.rabbitmq;

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken);

    Task PublishRawAsync(string exchange, string routingKey, byte[] body, CancellationToken cancellationToken);
}