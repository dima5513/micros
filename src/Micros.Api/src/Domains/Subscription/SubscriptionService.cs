using System.Text.Json;
using Micros.Api.Infrastructure.Authorize;
using Micros.Api.Infrastructure.Database;
using Micros.Api.Infrastructure.Outbox;
using Micros.Core.logging;
using Micros.Core.messages;
using Micros.Core.rabbitmq;
using Microsoft.EntityFrameworkCore;

namespace Micros.Api.Domains.Subscription;

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _db;

    private readonly IAuthorizeService _authorizeService;

    public SubscriptionService(AppDbContext db,
        IAuthorizeService authorizeService)
    {
        _db = db;
        _authorizeService = authorizeService;
    }

    public async Task<SubscriptionEntity> CreateAsync(CreateSubscriptionContract contract)
    {
        var isSubscriptionExist = await _db.Subscriptions.AnyAsync(e => e.Url == contract.Url);

        if (isSubscriptionExist)
            throw new SubscriptionUrlAlreadyExistsException(contract.Url);

        var subscription = new SubscriptionEntity
        {
            Id = Guid.NewGuid(),
            Url = contract.Url,
            UserId = contract.UserId
        };

        _db.Subscriptions.Add(subscription);

        var relationUser = await _db.Users
            .Where(u => u.Id == contract.UserId)
            .FirstAsync();

        _db.OutboxMessages.Add(new OutboxMessageEntity
        {
            Exchange = HHSubscriptionTopology.Exchange,
            RoutingKey = HHSubscriptionTopology.CreateSubscriptionKey,
            Payload = JsonSerializer.Serialize(new HHSubscriptionCreateMessage(
                subscription.Id, subscription.Url, relationUser.Id, relationUser.TelegramId
            )),
            CorrelationId = CorrelationIdContext.Current,
        });

        await _db.SaveChangesAsync();

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

        _db.Subscriptions.Remove(subscription);

        _db.OutboxMessages.Add(new OutboxMessageEntity
        {
            Exchange = HHSubscriptionTopology.Exchange,
            RoutingKey = HHSubscriptionTopology.DeleteSubscriptionKey,
            Payload = JsonSerializer.Serialize(new HHSubscriptionDeleteMessage(subscriptionId)),
            CorrelationId = CorrelationIdContext.Current,
        });

        await _db.SaveChangesAsync();
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