using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace Micros.Core.rabbitmq;

public class RabbitMqOptions
{
    [ConfigurationKeyName("RABBITMQ_HOST")] [Required] 
    public string Hostname { get; set; }
    [ConfigurationKeyName("RABBITMQ_PORT")] [Required]
    public int Port { get; set; }
    [ConfigurationKeyName("RABBITMQ_DEFAULT_USER")] [Required]
    public string Username { get; set; }
    [ConfigurationKeyName("RABBITMQ_DEFAULT_PASS")] [Required]
    public string Password { get; set; }
}