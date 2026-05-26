using System.Text.Json.Serialization;

namespace Micros.HHParser;

public record Subscription(
    [property:JsonPropertyName("id")] Guid Id,
    [property:JsonPropertyName("userId")] Guid UserId,
    [property:JsonPropertyName("telegramId")] long? TelegramId,
    [property:JsonPropertyName("url")] string Url,
    [property:JsonPropertyName("createdAt")] DateTimeOffset CreatedAt
);

public record SubscriptionCacheEntry(string Url, Guid UserId, long? TelegramId);