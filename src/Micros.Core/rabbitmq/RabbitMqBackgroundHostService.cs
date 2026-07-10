using System.Text;
using Micros.Core.logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Micros.Core.rabbitmq;

public class RabbitMqBackgroundHostService(
    ILogger<RabbitMqBackgroundHostService> logger,
    RabbitMqConnection rabbitMqConnection,
    IEnumerable<IRabbitMqConsumer> consumers
) : BackgroundService
{
    private const string DeadQueue = "micros.dead";
    
    private readonly List<IChannel> _channels = [];

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        foreach (var consumer in consumers)
        {
            var channel = await rabbitMqConnection.CreateChannelAsync(cancellationToken: ct);

            _channels.Add(channel);

            await SetupConsumerAsync(channel, consumer, ct);

            logger.LogInformation("consumer {Consumer} was started, listen queue: {Queue}", consumer.GetType().Name,
                consumer.QueueName);
        }

        await Task.Delay(Timeout.Infinite, cancellationToken: ct);
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        foreach (var channel in _channels)
        {
            await channel.CloseAsync(ct);
        }

        await base.StopAsync(ct);
    }

    private async Task SetupConsumerAsync(IChannel channel, IRabbitMqConsumer consumer, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(exchange: consumer.Exchange, consumer.Settings.ExchangeType,
            durable: consumer.Settings.DurableExchange,
            autoDelete: consumer.Settings.AutoDeleteExchange, cancellationToken: ct);
        
        await channel.QueueDeclareAsync(queue: consumer.QueueName, durable: consumer.Settings.DurableQueue,
            exclusive: consumer.Settings.ExclusiveQueue, autoDelete: consumer.Settings.AutoDeleteQueue,
            arguments: consumer.Settings.QueueArguments, cancellationToken: ct);
        
        await channel.QueueDeclareAsync(DeadQueue, durable: true, exclusive: false,
            autoDelete: false, arguments: null, cancellationToken: ct);
        
        await channel.QueueBindAsync(queue: consumer.QueueName, exchange: consumer.Exchange,
            routingKey: consumer.RoutingKey,
            cancellationToken: ct);

        await channel.BasicQosAsync(0, prefetchCount: consumer.Settings.PrefetchCount, global: false, cancellationToken: ct);

        var basicConsumer = new AsyncEventingBasicConsumer(channel);

        basicConsumer.ReceivedAsync += async (_, args) =>
        {

            var correlationId = ReadCorrelationId(args.BasicProperties.Headers) ?? CorrelationId.New();

            using var correlationIdScope = CorrelationIdContext.Begin(correlationId);
            
            var message = Encoding.UTF8.GetString(args.Body.ToArray());

            try
            {
                await consumer.HandleAsync(message, ct);
                await channel.BasicAckAsync(deliveryTag: args.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                var retryCount = ReadRetryCount(args.BasicProperties.Headers);

                if (retryCount < consumer.Settings.MaxRetries)
                {
                    logger.LogWarning(ex, "consumer {Consumer} failed, retry {Attempt}/{Max}",
                        consumer.GetType().Name, retryCount + 1, consumer.Settings.MaxRetries);

                    await Republish(channel, consumer.QueueName, args, retryCount + 1);
                }
                else
                {
                    logger.LogError(ex, "consumer {Consumer} exhausted {Max} retries, -> DLQ",
                        consumer.GetType().Name, consumer.Settings.MaxRetries);

                    await Republish(channel, DeadQueue, args, retryCount);
                }

                await channel.BasicAckAsync(args.DeliveryTag, multiple: false);
            }
        };

        await channel.BasicConsumeAsync(queue: consumer.QueueName, autoAck: false, consumer: basicConsumer,
            cancellationToken: ct);
    }
    
    private static async Task Republish(IChannel channel, string queue, BasicDeliverEventArgs args, int retryCount)
    {
        var headers = new Dictionary<string, object?>(args.BasicProperties.Headers ?? new Dictionary<string, object?>())
        {
            ["x-retry-count"] = retryCount
        };

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = args.BasicProperties.ContentType,
            Headers = headers
        };

        await channel.BasicPublishAsync(exchange: "", routingKey: queue,
            mandatory: false, basicProperties: props, body: args.Body);
    }

    private static int ReadRetryCount(IDictionary<string, object?>? headers)
        => headers is not null && headers.TryGetValue("x-retry-count", out var v) && v is not null
            ? Convert.ToInt32(v)
            : 0;

    private static string? ReadCorrelationId(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue(CorrelationId.HeaderName, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => value.ToString()
        };
    }

}