namespace Micros.Core.messages;

public record HHUserUpdatedMessage(Guid UserId, long? TelegramId);