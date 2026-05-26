using Micros.Core.Types;

namespace Micros.Api.Domains.User;

public record UpdateMeContract(Guid UserId, Patch<long?> TelegramId);
