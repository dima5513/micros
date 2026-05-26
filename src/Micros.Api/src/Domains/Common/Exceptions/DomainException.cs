namespace Micros.Api.Domains.Common.Exceptions;

public abstract class DomainException(string message) : Exception(message)
{
    public abstract string Code { get; }
    public abstract int StatusCode { get; }
}