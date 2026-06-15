namespace Micros.Core.messages;

public record HhParseRequested(Guid SubscriptionId, string Url, Guid UserId, long? TelegramId);