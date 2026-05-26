using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace Micros.Core.api;

public class S2sOptions
{
    [ConfigurationKeyName("S2S_API_KEY")]
    [Required]
    public string ApiKey { get; set; }
}
