using System.Text.Json;
using Micros.Core.messages;
using Micros.Core.rabbitmq;
using Microsoft.Extensions.Options;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces;
using TickerQ.Utilities.Interfaces.Managers;

namespace Micros.Scheduler;

public class SubscriptionCreateConsumer : IRabbitMqConsumer
{
    private const string ParseFunctionName = "hh-parse";

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly SchedulerOptions _schedulerOptions;

    public string Exchange => HHSubscriptionTopology.Exchange;
    public string QueueName => "hh-parser.subscription.create";
    public string RoutingKey => HHSubscriptionTopology.CreateSubscriptionKey;

    public RabbitMqConsumerSettings Settings => new();

    public SubscriptionCreateConsumer(IServiceScopeFactory scopeFactory, IOptions<SchedulerOptions> schedulerOptions)
    {
        _scopeFactory = scopeFactory;
        _schedulerOptions = schedulerOptions.Value;
    }

    public async Task HandleAsync(string body, CancellationToken ct)
    {
        var message = JsonSerializer.Deserialize<HHSubscriptionCreateMessage>(body);
        if (message is null) return;

        await using var scope = _scopeFactory.CreateAsyncScope();

        var cronManager = scope.ServiceProvider.GetRequiredService<ICronTickerManager<CronTickerEntity>>();
        var persistenceProvider = scope.ServiceProvider
            .GetRequiredService<ITickerPersistenceProvider<TimeTickerEntity, CronTickerEntity>>();

        // Create несёт снапшот подписки на момент создания. Если тикер с этим Id уже есть — это
        // повторная доставка; перезаписывать нельзя, иначе откатим более свежий UserUpdated (lost update).
        var exists = (await persistenceProvider.GetCronTickers(e => e.Id == message.SubscriptionId, ct)).Any();
        if (exists) return;

        var result = await cronManager.AddAsync(new CronTickerEntity
        {
            Id = message.SubscriptionId,
            Function = ParseFunctionName,
            Expression = _schedulerOptions.ParseCronExpression,
            Request = TickerHelper.CreateTickerRequest(
                new HhParsePayload(message.SubscriptionId, message.Url, message.UserId, message.TelegramId))
        }, ct);

        // AddAsync не бросает на ошибку валидации (cron/функция) — возвращает неуспешный результат.
        // Бросаем сами, чтобы хост залогировал и вернул сообщение в очередь, а не проглотил молча.
        if (!result.IsSucceeded)
            throw result.Exception
                  ?? new InvalidOperationException($"AddAsync failed for ticker {message.SubscriptionId}");
    }
}
