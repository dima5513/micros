using System.Net.Http.Headers;
using DotNetEnv;
using Micros.Core.api;
using Micros.Core.config;
using Micros.Core.rabbitmq;
using Micros.Scheduler;
using Micros.Scheduler.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.Customizer;
using TickerQ.EntityFrameworkCore.DependencyInjection;

Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddSingleton<RabbitMqConnection>();

builder.Services.AddOptions<PostgresqlOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<SchedulerOptions>().Bind(builder.Configuration.GetSection("Scheduler"));

builder.Services.AddOptions<ApiOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<AppDbContext>((sp, optionsBuilder) =>
{
    var options = sp.GetRequiredService<IOptions<PostgresqlOptions>>().Value;

    var connectionString = new NpgsqlConnectionStringBuilder
    {
        Host = options.Host,
        Port = options.Port,
        Username = options.Username,
        Password = options.Password,
        Database = options.Name,
    };
    
    optionsBuilder.UseNpgsql(connectionString.ToString()).UseSnakeCaseNamingConvention();
});

builder.Services.AddTickerQ(optionsBuilder =>
{
    optionsBuilder.AddOperationalStore(efOptions =>
    {
        efOptions.UseApplicationDbContext<AppDbContext>(ConfigurationType.IgnoreModelCustomizer);
        efOptions.SetSchema("tickerq");
    });
});

builder.Services.AddSingleton<IRabbitMqConsumer, SubscriptionCreateConsumer>();
builder.Services.AddSingleton<IRabbitMqConsumer, SubscriptionDeleteConsumer>();
builder.Services.AddSingleton<IRabbitMqConsumer, SubscriptionUserUpdatedConsumer>();

builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

builder.Services.AddHttpClient<ApiHttpClient>((sp, client) =>
{
    var apiOptions = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
    client.BaseAddress = new Uri(apiOptions.BackendApiUrl);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiOptions.ApiKey);
});

builder.Services.AddHostedService<SubscriptionsBootstrapService>();
builder.Services.AddHostedService<RabbitMqBackgroundHostService>();

var host = builder.Build();

// В Worker/generic host [ModuleInitializer] из source-gen может не выполниться,
// и тогда [TickerFunction]-делегаты (карта "hh-parse" -> делегат) не регистрируются:
// AddAsync с такой функцией молча не персистит, тикеры не срабатывают. Регистрируем явно.
Micros.Scheduler.TickerQInstanceFactoryExtensions.Initialize();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

host.UseTickerQ();

await host.RunAsync();