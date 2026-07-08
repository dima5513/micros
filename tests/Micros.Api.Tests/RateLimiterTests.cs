using System.Net;
using Micros.Api.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Micros.Api.Tests;

public class RateLimiterTests
{
    [Fact]
    public async Task UnathorizedUser_IpBucket_Get429()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAppRateLimiter();

        await using var app = builder.Build();
        app.UseRateLimiter();
        app.MapGet("/ping", () => Results.Ok())
            .RequireRateLimiting(AppRateLimiter.BucketPolicy);

        await app.StartAsync();
        var client = app.GetTestClient();

        const int ipTokenLimit = 20;

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < ipTokenLimit + 1; i++)
        {
            var response = await client.GetAsync("/ping");
            statuses.Add(response.StatusCode);
        }

        Assert.Equal(ipTokenLimit, statuses.Count(s => s == HttpStatusCode.OK));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
    }
}
