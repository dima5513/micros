namespace Micros.Core.Types;

public readonly struct Patch<T>(T? value)
{
    public bool IsSet { get; } = true;
    public T? Value { get; } = value;

    public bool TryGet(out T? value)
    {
        value = Value;
        return IsSet;
    }
}