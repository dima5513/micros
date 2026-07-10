using Serilog.Context;

namespace Micros.Core.logging;

public static class CorrelationIdContext
{
    private static readonly AsyncLocal<string?> CurrentId = new();

    public static string? Current => CurrentId.Value;

    public static IDisposable Begin(string correlationId)
    {
        var previous = CurrentId.Value;
        CurrentId.Value = correlationId;

        var popProperty = LogContext.PushProperty(CorrelationId.PropertyName, correlationId);

        return new Scope(popProperty, previous);
    }


    private sealed class Scope(IDisposable popProperty, string? previous) : IDisposable
    {
        public void Dispose()
        {
            popProperty.Dispose();
            CurrentId.Value = previous;
        }
    }
}