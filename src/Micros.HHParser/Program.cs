using DotNetEnv;
using Micros.Core.rabbitmq;
using Micros.Core.redis;
using Micros.HHParser;
using Microsoft.Extensions.Options;

Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<HhVacancyParserOptions>()
    .Bind(builder.Configuration.GetSection("HHParser"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<RedisOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<RedisConnection>();

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

builder.Services.AddSingleton<IRabbitMqConsumer, HhParseRequestedConsumer>();

builder.Services.AddHttpClient<HhVacancyHttpApiClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<HhVacancyParserOptions>>().Value;
    client.BaseAddress = new Uri(options.HhHost);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
});

builder.Services.AddHostedService<RabbitMqBackgroundHostService>();

await builder.Build().RunAsync();