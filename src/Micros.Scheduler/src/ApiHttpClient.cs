using System.Net.Http.Json;

namespace Micros.Scheduler;

public class ApiHttpClient(HttpClient httpClient, ILogger<ApiHttpClient> logger)
{
    public async Task<List<Subscription>> GetSubscriptionsAsync()
    {
        var subscriptions = await httpClient.GetFromJsonAsync<List<Subscription>>("api/internal/subscriptions");
        logger.LogInformation("subscriptions: {Subscriptions}", subscriptions);

        return subscriptions ?? [];
    }
}
