using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Micros.HHParser;

public static class HhHttpClientExtensions
{
    public static IHttpClientBuilder AddHhResilience(this IHttpClientBuilder builder)
    {
        builder.AddResilienceHandler("hh-http-retry", pipelineBuilder =>
        {
            pipelineBuilder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldRetryAfterHeader = true,
                ShouldHandle = args => ValueTask.FromResult(
                    HttpClientResiliencePredicates.IsTransient(args.Outcome) ||
                    args.Outcome.Result?.StatusCode == HttpStatusCode.TooManyRequests)
            });
        });
        return builder;
    }
}