using Micros.Api.Domains.Common;
using Micros.Api.Domains.Common.Exceptions;
using Micros.Api.Infrastructure.Authorize;

namespace Micros.Api.Tests;

public class AuthorizeServiceTests
{
    private readonly AuthorizeService _sut = new();

    private record FakeOwnedEntity(Guid UserId) : IOwnedEntity;


    [Fact]
    public void EnsureOwner_WhenUserOwnsEntity_DoesNotThrow()
    {
        var userId = Guid.NewGuid();

        var entity = new FakeOwnedEntity(userId);

        var exception = Record.Exception(() => _sut.EnsureOwner(entity, userId));
        
        Assert.Null(exception);
    }

    [Fact]
    public void EnsureOwner_WhenUserIsNotOwner_ThrowsForbidden()
    {
        var strangerId = Guid.NewGuid();

        var entity = new FakeOwnedEntity(Guid.NewGuid());

        Assert.Throws<ForbiddenException>(() => _sut.EnsureOwner(entity, strangerId));
    }
}