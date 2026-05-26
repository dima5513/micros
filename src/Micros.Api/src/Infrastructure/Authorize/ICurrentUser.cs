namespace Micros.Api.Infrastructure.Authorize;

public interface ICurrentUser
{
    Guid Id { get; }
}