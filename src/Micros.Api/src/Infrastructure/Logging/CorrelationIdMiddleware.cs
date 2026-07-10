using Micros.Core.logging;

namespace Micros.Api.Infrastructure.Logging;

public class CorrelationIdMiddleware
{
    private RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationId.HeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = CorrelationId.New();
        }

        context.Response.Headers[CorrelationId.HeaderName] = correlationId;

        using (CorrelationIdContext.Begin(correlationId))
        {
            await _next(context);
        }
    }
}