using Micros.Api.Domains.Common;
using Micros.Api.Domains.Common.Exceptions;

namespace Micros.Api.Infrastructure.Authorize;

public class AuthorizeService: IAuthorizeService
{
    public void EnsureOwner<T>(T entity, Guid userId) where T : IOwnedEntity
    {
        if (entity.UserId != userId)
            throw new ForbiddenException();
    }
}