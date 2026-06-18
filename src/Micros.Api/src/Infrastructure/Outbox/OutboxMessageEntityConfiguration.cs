using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Micros.Api.Infrastructure.Outbox;

public class OutboxMessageEntityConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("outbox_messages");

        builder
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder
            .Property(e => e.OccurredOn)
            .HasDefaultValueSql("now()");

        builder
            .Property(e => e.Status)
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<OutboxMessageStatus>(v, ignoreCase: true)
            )
            .HasDefaultValue(OutboxMessageStatus.Created);

        builder
            .HasIndex(e => e.OccurredOn)
            .HasFilter("status = 'created'");
    }
}