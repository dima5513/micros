using Micros.Api.Domains.Authenticate;
using Micros.Api.Domains.User;
using Micros.Api.Infrastructure.Database;
using Micros.Api.Infrastructure.Jwt;
using Micros.Api.Infrastructure.PasswordHash;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Micros.Api.Tests;

public class AuthenticateServiceTests : IClassFixture<PostgresFixture>
{
    private readonly PasswordHashService _passwordHashService = new();

    private readonly PostgresFixture _fixture;

    public AuthenticateServiceTests(PostgresFixture postgresFixture)
    {
        _fixture = postgresFixture;
    }

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
    public async Task Register_NewEmail_PersistsUser()
    {
        await using var db = _fixture.CreateDbContext();
        var sut = CreateSut(db);

        var userEmail = $"tester-{Guid.NewGuid()}@example.com";
        var userPassword = "hashed_password";
        var userUsername = $"tester-{Guid.NewGuid()}";

        await sut.Register(
            new RegisterContract(
                userUsername,
                userEmail,
                userPassword
            )
        );

        await using var verifyDb = _fixture.CreateDbContext();
        var user = await verifyDb.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

        Assert.NotNull(user);
        Assert.True(_passwordHashService.Verify(userPassword, user.HashPassword));
        Assert.Equal(userUsername, user.Username);
        Assert.Equal(userEmail, user.Email);
    }

    [Fact]
    public async Task Register_EmailExists_ThrowsEmailAlreadyExists()
    {
        var userEmail = $"tester-{Guid.NewGuid()}@example.com";

        await using (var seedDb = _fixture.CreateDbContext())
        {
            seedDb.Users.Add(new UserEntity
            {
                Email = userEmail,
                HashPassword = "hashed_password",
                Username = $"tester-{Guid.NewGuid()}"
            });
            await seedDb.SaveChangesAsync();
        }

        await using var db = _fixture.CreateDbContext();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() =>
            sut.Register(new RegisterContract(
                $"tester-{Guid.NewGuid()}",
                userEmail,
                "hashed_password")
            )
        );

        await using var verifyDb = _fixture.CreateDbContext();
        var count = await verifyDb.Users.CountAsync(u => u.Email == userEmail);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Login_UserExists_ReturningPairTokens()
    {
        var userUsername = $"tester-{Guid.NewGuid()}";
        var userEmail = $"tester-{Guid.NewGuid()}@example.com";
        var userPassword = "password";

        await using (var seedDb = _fixture.CreateDbContext())
        {
            var user = new UserEntity
            {
                Username = userUsername,
                Email = userEmail,
                HashPassword = _passwordHashService.Hash(userPassword),
            };

            seedDb.Users.Add(user);
            await seedDb.SaveChangesAsync();
        }


        await using var db = _fixture.CreateDbContext();
        var sut = CreateSut(db);

        var tokensPair = await sut.Login(new LoginContract(userEmail, userPassword));

        Assert.NotNull(tokensPair);
        Assert.False(string.IsNullOrWhiteSpace(tokensPair.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokensPair.RefreshToken));
        Assert.NotEqual(tokensPair.AccessToken, tokensPair.RefreshToken);
    }

    [Fact]
    public async Task Login_UserNotExists_ThrowsUserNotFoundException()
    {
        await using var db = _fixture.CreateDbContext();

        var sut = CreateSut(db);
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            sut.Login(new LoginContract($"tester-{Guid.NewGuid()}@example.com", "password"))
        );
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsInvalidCredentialsException()
    {
        var userEmail = $"tester-{Guid.NewGuid()}@example.com";
        var correctPassword = "correct_password";

        await using (var seedDb = _fixture.CreateDbContext())
        {
            var user = new UserEntity
            {
                Username = "tester",
                Email = userEmail,
                HashPassword = _passwordHashService.Hash(correctPassword)
            };
            seedDb.Users.Add(user);
            await seedDb.SaveChangesAsync();
        }

        await using var db = _fixture.CreateDbContext();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            sut.Login(new LoginContract(userEmail, "wrong_password"))
        );
    }

    private AuthenticateService CreateSut(AppDbContext db) =>
        new AuthenticateService(db, _passwordHashService, new JwtService(Options.Create(_jwtOptions)));
}