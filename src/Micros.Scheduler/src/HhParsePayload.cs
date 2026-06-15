namespace Micros.Scheduler;

public record HhParsePayload(Guid SubscriptionId, string Url, Guid UserId, long? TelegramId);