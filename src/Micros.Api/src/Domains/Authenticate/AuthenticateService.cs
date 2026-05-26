using Micros.Api.Domains.User;
using Micros.Api.Infrastructure.Database;
using Micros.Api.Infrastructure.Jwt;
using Micros.Api.Infrastructure.PasswordHash;
using Microsoft.EntityFrameworkCore;

namespace Micros.Api.Domains.Authenticate;

public class AuthenticateService(
    AppDbContext db,
    PasswordHashService passwordHashService,
    JwtService jwtService) : IAuthenticateService
{
    private readonly AppDbContext _db = db;
    private readonly PasswordHashService _passwordHashService = passwordHashService;
    private readonly JwtService _jwtService = jwtService;

    public async Task<UserEntity> Register(RegisterContract contract)
    {
        var isExistedUser = await _db.Users.AnyAsync(u => u.Email == contract.Email);

        if (isExistedUser)
            throw new EmailAlreadyExistsException(contract.Email);

        var hashedPassword = _passwordHashService.Hash(contract.Password);

        var user = new UserEntity
        {
            HashPassword = hashedPassword,
            Username = contract.Username,
            Email = contract.Email,
        };

        _db.Users.Add(user);

        await _db.SaveChangesAsync();

        return user;
    }

    public async Task<JwtTokensPair> Login(LoginContract contract)
    {
        var user = await _db.Users.FirstOrDefaultAsync(e => e.Email == contract.Email)
                   ?? throw new UserNotFoundException(contract.Email);

        if (!_passwordHashService.Verify(contract.Password, user.HashPassword))
        {
            throw new InvalidCredentialsException();
        }

        var payload = new JwtTokenPayload { Id = user.Id, Email = user.Email };

        return new JwtTokensPair(_jwtService.GenerateAccessToken(payload),
            _jwtService.GenerateRefreshToken(payload));
    }

    public async Task<JwtTokensPair> Refresh(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(e => e.Id == userId)
                   ?? throw new UserNotFoundException("");

        var payload = new JwtTokenPayload { Id = user.Id, Email = user.Email };

        return new JwtTokensPair(_jwtService.GenerateAccessToken(payload),
            _jwtService.GenerateRefreshToken(payload));
    }
}