using System.Text.Json;
using System.Web;
using Micros.Core.messages;
using Micros.Core.rabbitmq;
using Micros.Core.redis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Micros.HHParser;

public class HhVacancyParserBackgroundService(
    ILogger<HhVacancyParserBackgroundService> logger,
    IOptions<HhVacancyParserOptions> options,
    HhVacancyHttpApiClient apiClient,
    RedisConnection redis,
    IRabbitMqPublisher rabbitMqPublisher,
    ApiHttpClient apiHttpClient
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await BootstrapSubscriptionsAsync(ct);

        var interval = TimeSpan.FromSeconds(options.Value.IntervalSeconds);

        while (!ct.IsCancellationRequested)
        {
            logger.LogInformation("poll tick start");
            try
            {
                var db = await redis.GetDatabaseAsync();

                var entries = await db.HashGetAllAsync("hh:tasks");
                logger.LogInformation("poll tick: {Count} subscriptions in hh:tasks", entries.Length);

                await Parallel.ForEachAsync(entries,
                    new ParallelOptions
                    {
                        CancellationToken = ct,
                        MaxDegreeOfParallelism = options.Value.MaxConcurrency
                    },
                    async (entry, stopToken) =>
                    {
                        var taskId = Guid.Parse((string)entry.Name!);

                        var json = (string)entry.Value!;
                        var cached = JsonSerializer.Deserialize<SubscriptionCacheEntry>(json)
                                     ?? throw new InvalidOperationException($"corrupt cache entry for {taskId}");

                        await ProcessTaskAsync(db: db, taskId: taskId, sub: cached, ct: stopToken);
                    }
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "poll failed");
            }

            logger.LogInformation("poll tick done, sleeping {Seconds}s", interval.TotalSeconds);
            try
            {
                await Task.Delay(interval, ct);
            }
            catch (OperationCanceledException ex)
            {
                break;
            }
        }
    }

    private async Task ProcessTaskAsync(IDatabase db, Guid taskId, SubscriptionCacheEntry sub, CancellationToken ct)
    {
        try
        {
            var storedRedisValue = await db.StringGetAsync($"hh:watermark:{taskId}");

            DateTimeOffset? latestCreationTime =
                storedRedisValue.HasValue ? DateTimeOffset.Parse(storedRedisValue!) : null;

            DateTimeOffset? newestCreationTime = null;

            var queryString = new Uri(sub.Url).Query;

            var query = HttpUtility.ParseQueryString(queryString);

            query["search_period"] = "1";

            await foreach (var item in apiClient.SearchHtmlAsync(query, ct))
            {
                if (newestCreationTime is null || item.CreationTime > newestCreationTime)
                    newestCreationTime = item.CreationTime;

                if (latestCreationTime is null) continue;
                if (item.CreationTime <= latestCreationTime) continue;

                logger.LogInformation("fresh vacancy {Item}", item);

                if (sub.TelegramId is not null)
                    await rabbitMqPublisher.PublishAsync(
                        exchange: "hh.vacancies",
                        routingKey: "hh.new-vacancy",
                        message: new NewVacancyMessage(
                            TaskId: taskId,
                            VacancyId: item.VacancyId,
                            TelegramId: (long)sub.TelegramId,
                            Name: item.Name,
                            Url: $"{options.Value.HhHost}vacancy/{item.VacancyId}",
                            CreationTime: item.CreationTime
                        ),
                        cancellationToken: ct
                    );
            }

            if (newestCreationTime is not null &&
                (latestCreationTime is null || newestCreationTime > latestCreationTime))
            {
                await db.StringSetAsync($"hh:watermark:{taskId}",
                    newestCreationTime.Value.ToString("O"));
            }
        }

        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "task {TaskId} failed", taskId);
        }
    }


    private async Task BootstrapSubscriptionsAsync(CancellationToken ct)
    {
        var subs = await apiHttpClient.GetSubscriptionsAsync();

        var db = await redis.GetDatabaseAsync();

        await db.KeyDeleteAsync("hh:tasks");
        if (subs.Count == 0) return;

        var entries = subs
            .Select(s => new HashEntry(s.Id.ToString(),
                JsonSerializer.Serialize(new SubscriptionCacheEntry(s.Url, s.UserId, s.TelegramId))))
            .ToArray();
        await db.HashSetAsync("hh:tasks", entries);

        logger.LogInformation("bootstrapped {Count} subscriptions into hh:tasks", subs.Count);
    }
}