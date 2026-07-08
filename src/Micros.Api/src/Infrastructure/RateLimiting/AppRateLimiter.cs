using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Micros.Api.Infrastructure.RateLimiting;

public static class AppRateLimiter
{
    public const string BucketPolicy = "bucket-rate-limiter";

    public static void AddAppRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(BucketPolicy, context =>
            {
                if (Guid.TryParse(context.User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
                {
                    return RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: $"user:{userId}",
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 50,
                            TokensPerPeriod = 10,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                            QueueLimit = 0,
                            AutoReplenishment = true,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        });
                }

                return RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 20,
                        TokensPerPeriod = 5,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    });
            });
        });
    }
}
