using System.Text;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;

namespace Micros.Api.Infrastructure.Jwt;

public class JwtOptions
{
    [ConfigurationKeyName("JWT_ISSUER")]
    public string Issuer { get; set; }
    [ConfigurationKeyName("JWT_AUDIENCE")]
    
    public string Audience { get; set; }
    [ConfigurationKeyName("JWT_ACCESS_SECURITY_KEY")]

    public string JwtAccessSecurityKey { get; set; }
    [ConfigurationKeyName("JWT_ACCESS_EXPIRE_HOURS")]
    

    public int JwtAccessExpireHours { get; set; }   
    [ConfigurationKeyName("JWT_REFRESH_SECURITY_KEY")]
    
    public string JwtRefreshSecurityKey { get; set; }
    [ConfigurationKeyName("JWT_REFRESH_EXPIRE_HOURS")]
    
    public int JwtRefreshExpireHours { get; set; }
    
    public TokenValidationParameters BuildValidationParameters(string securityKey, string issuer, string audience) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey)),
        ClockSkew = TimeSpan.Zero
    };
}