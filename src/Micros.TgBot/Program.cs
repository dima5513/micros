using DotNetEnv;
using Micros.Core.rabbitmq;
using Micros.TgBot;
using Microsoft.Extensions.Options;
using Telegram.Bot;

Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<TelegramOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<RabbitMqConnection>();

builder.Services
    .AddHttpClient("tgbot")
    .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
    {
        var opts = sp.GetRequiredService<IOptions<TelegramOptions>>();
        return new TelegramBotClient(opts.Value.ApiKey, httpClient);
    });

builder.Services.AddHostedService<BotBackgroundService>();

builder.Services.AddSingleton<IRabbitMqConsumer, NewHhVacancyConsumer>();

builder.Services.AddHostedService<RabbitMqBackgroundHostService>();

await builder.Build().RunAsync();