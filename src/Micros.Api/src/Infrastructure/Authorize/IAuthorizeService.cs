using Micros.Api.Domains.Common;

namespace Micros.Api.Infrastructure.Authorize;

public interface IAuthorizeService
{
    void EnsureOwner<T>(T entity, Guid userId) where T : IOwnedEntity;
}