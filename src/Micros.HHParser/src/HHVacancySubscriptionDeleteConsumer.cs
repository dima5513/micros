using System.Text.Json;
using Micros.Core.messages;
using Micros.Core.rabbitmq;
using Micros.Core.redis;

namespace Micros.HHParser;

public class HHVacancySubscriptionDeleteConsumer: IRabbitMqConsumer
{
    private readonly RedisConnection _redis;

    public string Exchange => HHSubscriptionTopology.Exchange;
    public string QueueName => "hh-parser.subscription.delete";
    public string RoutingKey => HHSubscriptionTopology.DeleteSubscriptionKey;

    public RabbitMqConsumerSettings Settings => new();

    public HHVacancySubscriptionDeleteConsumer(RedisConnection redis)
    {
        _redis = redis;
    }

    public async Task HandleAsync(string body, CancellationToken ct)
    {
        var message = JsonSerializer.Deserialize<HHSubscriptionDeleteMessage>(body) ??
                      throw new InvalidOperationException("Failed to deserialize delete subscription message");

        var redisDb = await _redis.GetDatabaseAsync();

        await redisDb.HashDeleteAsync(
            "hh:tasks",
            message.SubscriptionId.ToString()
        );

        await redisDb.KeyDeleteAsync($"hh:watermark:{message.SubscriptionId}");
    }
}