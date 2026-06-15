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

    public RabbitMqConsumerSettings Settings => new();

    public SubscriptionDeleteConsumer(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task HandleAsync(string body, CancellationToken ct)
    {
        var message = JsonSerializer.Deserialize<HHSubscriptionDeleteMessage>(body);
        if (message is null) return;

        await using var scope = _scopeFactory.CreateAsyncScope();

        var cronManager = scope.ServiceProvider.GetRequiredService<ICronTickerManager<CronTickerEntity>>();
        var persistenceProvider = scope.ServiceProvider
            .GetRequiredService<ITickerPersistenceProvider<TimeTickerEntity, CronTickerEntity>>();

        var exists = (await persistenceProvider.GetCronTickers(e => e.Id == message.SubscriptionId, ct)).Any();
        if (!exists) return;

        await cronManager.DeleteAsync(message.SubscriptionId, ct);
    }
}
