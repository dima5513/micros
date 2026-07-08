using DotNetEnv;
using Micros.Api.Domains.Authenticate;
using Micros.Api.Domains.Subscription;
using Micros.Api.Domains.User;
using Micros.Api.Infrastructure.Authorize;
using Micros.Api.Infrastructure.Database;
using Micros.Api.Infrastructure.ExceptionHandlers;
using Micros.Api.Infrastructure.Jwt;
using Micros.Api.Infrastructure.OpenApi;
using Micros.Api.Infrastructure.Outbox;
using Micros.Api.Infrastructure.PasswordHash;
using Micros.Api.Infrastructure.RateLimiting;
using Micros.Core.rabbitmq;
using Micros.Core.Types;
using Microsoft.EntityFrameworkCore;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder();

builder.Services.AddCors(options =>
{
    options.AddPolicy("local", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new PatchJsonConverterFactory()));

builder.Services.AddOpenApi(options => { options.AddSchemaTransformer<PatchOpenApiSchemaTransformer>(); });

builder.Services.AddAppDatabase(builder.Configuration);

builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new PatchJsonConverterFactory()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddAppAuthorize(builder.Configuration);

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<PasswordHashService>();

builder.Services.AddScoped<IAuthenticateService, AuthenticateService>();

builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddScoped<IAuthorizeService, AuthorizeService>();

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

builder.Services.AddHostedService<OutboxProcesser>();

builder.Services.AddAppRateLimiter();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCors("local");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/api/docs/{documentName}.json");
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/api/docs/v1.json", "Micros API v1");
        options.RoutePrefix = "api/docs";
    });
}

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers().RequireRateLimiting(AppRateLimiter.BucketPolicy);

app.Run();