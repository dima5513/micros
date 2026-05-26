using Micros.Api.Domains.User;
using Micros.Api.Infrastructure.Jwt;

namespace Micros.Api.Domains.Authenticate;

public interface IAuthenticateService
{
    Task<UserEntity> Register(RegisterContract contract);
    Task<JwtTokensPair> Login(LoginContract contract);
    Task<JwtTokensPair> Refresh(Guid userId);
}