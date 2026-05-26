namespace Micros.Api.Infrastructure.Jwt;

public class JwtTokenPayload
{
    public Guid Id { get; set; }
    public string Email { get; set; }
}