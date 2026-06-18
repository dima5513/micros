using Micros.Api.Domains.Subscription;
using Micros.Api.Domains.User;
using Micros.Api.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Micros.Api.Infrastructure.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options): DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<SubscriptionEntity> Subscriptions => Set<SubscriptionEntity>();

    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}