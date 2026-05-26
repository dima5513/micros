using Micros.Api.Domains.Subscription;

namespace Micros.Api.Domains.User;

public class UserEntity
{
    public Guid Id { get; set; } 
    public string Username { get; set; }
    public string Email { get; set; }
    public string HashPassword { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    public long? TelegramId { get; set; }
    
    public List<SubscriptionEntity> Subscriptions { get; set; }
}