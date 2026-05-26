using RabbitMQ.Client;

namespace Micros.Core.rabbitmq;

public record RabbitMqConsumerSettings(
    string ExchangeType = ExchangeType.Topic,
    bool DurableExchange = true,
    bool DurableQueue = true,
    bool AutoDeleteExchange = false,
    bool AutoDeleteQueue = false,
    bool ExclusiveQueue = false,
    ushort PrefetchCount = 1,
    IDictionary<string, object?>? QueueArguments = null
);