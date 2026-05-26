namespace Micros.Api.Domains.Subscription;

public interface ISubscriptionService
{
    Task<SubscriptionEntity> CreateAsync(CreateSubscriptionContract contract);
    Task DeleteAsync(Guid subscriptionId, Guid userId);
    Task<List<SubscriptionEntity>> GetForCurrentUserAsync(Guid userId);
    Task<List<SubscriptionEntity>> GetAllWithTelegramIdAsync();
}