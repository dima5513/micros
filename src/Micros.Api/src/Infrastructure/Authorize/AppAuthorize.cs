using Micros.Api.Infrastructure.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Micros.Api.Infrastructure.Authorize;

public static class AppAuthorize
{
    public const string AccessScheme = "AccessScheme";
    public const string RefreshScheme = "RefreshScheme";

    public static void AddAppAuthorize(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();


        var jwtOptions = configuration.Get<JwtOptions>()
                         ?? throw new InvalidOperationException("jwt options is missing");

        services.AddAuthentication(AccessScheme)
            .AddJwtBearer(AccessScheme, options =>
            {

                options.MapInboundClaims = false;

                options.TokenValidationParameters =
                    jwtOptions.BuildValidationParameters(securityKey: jwtOptions.JwtAccessSecurityKey,
                        issuer: jwtOptions.Issuer, audience: jwtOptions.Audience);


                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        if (ctx.Request.Cookies.TryGetValue(AuthorizeCookieNames.AccessToken, out var value))
                            ctx.Token = value;
                        return Task.CompletedTask;
                    }
                };
            })
            .AddJwtBearer(RefreshScheme, options =>
            {

                options.MapInboundClaims = false;

                options.TokenValidationParameters =
                    jwtOptions.BuildValidationParameters(securityKey: jwtOptions.JwtRefreshSecurityKey,
                        issuer: jwtOptions.Issuer, audience: jwtOptions.Audience);


                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        if (ctx.Request.Cookies.TryGetValue(AuthorizeCookieNames.RefreshToken, out var value))
                            ctx.Token = value;
                        return Task.CompletedTask;
                    }
                };
            });
    }
}
