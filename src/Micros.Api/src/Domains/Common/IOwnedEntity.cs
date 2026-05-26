namespace Micros.Api.Domains.Common;

public interface IOwnedEntity
{
    Guid UserId { get; }
}