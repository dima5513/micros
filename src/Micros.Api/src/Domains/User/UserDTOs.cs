using System.Text.Json.Serialization;
using Micros.Core.Types;

namespace Micros.Api.Domains.User;

public record UserResponseDTO(
    Guid Id,
    string Username,
    string Email,
    long? TelegramId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record UpdateMeRequestDTO(
    [property: JsonPropertyName("telegramId")] Patch<long?> TelegramId
);
