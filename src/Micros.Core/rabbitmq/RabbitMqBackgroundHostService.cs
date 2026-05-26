using System.Text;
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
        
        await channel.QueueBindAsync(queue: consumer.QueueName, exchange: consumer.Exchange,
            routingKey: consumer.RoutingKey,
            cancellationToken: ct);

        await channel.BasicQosAsync(0, prefetchCount: consumer.Settings.PrefetchCount, global: false, cancellationToken: ct);

        var basicConsumer = new AsyncEventingBasicConsumer(channel);

        basicConsumer.ReceivedAsync += async (_, args) =>
        {
            var message = Encoding.UTF8.GetString(args.Body.ToArray());

            try
            {
                await consumer.HandleAsync(message, ct);
                await channel.BasicAckAsync(deliveryTag: args.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                logger.LogError("consumer: {Consumer}, exception: {Exception}", consumer.GetType().Name, ex);
                await channel.BasicNackAsync(deliveryTag: args.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await channel.BasicConsumeAsync(queue: consumer.QueueName, autoAck: false, consumer: basicConsumer,
            cancellationToken: ct);
    }
}