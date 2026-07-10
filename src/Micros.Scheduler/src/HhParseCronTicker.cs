using Micros.Core.messages;
using Micros.Core.rabbitmq;
using TickerQ.Utilities.Base;
using Micros.Core.logging;

namespace Micros.Scheduler;

public class HhParseCronTickers
{
    private readonly IRabbitMqPublisher _rabbitMqPublisher;
    private readonly ILogger<HhParseCronTickers> _logger;


    public HhParseCronTickers(IRabbitMqPublisher rabbitMqPublisher, ILogger<HhParseCronTickers> logger)
    {
        _rabbitMqPublisher = rabbitMqPublisher;
        _logger = logger;
    }

    [TickerFunction("hh-parse")]
    public async Task HhParseCronTickerFunc(TickerFunctionContext<HhParsePayload> context, CancellationToken ct)
    {
        using var _ = CorrelationIdContext.Begin(CorrelationId.New());
        
        var payload = context.Request;

        _logger.LogInformation("hh-parse tick: publishing HhParseRequested for {SubscriptionId}", payload.SubscriptionId);

        await _rabbitMqPublisher.PublishAsync(
            exchange: HHSubscriptionTopology.Exchange,
            routingKey: HHSubscriptionTopology.ParseSubscriptionKey,
            message: new HhParseRequested(payload.SubscriptionId, payload.Url, payload.UserId, payload.TelegramId),
            cancellationToken: ct
        );
    }
}