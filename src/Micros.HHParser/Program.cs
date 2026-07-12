using DotNetEnv;
using Micros.Core.logging;
using Micros.Core.rabbitmq;
using Micros.Core.redis;
using Micros.HHParser;
using Microsoft.Extensions.Options;

Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMicrosLogging(builder.Configuration,"Micros.HHParser");

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
    client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
}).AddHhResilience();

builder.Services.AddHostedService<RabbitMqBackgroundHostService>();

await builder.Build().RunAsync();