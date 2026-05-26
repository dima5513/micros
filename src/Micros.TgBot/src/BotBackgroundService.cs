using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace Micros.TgBot;

public class BotBackgroundService : BackgroundService
{
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<BotBackgroundService> _logger;

    public BotBackgroundService(ITelegramBotClient bot, ILogger<BotBackgroundService> logger)
    {
        _bot = bot;
        _logger = logger;
    }


    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var me = await _bot.GetMe(cancellationToken);
        _logger.LogInformation("Bot @{Username} started", me.Username);
        
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = []
        };

        await _bot.ReceiveAsync(
            receiverOptions: receiverOptions,
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleUpdateAsync(
        ITelegramBotClient client,
        Update update,
        CancellationToken ct)
    {
        if (update.Message is not { Text: { } text } message) return;
        
        switch (text)
        {
            case "/start":
                await client.SendMessage(message.Chat.Id, $"Привет. Я эхо-бот. Напиши что-нибудь в чат.");
                break;
            default:
                await client.SendMessage(message.Chat.Id, $"Echo {text}");
                break;
        }
    }
    

    private Task HandleErrorAsync(
        ITelegramBotClient client,
        Exception ex,
        CancellationToken ct)
    {
        _logger.LogError(ex, "Telegram polling error");
        return Task.CompletedTask;
    }
}