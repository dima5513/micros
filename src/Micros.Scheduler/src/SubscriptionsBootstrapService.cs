using Micros.Core.messages;
using Microsoft.Extensions.Options;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces;
using TickerQ.Utilities.Interfaces.Managers;

namespace Micros.Scheduler;

// При старте досоздаёт тикеры для подписок, которые есть в Api, но которых нет в расписании
// (например, события Create, пропущенные пока Scheduler был выключен). Удалением сирот не занимается —
// это делает SubscriptionDeleteConsumer. Идемпотентно: существующие Id пропускаются.
public class SubscriptionsBootstrapService(
    IServiceScopeFactory scopeFactory,
    IOptions<SchedulerOptions> schedulerOptions,
    ILogger<SubscriptionsBootstrapService> logger
) : BackgroundService
{
    private const string ParseFunctionName = "hh-parse";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            var apiClient = scope.ServiceProvider.GetRequiredService<ApiHttpClient>();
            var cronManager = scope.ServiceProvider.GetRequiredService<ICronTickerManager<CronTickerEntity>>();
            var persistenceProvider = scope.ServiceProvider
                .GetRequiredService<ITickerPersistenceProvider<TimeTickerEntity, CronTickerEntity>>();

            var subscriptions = await apiClient.GetSubscriptionsAsync();
            if (subscriptions.Count == 0) return;

            var existingIds = (await persistenceProvider.GetCronTickers(e => e.Function == ParseFunctionName, ct))
                .Select(e => e.Id)
                .ToHashSet();

            var created = 0;
            foreach (var sub in subscriptions)
            {
                if (existingIds.Contains(sub.Id)) continue;

                var result = await cronManager.AddAsync(new CronTickerEntity
                {
                    Id = sub.Id,
                    Function = ParseFunctionName,
                    Expression = schedulerOptions.Value.ParseCronExpression,
                    Request = TickerHelper.CreateTickerRequest(
                        new HhParsePayload(sub.Id, sub.Url, sub.UserId, sub.TelegramId))
                }, ct);

                if (result.IsSucceeded)
                    created++;
                else
                    logger.LogError(result.Exception,
                        "bootstrap: AddAsync NOT persisted for {SubscriptionId} (expr='{Expr}')",
                        sub.Id, schedulerOptions.Value.ParseCronExpression);
            }

            logger.LogInformation("bootstrap: created {Created} missing tickers of {Total} subscriptions",
                created, subscriptions.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "bootstrap failed");
        }
    }
}
