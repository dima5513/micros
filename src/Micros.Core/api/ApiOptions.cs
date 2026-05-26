using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace Micros.Core.api;

public class ApiOptions
{
    [ConfigurationKeyName("BACKEND_URL")]
    [Required]
    public string BackendApiUrl { get; set; }

    [ConfigurationKeyName("S2S_API_KEY")]
    [Required]
    public string ApiKey { get; set; }
}
