using System.Text.Json;
using Micros.Core.messages;
using Micros.Core.rabbitmq;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces;
using TickerQ.Utilities.Interfaces.Managers;

namespace Micros.Scheduler;

public class SubscriptionUserUpdatedConsumer : IRabbitMqConsumer
{
    private readonly IServiceScopeFactory _scopeFactory;

    public string Exchange => HHUserTopology.Exchange;
    public string QueueName => "hh-parser.user.updated";
    public string RoutingKey => HHUserTopology.UpdateUserKey;

    public RabbitMqConsumerSettings Settings => new();

    public SubscriptionUserUpdatedConsumer(IServiceScopeFactory scopeFactory) =>
        _scopeFactory = scopeFactory;

    public async Task HandleAsync(string body, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var cronTickerManager = scope.ServiceProvider.GetRequiredService<ICronTickerManager<CronTickerEntity>>();
        var tickerPersistenceProvider = scope.ServiceProvider
            .GetRequiredService<ITickerPersistenceProvider<TimeTickerEntity, CronTickerEntity>>();

        var message = JsonSerializer.Deserialize<HHUserUpdatedMessage>(body);

        var cronTickers = await tickerPersistenceProvider.GetCronTickers(e => e.Function == "hh-parse", ct);

        if (cronTickers is not null && message is not null)
        {
            var updatedCronTickerEntities = cronTickers
                .Select(entity => new
                    {
                        entity,
                        payload = TickerHelper.ReadTickerRequest<HhParsePayload>(entity.Request)
                    }
                )
                .Where(data => data.payload.UserId == message.UserId).ToList();

            foreach (var data in updatedCronTickerEntities)
            {
                data.entity.Request =
                    TickerHelper.CreateTickerRequest(data.payload with { TelegramId = message.TelegramId });

                await cronTickerManager.UpdateAsync(data.entity, ct);
            }
        }
    }
}