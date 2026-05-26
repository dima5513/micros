namespace Micros.Core.rabbitmq;

public interface IRabbitMqConsumer
{
    string Exchange { get; }
    string QueueName { get; }
    string RoutingKey { get; }

    RabbitMqConsumerSettings Settings => new();

    Task HandleAsync(string message, CancellationToken ct);
}