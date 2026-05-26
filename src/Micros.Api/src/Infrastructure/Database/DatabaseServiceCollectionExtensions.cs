using Micros.Core.config;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Micros.Api.Infrastructure.Database;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddAppDatabase(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PostgresqlOptions>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        services.AddDbContext<AppDbContext>((sp, opt) =>
        {
            var options = sp.GetRequiredService<IOptions<PostgresqlOptions>>().Value;

            var connectionString = new NpgsqlConnectionStringBuilder
            {
                Host = options.Host,
                Port = options.Port,
                Username = options.Username,
                Password = options.Password,
                Database = options.Name,
            }.ToString();

            opt.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
        });

        return services;
    }
}