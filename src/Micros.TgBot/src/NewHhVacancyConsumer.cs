using System.Text.Json;
using Micros.Core.messages;
using Micros.Core.rabbitmq;
using Telegram.Bot;

namespace Micros.TgBot;

public class NewHhVacancyConsumer(
    ITelegramBotClient tgBotClient,
    ILogger<NewHhVacancyConsumer> logger
)
    : IRabbitMqConsumer
{
    public string Exchange => HHVacancyTopology.Exchange;
    public string QueueName => "tgbot.new-vacancies";
    public string RoutingKey => HHVacancyTopology.NewVacancyKey;

    public async Task HandleAsync(string body, CancellationToken ct)
    {
        var message = JsonSerializer.Deserialize<NewVacancyMessage>(body)
                      ?? throw new InvalidOperationException("Failed to deserialize NewVacancyMessage");
        
        await tgBotClient.SendMessage(
            chatId: message.TelegramId,
            text: $"Задача: {message.TaskId}\n\n{message.Name}\n{message.Url}",
            cancellationToken: ct
        );

        logger.LogInformation("vacancy received, sending to chat {TelegramId}", message.TelegramId);
    }
}