using Micros.Api.Domains.Common.Exceptions;
using Micros.Api.Domains.Subscription;
using Micros.Api.Domains.User;
using Micros.Api.Infrastructure.Authorize;
using Micros.Core.messages;
using Micros.Core.rabbitmq;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Micros.Api.Tests;

public class SubscriptionServiceTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public SubscriptionServiceTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateAsync_PersistsSubscription_AndPublishesEvent()
    {
        // Arrange
        var publisher = Substitute.For<IRabbitMqPublisher>();

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
        var sut = new SubscriptionService(db, publisher, new AuthorizeService());

        // Act
        var created = await sut.CreateAsync(new CreateSubscriptionContract(url, userId));

        // Assert — реально сохранилось в БД
        await using var verifyDb = _fixture.CreateDbContext();
        var stored = await verifyDb.Subscriptions.FirstOrDefaultAsync(s => s.Id == created.Id);

        Assert.NotNull(stored);
        Assert.Equal(url, stored!.Url);
        Assert.Equal(userId, stored.UserId);

        // Assert — событие опубликовано ровно один раз
        await publisher.Received(1).PublishAsync(
            HHSubscriptionTopology.Exchange,
            HHSubscriptionTopology.CreateSubscriptionKey,
            Arg.Any<HHSubscriptionCreateMessage>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_DuplicateUrl_ThrowsSubscriptionUrlAlreadyExists()
    {
        var publisher = Substitute.For<IRabbitMqPublisher>();

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
        var sut = new SubscriptionService(db, publisher, new AuthorizeService());

        await Assert.ThrowsAsync<SubscriptionUrlAlreadyExistsException>(() =>
            sut.CreateAsync(new CreateSubscriptionContract(url, userId))
        );

        await publisher.DidNotReceive().PublishAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<HHSubscriptionCreateMessage>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task DeleteAsync_OwnerDeletesSubscription_RemovesAndPublishes()
    {
        var publisher = Substitute.For<IRabbitMqPublisher>();

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
        var sut = new SubscriptionService(db, publisher, new AuthorizeService());

        await sut.DeleteAsync(subscriptionId, userId);

        await using var verifyDb = _fixture.CreateDbContext();

        var deletedSubscription = await verifyDb.Subscriptions.FirstOrDefaultAsync(s => s.Id == subscriptionId);

        Assert.Null(deletedSubscription);

        await publisher.Received(1).PublishAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<HHSubscriptionDeleteMessage>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsAndDoesNotPublish()
    {
        var publisher = Substitute.For<IRabbitMqPublisher>();

        await using var db = _fixture.CreateDbContext();
        var sut = new SubscriptionService(db, publisher, new AuthorizeService());

        await Assert.ThrowsAsync<SubscriptionNotFoundException>(
            () => sut.DeleteAsync(Guid.NewGuid(), Guid.NewGuid())
        );

        await publisher.DidNotReceive().PublishAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<HHSubscriptionDeleteMessage>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task DeleteAsync_NotOwner_ThrowsForbiddenAndDoesNotPublish()
    {
        var publisher = Substitute.For<IRabbitMqPublisher>();

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
        var sut = new SubscriptionService(db, publisher, new AuthorizeService());

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.DeleteAsync(subscriptionId, Guid.NewGuid())
        );
        
        await using var verifyDb = _fixture.CreateDbContext();
        var stillThere = await verifyDb.Subscriptions.FirstOrDefaultAsync(s => s.Id == subscriptionId);
        Assert.NotNull(stillThere);

        await publisher.DidNotReceive().PublishAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<HHSubscriptionDeleteMessage>(),
            Arg.Any<CancellationToken>()
        );
    }
}