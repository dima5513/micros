using Micros.Api.Infrastructure.Authorize;
using Micros.Api.Infrastructure.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Micros.Api.Domains.Authenticate;

[ApiController]
[Route("api/auth")]
public class AuthenticateController(
    IAuthenticateService authenticateService,
    IOptions<JwtOptions> jwtOptions,
    ILogger<AuthenticateController> logger)
    : ControllerBase
{
    private readonly IAuthenticateService _authenticateService = authenticateService;
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    [HttpPost("register")]
    [ProducesResponseType<RegisterResponseDTO>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDTO dto)
    {
        var createdUser =
            await _authenticateService.Register(new RegisterContract(dto.Username, dto.Email, dto.Password));

        return StatusCode(StatusCodes.Status201Created, new RegisterResponseDTO(
            createdUser.Id, createdUser.Username, createdUser.Email,
            createdUser.CreatedAt, createdUser.UpdatedAt));
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO dto)
    {
        var tokensPair =
            await _authenticateService.Login(new LoginContract(dto.Email, dto.Password));

        var accessCookiesOptions = BuildAuthCookie(_jwtOptions.JwtAccessExpireHours);

        var refreshCookiesOptions = BuildAuthCookie(_jwtOptions.JwtRefreshExpireHours);

        Response.Cookies.Append(AuthorizeCookieNames.AccessToken, tokensPair.AccessToken, accessCookiesOptions);
        Response.Cookies.Append(AuthorizeCookieNames.RefreshToken, tokensPair.RefreshToken, refreshCookiesOptions);

        return NoContent();
    }

    [HttpPost("refresh")]
    [Authorize(AuthenticationSchemes = AppAuthorize.RefreshScheme)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefreshTokens([FromServices] ICurrentUser currentUser)
    {
        var tokensPair = await _authenticateService.Refresh(currentUser.Id);

        var accessCookiesOptions = BuildAuthCookie(_jwtOptions.JwtAccessExpireHours);

        var refreshCookiesOptions = BuildAuthCookie(_jwtOptions.JwtRefreshExpireHours);

        Response.Cookies.Append(AuthorizeCookieNames.AccessToken, tokensPair.AccessToken, accessCookiesOptions);
        Response.Cookies.Append(AuthorizeCookieNames.RefreshToken, tokensPair.RefreshToken, refreshCookiesOptions);

        return NoContent();
    }

    private CookieOptions BuildAuthCookie(int expireInHours) => new()
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        Secure = true,
        Expires = DateTime.UtcNow.AddHours(expireInHours)
    };

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthorizeCookieNames.AccessToken);
        Response.Cookies.Delete(AuthorizeCookieNames.RefreshToken);

        return NoContent();
    }
}