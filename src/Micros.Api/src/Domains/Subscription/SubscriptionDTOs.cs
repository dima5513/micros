using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Micros.Api.Domains.Subscription;

public record CreateSubscriptionDTO(
    [property: JsonPropertyName("url")] [Url] string Url
);

public record SubscriptionResponseDTO(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt
);


public record SubscriptionWithTelegramIdResponseDTO(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("telegramId")] long? TelegramId,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt
): SubscriptionResponseDTO(Id, UserId, Url, CreatedAt);