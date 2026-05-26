using System.Text.Json;
using Micros.Core.messages;
using Micros.Core.rabbitmq;
using Micros.Core.redis;
using StackExchange.Redis;

namespace Micros.HHParser;

public class HHUserUpdatedConsumer : IRabbitMqConsumer
{
    private readonly RedisConnection _redis;

    public HHUserUpdatedConsumer(RedisConnection redis)
    {
        _redis = redis;
    }

    public string Exchange => HHUserTopology.Exchange;
    public string QueueName => "hh-parser.user.updated";
    public string RoutingKey => HHUserTopology.UpdateUserKey;

    public RabbitMqConsumerSettings Settings => new();

    public async Task HandleAsync(string body, CancellationToken ct)
    {
        var message = JsonSerializer.Deserialize<HHUserUpdatedMessage>(body)
                      ?? throw new InvalidOperationException("failed to deserialize payload");

        var db = await _redis.GetDatabaseAsync();

        var taskEntries = await db.HashGetAllAsync("hh:tasks");

        var updated = taskEntries
            .Select(e => new
            {
                Name = e.Name,
                Cached = JsonSerializer.Deserialize<SubscriptionCacheEntry>((string)e.Value!)
                         ?? throw new InvalidOperationException($"corrupt cache entry for {e.Name}")
            })
            .Where(x => x.Cached.UserId == message.UserId)
            .Select(x => new HashEntry(
                x.Name,
                JsonSerializer.Serialize(x.Cached with { TelegramId = message.TelegramId })
            ))
            .ToArray();

        if (updated.Length == 0) return;

        await db.HashSetAsync("hh:tasks", updated);
    }
}
