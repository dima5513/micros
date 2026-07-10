namespace Micros.Core.logging;

public static class CorrelationId
{
    public const string HeaderName = "X-Correlation-Id";
    public const string PropertyName = "CorrelationId";
    public static string New() => Guid.NewGuid().ToString("N");
}