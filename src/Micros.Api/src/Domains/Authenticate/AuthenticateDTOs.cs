using System.ComponentModel.DataAnnotations;

namespace Micros.Api.Domains.Authenticate;

public record RegisterRequestDTO(
    string Username,
    [EmailAddress] string Email,
    string Password);

public record LoginRequestDTO(
    [EmailAddress] string Email,
    string Password);

public record RegisterResponseDTO(
    Guid Id,
    string Username,
    string Email,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);