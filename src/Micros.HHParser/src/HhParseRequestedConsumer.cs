using System.Text.Json;
using System.Web;
using Micros.Core.messages;
using Micros.Core.rabbitmq;
using Micros.Core.redis;
using Microsoft.Extensions.Options;

namespace Micros.HHParser;

public class HhParseRequestedConsumer(IServiceScopeFactory scopeFactory, ILogger<HhParseRequestedConsumer> logger)
    : IRabbitMqConsumer
{
    public string Exchange => HHSubscriptionTopology.Exchange;
    public string QueueName => "hh-parser.parse.run";
    public string RoutingKey => HHSubscriptionTopology.ParseSubscriptionKey;

    public async Task HandleAsync(string message, CancellationToken ct)
    {
        var request = JsonSerializer.Deserialize<HhParseRequested>(message);
        if (request is null) return;

        logger.LogInformation("parse requested for {SubscriptionId}", request.SubscriptionId);

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var apiClient = scope.ServiceProvider.GetRequiredService<HhVacancyHttpApiClient>();
            var redis = scope.ServiceProvider.GetRequiredService<RedisConnection>();
            var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<HhVacancyParserOptions>>().Value;

            var db = await redis.GetDatabaseAsync();

            var seenKey = $"hh:seen:{request.SubscriptionId}";
            var firstRun = !await db.KeyExistsAsync(seenKey);

            var host = apiClient.ResolveHost(request.Url);

            var query = HttpUtility.ParseQueryString(new Uri(request.Url).Query);
            query["search_period"] = options.SearchPeriodDays.ToString();

            var createdAfter = DateTimeOffset.UtcNow.AddDays(-options.SearchPeriodDays);

            var found = 0;
            var republished = 0;

            await foreach (var item in apiClient.SearchHtmlAsync(host, query, ct))
            {
                found++;

                if (!await db.SetAddAsync(seenKey, item.VacancyId)) continue;
                if (firstRun) continue;

                if (item.CreationTime < createdAfter)
                {
                    republished++;
                    continue;
                }

                logger.LogInformation("fresh vacancy {VacancyId} {Name}", item.VacancyId, item.Name);

                if (request.TelegramId is not null)
                    await publisher.PublishAsync(
                        exchange: HHVacancyTopology.Exchange,
                        routingKey: HHVacancyTopology.NewVacancyKey,
                        message: new NewVacancyMessage(
                            TaskId: request.SubscriptionId,
                            VacancyId: item.VacancyId,
                            Name: item.Name,
                            TelegramId: (long)request.TelegramId,
                            CreationTime: item.CreationTime,
                            Url: $"{host}vacancy/{item.VacancyId}"),
                        cancellationToken: ct);
            }

            if (found > 0)
                await db.KeyExpireAsync(seenKey, TimeSpan.FromDays(30));

            logger.LogInformation(
                "parse finished for {SubscriptionId}: {Found} in feed, {Republished} republished skipped, first run {FirstRun}",
                request.SubscriptionId, found, republished, firstRun);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "parse failed for {SubscriptionId}", request.SubscriptionId);
        }
    }
}
