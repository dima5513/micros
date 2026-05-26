namespace Micros.Core.messages;

public record HHSubscriptionCreateMessage(
    Guid SubscriptionId,
    string Url,
    Guid UserId,
    long? TelegramId
);

public record HHSubscriptionDeleteMessage(
    Guid SubscriptionId
);