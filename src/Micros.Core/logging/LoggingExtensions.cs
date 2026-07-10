using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Micros.Core.logging;

public static class LoggingExtensions
{
    public static IServiceCollection AddMicrosLogging(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        var seqUrl = configuration["SEQ_URL"];
        
        return services.AddSerilog(config =>
        {
            config
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", serviceName)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] {Message:lj}{NewLine}{Exception}"
                );

            if (!string.IsNullOrWhiteSpace(seqUrl)) config.WriteTo.Seq(seqUrl);

        });
        
    }
}