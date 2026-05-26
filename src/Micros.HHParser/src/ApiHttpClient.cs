using System.Net.Http.Headers;
using System.Net.Http.Json;
using Micros.Core.api;
using Microsoft.Extensions.Options;

namespace Micros.HHParser;

public class ApiHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiHttpClient> _logger;

    public ApiHttpClient(HttpClient httpClient, ILogger<ApiHttpClient> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        
    }

    public async Task<List<Subscription>> GetSubscriptionsAsync()
    {
        var subscriptions =  await _httpClient.GetFromJsonAsync<List<Subscription>>("api/internal/subscriptions");
        _logger.LogInformation("subscriptions: {Subscriptions}", subscriptions);

        return subscriptions ?? [];
    }
}