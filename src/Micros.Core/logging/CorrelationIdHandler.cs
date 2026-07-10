namespace Micros.Core.logging;

public class CorrelationIdHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = CorrelationIdContext.Current;

        if (!string.IsNullOrWhiteSpace(correlationId) && !request.Headers.Contains(CorrelationId.HeaderName))
        {
            request.Headers.Add(CorrelationId.HeaderName, correlationId);
        }
        
        return base.SendAsync(request, cancellationToken);
    }
}