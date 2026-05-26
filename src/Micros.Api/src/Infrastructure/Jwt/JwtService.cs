using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Micros.Api.Infrastructure.Jwt;

public class JwtService(IOptions<JwtOptions> options)
  {
      private static readonly JwtSecurityTokenHandler Handler = new()
      {
          MapInboundClaims = false
      };
      private readonly JwtOptions _options = options.Value;

      public string GenerateAccessToken(JwtTokenPayload payload) =>
          GenerateToken(payload, _options.JwtAccessSecurityKey, _options.JwtAccessExpireHours);

      public string GenerateRefreshToken(JwtTokenPayload payload) =>
          GenerateToken(payload, _options.JwtRefreshSecurityKey, _options.JwtRefreshExpireHours);

      public ClaimsPrincipal? ValidateAccessToken(string token) =>
          ValidateToken(token, _options.JwtAccessSecurityKey, _options.Issuer, _options.Audience);

      public ClaimsPrincipal? ValidateRefreshToken(string token) =>
          ValidateToken(token, _options.JwtRefreshSecurityKey, _options.Issuer, _options.Audience);

      private string GenerateToken(JwtTokenPayload payload, string securityKey, int expireHours)
      {
          var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));
          var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

          var token = new JwtSecurityToken(
              issuer: _options.Issuer,
              audience: _options.Audience,
              claims: [
                  new Claim(JwtRegisteredClaimNames.Sub, payload.Id.ToString()),
                  new Claim(JwtRegisteredClaimNames.Email, payload.Email),
                  new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
              ],
              expires: DateTime.UtcNow.AddHours(expireHours),
              signingCredentials: creds);

          return Handler.WriteToken(token);
      }

      private ClaimsPrincipal? ValidateToken(string token, string securityKey, string issuer, string audience)
      {
          if (string.IsNullOrWhiteSpace(token) || !Handler.CanReadToken(token))
              return null;
          
          try
          {
              return Handler.ValidateToken(
                  token,
                  _options.BuildValidationParameters(securityKey, issuer, audience),
                  out _);
          }
          catch (SecurityTokenException)
          {
              return null;
          }
      }
  }
