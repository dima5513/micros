using System.Text;
using Micros.Api.Infrastructure.Database;
using Micros.Core.logging;
using Micros.Core.rabbitmq;
using Microsoft.EntityFrameworkCore;

namespace Micros.Api.Infrastructure.Outbox;

public class OutboxProcesser : BackgroundService
{
    private IServiceScopeFactory _scopeFactory;
    private IRabbitMqPublisher _rabbitMqPublisher;
    private ILogger<OutboxProcesser> _logger;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    public OutboxProcesser(IServiceScopeFactory scopeFactory, IRabbitMqPublisher rabbitMqPublisher,
        ILogger<OutboxProcesser> logger)
    {
        _scopeFactory = scopeFactory;
        _rabbitMqPublisher = rabbitMqPublisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessageAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processing iteration failed");
            }

            await Task.Delay(PollInterval, cancellationToken: ct);
        }
    }

    private async Task ProcessOutboxMessageAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();


        var messages =
            await db.OutboxMessages
                .Where(e => e.Status == OutboxMessageStatus.Created)
                .OrderBy(e => e.OccurredOn)
                .Take(50)
                .ToListAsync(ct);

        if (messages.Count == 0) return;

        try
        {
            foreach (var message in messages)
            {
                using var correlationIdScope = CorrelationIdContext.Begin(message.CorrelationId ?? CorrelationId.New());
                
                await _rabbitMqPublisher.PublishRawAsync(
                    exchange: message.Exchange,
                    routingKey: message.RoutingKey,
                    body: Encoding.UTF8.GetBytes(message.Payload),
                    cancellationToken: ct
                );

                message.Status = OutboxMessageStatus.Processed;
            }


            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("OutboxProcesser send message to broker with error: {Error}", ex);
        }
    }
}