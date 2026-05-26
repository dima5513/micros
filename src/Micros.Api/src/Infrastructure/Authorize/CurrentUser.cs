using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Micros.Api.Infrastructure.Authorize;

public class CurrentUser : ICurrentUser
{
    public CurrentUser(IHttpContextAccessor accessor)
    {
        var sub = accessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        Id = Guid.TryParse(sub, out var id) ? id : throw new UnauthorizedAccessException("missing sub claim");
    }

    public Guid Id { get; }
}