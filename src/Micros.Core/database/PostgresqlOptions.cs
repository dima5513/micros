using Microsoft.Extensions.Configuration;

namespace Micros.Core.config;

using System.ComponentModel.DataAnnotations;

public class PostgresqlOptions
{
    [ConfigurationKeyName("POSTGRES_HOST")] [Required]
    public string Host { get; set; }
    [ConfigurationKeyName("POSTGRES_PORT")] [Required]
    public int Port { get; set; }
    [ConfigurationKeyName("POSTGRES_USER")] [Required]
    public string Username { get; set; }
    
    [ConfigurationKeyName("POSTGRES_PASSWORD")] [Required]
    public string Password { get; set; }
    [ConfigurationKeyName("POSTGRES_DB")] [Required]
    public string Name { get; set; }
}