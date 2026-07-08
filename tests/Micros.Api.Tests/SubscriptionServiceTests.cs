using Micros.Api.Domains.Common.Exceptions;
using Micros.Api.Domains.Subscription;
using Micros.Api.Domains.User;
using Micros.Api.Infrastructure.Authorize;
using Micros.Core.messages;
using Microsoft.EntityFrameworkCore;

namespace Micros.Api.Tests;

public class SubscriptionServiceTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public SubscriptionServiceTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateAsync_PersistsSubscription_AndWritesOutboxMessage()
    {
        Guid userId;

        await using (var seedDb = _fixture.CreateDbContext())
        {
            var user = new UserEntity()
            {
                Username = "tester",
                Email = $"tester-{Guid.NewGuid()}@example.com",
                HashPassword = "hashed_password"
            };
            seedDb.Users.Add(user);
            await seedDb.SaveChangesAsync();

            userId = user.Id;
        }


        var url = $"https://hh.ru/search/vacancy?text={Guid.NewGuid()}";

        await using var db = _fixture.CreateDbContext();
        var sut = new SubscriptionService(db, new AuthorizeService());

        var created = await sut.CreateAsync(new CreateSubscriptionContract(url, userId));

        await using var verifyDb = _fixture.CreateDbContext();
        var stored = await verifyDb.Subscriptions.FirstOrDefaultAsync(s => s.Id == created.Id);

        Assert.NotNull(stored);
        Assert.Equal(url, stored!.Url);
        Assert.Equal(userId, stored.UserId);

        var outboxCount = await verifyDb.OutboxMessages.CountAsync(m =>
            m.Exchange == HHSubscriptionTopology.Exchange &&
            m.RoutingKey == HHSubscriptionTopology.CreateSubscriptionKey &&
            m.Payload.Contains(created.Id.ToString()));

        Assert.Equal(1, outboxCount);
    }

    [Fact]
    public async Task CreateAsync_DuplicateUrl_ThrowsSubscriptionUrlAlreadyExists()
    {
        var url = $"https://hh.ru/search/vacancy?text={Guid.NewGuid()}";

        Guid userId;
        await using (var seedDb = _fixture.CreateDbContext())
        {
            var user = new UserEntity
            {
                Username = "tester",
                Email = $"tester-{Guid.NewGuid()}@example.com",
                HashPassword = "hashed_password"
            };

            seedDb.Users.Add(user);
            await seedDb.SaveChangesAsync();

            seedDb.Subscriptions.Add(new SubscriptionEntity
            {
                Url = url,
                UserId = user.Id
            });
            await seedDb.SaveChangesAsync();

            userId = user.Id;
        }

        await using var db = _fixture.CreateDbContext();
        var sut = new SubscriptionService(db, new AuthorizeService());

        await Assert.ThrowsAsync<SubscriptionUrlAlreadyExistsException>(() =>
            sut.CreateAsync(new CreateSubscriptionContract(url, userId))
        );

        await using var verifyDb = _fixture.CreateDbContext();
        var outboxCount = await verifyDb.OutboxMessages.CountAsync(m =>
            m.RoutingKey == HHSubscriptionTopology.CreateSubscriptionKey &&
            m.Payload.Contains(url));

        Assert.Equal(0, outboxCount);
    }

    [Fact]
    public async Task DeleteAsync_OwnerDeletesSubscription_RemovesAndWritesOutboxMessage()
    {
        var url = $"https://hh.ru/search/vacancy?text={Guid.NewGuid()}";

        Guid userId;
        Guid subscriptionId;
        await using (var seedDb = _fixture.CreateDbContext())
        {
            var user = new UserEntity
            {
                Username = "tester",
                Email = $"tester-{Guid.NewGuid()}@example.com",
                HashPassword = "hashed_password"
            };
            seedDb.Users.Add(user);
            await seedDb.SaveChangesAsync();
            userId = user.Id;

            var subscription = new SubscriptionEntity
            {
                UserId = user.Id,
                Url = url
            };
            seedDb.Subscriptions.Add(subscription);
            await seedDb.SaveChangesAsync();
            subscriptionId = subscription.Id;
        }

        await using var db = _fixture.CreateDbContext();
        var sut = new SubscriptionService(db, new AuthorizeService());

        await sut.DeleteAsync(subscriptionId, userId);

        await using var verifyDb = _fixture.CreateDbContext();

        var deletedSubscription = await verifyDb.Subscriptions.FirstOrDefaultAsync(s => s.Id == subscriptionId);

        Assert.Null(deletedSubscription);

        var outboxCount = await verifyDb.OutboxMessages.CountAsync(m =>
            m.RoutingKey == HHSubscriptionTopology.DeleteSubscriptionKey &&
            m.Payload.Contains(subscriptionId.ToString()));

        Assert.Equal(1, outboxCount);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsAndDoesNotWriteOutbox()
    {
        await using var db = _fixture.CreateDbContext();
        var sut = new SubscriptionService(db, new AuthorizeService());

        var missingId = Guid.NewGuid();

        await Assert.ThrowsAsync<SubscriptionNotFoundException>(
            () => sut.DeleteAsync(missingId, Guid.NewGuid())
        );

        await using var verifyDb = _fixture.CreateDbContext();
        var outboxCount = await verifyDb.OutboxMessages.CountAsync(m =>
            m.RoutingKey == HHSubscriptionTopology.DeleteSubscriptionKey &&
            m.Payload.Contains(missingId.ToString()));

        Assert.Equal(0, outboxCount);
    }

    [Fact]
    public async Task DeleteAsync_NotOwner_ThrowsForbiddenAndDoesNotWriteOutbox()
    {
        var url = $"https://hh.ru/search/vacancy?text={Guid.NewGuid()}";

        Guid subscriptionId;
        await using (var seedDb = _fixture.CreateDbContext())
        {
            var user = new UserEntity
            {
                Username = "tester",
                Email = $"tester-{Guid.NewGuid()}@example.com",
                HashPassword = "hashed_password"
            };
            seedDb.Users.Add(user);
            await seedDb.SaveChangesAsync();

            var subscription = new SubscriptionEntity
            {
                UserId = user.Id,
                Url = url
            };

            seedDb.Subscriptions.Add(subscription);
            await seedDb.SaveChangesAsync();
            subscriptionId = subscription.Id;
        }

        await using var db = _fixture.CreateDbContext();
        var sut = new SubscriptionService(db, new AuthorizeService());

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.DeleteAsync(subscriptionId, Guid.NewGuid())
        );

        await using var verifyDb = _fixture.CreateDbContext();
        var stillThere = await verifyDb.Subscriptions.FirstOrDefaultAsync(s => s.Id == subscriptionId);
        Assert.NotNull(stillThere);

        var outboxCount = await verifyDb.OutboxMessages.CountAsync(m =>
            m.RoutingKey == HHSubscriptionTopology.DeleteSubscriptionKey &&
            m.Payload.Contains(subscriptionId.ToString()));

        Assert.Equal(0, outboxCount);
    }
}
