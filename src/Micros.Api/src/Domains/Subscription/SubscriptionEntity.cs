using Micros.Api.Domains.Common;
using Micros.Api.Domains.User;

namespace Micros.Api.Domains.Subscription;

public class SubscriptionEntity: IOwnedEntity
{
    public Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string Url { get; set; }
    public UserEntity User { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}