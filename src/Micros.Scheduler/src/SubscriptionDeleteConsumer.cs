using System.Text.Json;
using Micros.Core.messages;
using Micros.Core.rabbitmq;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces;
using TickerQ.Utilities.Interfaces.Managers;

namespace Micros.Scheduler;

public class SubscriptionDeleteConsumer : IRabbitMqConsumer
{
    private readonly IServiceScopeFactory _scopeFactory;

    public string Exchange => HHSubscriptionTopology.Exchange;
    public string QueueName => "hh-parser.subscription.delete";
    public string RoutingKey => HHSubscriptionTopology.DeleteSubscriptionKey;

    private readonly ILogger<SubscriptionDeleteConsumer> _logger;

    public RabbitMqConsumerSettings Settings => new();

    public SubscriptionDeleteConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionDeleteConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(string body, CancellationToken ct)
    {
        var message = JsonSerializer.Deserialize<HHSubscriptionDeleteMessage>(body);
        if (message is null) return;

        _logger.LogInformation("subscription delete: {SubscriptionId}", message.SubscriptionId);

        await using var scope = _scopeFactory.CreateAsyncScope();

        var cronManager = scope.ServiceProvider.GetRequiredService<ICronTickerManager<CronTickerEntity>>();
        var persistenceProvider = scope.ServiceProvider
            .GetRequiredService<ITickerPersistenceProvider<TimeTickerEntity, CronTickerEntity>>();

        var exists = (await persistenceProvider.GetCronTickers(e => e.Id == message.SubscriptionId, ct)).Any();
        
        if (!exists)
        {
            _logger.LogWarning("ticker {SubscriptionId} not found, nothing to delete", message.SubscriptionId);
            return;
        }

        await cronManager.DeleteAsync(message.SubscriptionId, ct);
    }
}