using Micros.Api.Infrastructure.Authorize;
using Micros.Api.Infrastructure.Database;
using Micros.Core.messages;
using Micros.Core.rabbitmq;
using Microsoft.EntityFrameworkCore;

namespace Micros.Api.Domains.Subscription;

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _db;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;

    private readonly IAuthorizeService _authorizeService;

    public SubscriptionService(AppDbContext db, IRabbitMqPublisher rabbitMqPublisher, IAuthorizeService authorizeService)
    {
        _db = db;
        _rabbitMqPublisher = rabbitMqPublisher;
        _authorizeService = authorizeService;
    }

    public async Task<SubscriptionEntity> CreateAsync(CreateSubscriptionContract contract)
    {
        var isSubscriptionExist = await _db.Subscriptions.AnyAsync(e => e.Url == contract.Url);

        if (isSubscriptionExist)
            throw new SubscriptionUrlAlreadyExistsException(contract.Url);

        var subscription = new SubscriptionEntity
        {
            Url = contract.Url,
            UserId = contract.UserId
        };

        _db.Subscriptions.Add(subscription);

        await _db.SaveChangesAsync();

        var relationUser = await _db.Users
            .Where(u => u.Id == contract.UserId)
            .FirstAsync();

        await _rabbitMqPublisher.PublishAsync(
            exchange: HHSubscriptionTopology.Exchange,
            routingKey: HHSubscriptionTopology.CreateSubscriptionKey,
            message: new HHSubscriptionCreateMessage(
                subscription.Id, subscription.Url, relationUser.Id, relationUser.TelegramId
            ),
            cancellationToken: CancellationToken.None
        );

        return subscription;
    }

    public async Task DeleteAsync(Guid subscriptionId, Guid userId)
    {
        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(e => e.Id == subscriptionId);

        if (subscription is null)
        {
            throw new SubscriptionNotFoundException(subscriptionId);
        }
        
        _authorizeService.EnsureOwner(subscription, userId);

        await _db.Subscriptions.Where(e => e.Id == subscriptionId).ExecuteDeleteAsync();

        await _rabbitMqPublisher.PublishAsync(
            exchange: HHSubscriptionTopology.Exchange,
            routingKey: HHSubscriptionTopology.DeleteSubscriptionKey,
            message: new HHSubscriptionDeleteMessage(subscriptionId),
            cancellationToken: CancellationToken.None
        );
    }

    public async Task<List<SubscriptionEntity>> GetForCurrentUserAsync(Guid userId)
    {
        return await _db.Subscriptions.Where(e => e.User.Id == userId).ToListAsync();
    }

    public async Task<List<SubscriptionEntity>> GetAllWithTelegramIdAsync()
    {
        return await _db.Subscriptions.Include(e => e.User).ToListAsync();
    }
}