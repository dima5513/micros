using System.Text.Json;
using Micros.Core.messages;
using Micros.Core.rabbitmq;
using Micros.Core.redis;

namespace Micros.HHParser;

public class HHVacancySubscriptionCreateConsumer : IRabbitMqConsumer
{
    private readonly RedisConnection _redis;

    public string Exchange => HHSubscriptionTopology.Exchange;
    public string QueueName => "hh-parser.subscription.create";
    public string RoutingKey => HHSubscriptionTopology.CreateSubscriptionKey;

    public RabbitMqConsumerSettings Settings => new();

    public HHVacancySubscriptionCreateConsumer(RedisConnection redis)
    {
        _redis = redis;
    }

    public async Task HandleAsync(string body, CancellationToken ct)
    {
        var message = JsonSerializer.Deserialize<HHSubscriptionCreateMessage>(body) ??
                      throw new InvalidOperationException("Failed to deserialize create subscription message");

        var redisDb = await _redis.GetDatabaseAsync();

        await redisDb.HashSetAsync(
            "hh:tasks",
            message.SubscriptionId.ToString(),
            JsonSerializer.Serialize(new SubscriptionCacheEntry(message.Url, message.UserId, message.TelegramId))
        );
    }
}