using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Micros.Api.Infrastructure.Jwt;
using Microsoft.Extensions.Options;

namespace Micros.Api.Tests;

public class JwtServiceTests
{
    private readonly JwtOptions _jwtOptions = new JwtOptions
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        JwtAccessSecurityKey = "test-access-key-must-be-at-least-32-chars-long!!",
        JwtAccessExpireHours = 1,
        JwtRefreshSecurityKey = "test-refresh-key-must-be-at-least-32-chars-long!",
        JwtRefreshExpireHours = 24
    };

    [Fact]
    public void ValidateAccessToken_WithValidToken_ReturnsClaimsPrincipal()
    {
        var userEmail = $"tester{Guid.NewGuid()}@example.com";
        var userId = Guid.NewGuid();
        JwtTokenPayload tokenPayload = new JwtTokenPayload
        {
            Email = userEmail,
            Id = userId
        };

        var sut = CreateSut();

        var token = sut.GenerateAccessToken(tokenPayload);

        var principal = sut.ValidateAccessToken(token);

        Assert.NotNull(principal);
        Assert.Equal(userEmail, principal.FindFirstValue(JwtRegisteredClaimNames.Email));
        Assert.Equal(userId.ToString(), principal.FindFirstValue(JwtRegisteredClaimNames.Sub));
    }

    [Fact]
    public void ValidateAccessToken_WithIncorrectToken_ReturnsNull()
    {
        var sut = CreateSut();

        var principal = sut.ValidateAccessToken("abcde");

        Assert.Null(principal);
    }
    
    [Fact]
    public void ValidateAccessToken_WithRefreshToken_ReturnsNull()
    {
        var payload = new JwtTokenPayload { Email = "a@b.c", Id = Guid.NewGuid() };
        var sut = CreateSut();

        var refreshToken = sut.GenerateRefreshToken(payload);
        var principal = sut.ValidateAccessToken(refreshToken);

        Assert.Null(principal);
    }

    private JwtService CreateSut() => new JwtService(Options.Create(_jwtOptions));
}