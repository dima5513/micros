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

            var stored = await db.StringGetAsync($"hh:watermark:{request.SubscriptionId}");
            DateTimeOffset? latest = stored.HasValue ? DateTimeOffset.Parse(stored!) : null;
            DateTimeOffset? newest = null;

            var query = HttpUtility.ParseQueryString(new Uri(request.Url).Query);
            query["search_period"] = "1";

            await foreach (var item in apiClient.SearchHtmlAsync(query, ct))
            {
                if (newest is null || item.CreationTime > newest) newest = item.CreationTime;

                if (latest is null) continue;                 // первый прогон — только baseline, не спамим
                if (item.CreationTime <= latest) continue;

                logger.LogInformation("fresh vacancy {Item}", item);

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
                            Url: $"{options.HhHost}vacancy/{item.VacancyId}"),
                        cancellationToken: ct);
            }

            if (newest is not null && (latest is null || newest > latest))
                await db.StringSetAsync($"hh:watermark:{request.SubscriptionId}", newest.Value.ToString("O"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "parse failed for {SubscriptionId}", request.SubscriptionId);
        }
    }
}
