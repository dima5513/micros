using System.Security.Claims;
using System.Text.Encodings.Web;
using Micros.Core.api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Micros.Api.Infrastructure.Authorize;

public class S2sAuthenticationHandler : AuthenticationHandler<S2sAuthenticationOptions>
{
    private readonly S2sOptions _s2sOptions;

    public S2sAuthenticationHandler(
        IOptionsMonitor<S2sAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<S2sOptions> s2sOptions)
        : base(options, logger, encoder)
    {
        _s2sOptions = s2sOptions.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? header = Request.Headers.Authorization;

        if (header != $"Bearer {_s2sOptions.ApiKey}")
            return Task.FromResult(AuthenticateResult.Fail("Invalid key"));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(Scheme.Name));
        return Task.FromResult(
            AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
