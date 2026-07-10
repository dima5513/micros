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

    private readonly ILogger<SubscriptionCreateConsumer> _logger;

    public string Exchange => HHSubscriptionTopology.Exchange;
    public string QueueName => "hh-parser.subscription.create";
    public string RoutingKey => HHSubscriptionTopology.CreateSubscriptionKey;

    public RabbitMqConsumerSettings Settings => new();

    public SubscriptionCreateConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<SchedulerOptions> schedulerOptions,
        ILogger<SubscriptionCreateConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _schedulerOptions = schedulerOptions.Value;
        _logger = logger;
    }

    public async Task HandleAsync(string body, CancellationToken ct)
    {
        var message = JsonSerializer.Deserialize<HHSubscriptionCreateMessage>(body);
        if (message is null) return;

        _logger.LogInformation("subscription create: {SubscriptionId} for user {UserId}", message.SubscriptionId,
            message.UserId);

        await using var scope = _scopeFactory.CreateAsyncScope();

        var cronManager = scope.ServiceProvider.GetRequiredService<ICronTickerManager<CronTickerEntity>>();
        var persistenceProvider = scope.ServiceProvider
            .GetRequiredService<ITickerPersistenceProvider<TimeTickerEntity, CronTickerEntity>>();

        var exists = (await persistenceProvider.GetCronTickers(e => e.Id == message.SubscriptionId, ct)).Any();
        if (exists)
        {
            _logger.LogInformation("ticker {SubscriptionId} already exists, skip (redelivery)", message.SubscriptionId);
            return;
        }
        
        var result = await cronManager.AddAsync(new CronTickerEntity
        {
            Id = message.SubscriptionId,
            Function = ParseFunctionName,
            Expression = _schedulerOptions.ParseCronExpression,
            Request = TickerHelper.CreateTickerRequest(
                new HhParsePayload(message.SubscriptionId, message.Url, message.UserId, message.TelegramId))
        }, ct);


        if (!result.IsSucceeded)
            throw result.Exception
                  ?? new InvalidOperationException($"AddAsync failed for ticker {message.SubscriptionId}");
    }
}