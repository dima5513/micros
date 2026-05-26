using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace Micros.TgBot;

public class TelegramOptions
{
    [ConfigurationKeyName("TELEGRAM_BOT_API_KEY")] [Required] 
    public string ApiKey { get; set; }
}