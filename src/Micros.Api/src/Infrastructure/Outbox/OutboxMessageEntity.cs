namespace Micros.Api.Infrastructure.Outbox;

public class OutboxMessageEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredOn { get; set; }
    public required string Payload { get; set; }
    public OutboxMessageStatus Status { get; set; }
    public required string Exchange { get; set; }
    public required string RoutingKey { get; set; }
    
    public string? CorrelationId { get; set; }
}

public enum OutboxMessageStatus
{
    Created,
    Processed
}