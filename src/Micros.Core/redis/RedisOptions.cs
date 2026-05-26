using Microsoft.Extensions.Configuration;

namespace Micros.Core.redis;

public class RedisOptions
{
    [ConfigurationKeyName("REDIS_HOST")]
    public string Host { get; set; }
    [ConfigurationKeyName("REDIS_PORT")]
    public int Port { get; set; }
}