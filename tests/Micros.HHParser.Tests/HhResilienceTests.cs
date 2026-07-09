using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using static System.Net.HttpStatusCode;

namespace Micros.HHParser.Tests;

public class HhResilienceTests
{
    [Theory]
    [InlineData(TooManyRequests)]
    [InlineData(InternalServerError)]
    [InlineData(ServiceUnavailable)]
    public async Task Retries_Transient_Failure_Then_Succeeds(HttpStatusCode transient)
    {
        var stub = new SequenceHandler(transient, OK);

        var resp = await Send(stub);

        Assert.Equal(OK, resp.StatusCode);
        Assert.Equal(2, stub.Calls);
    }

    [Theory]
    [InlineData(BadRequest)]
    [InlineData(NotFound)]
    [InlineData(Unauthorized)]
    public async Task Does_Not_Retry_Non_Transient_Failure(HttpStatusCode nonTransient)
    {
        var stub = new SequenceHandler(nonTransient);

        var resp = await Send(stub);

        Assert.Equal(nonTransient, resp.StatusCode);
        Assert.Equal(1, stub.Calls);
    }

    [Fact]
    public async Task Gives_Up_After_Max_Retry_Attempts()
    {
        var stub = new SequenceHandler(ServiceUnavailable);

        var resp = await Send(stub);

        Assert.Equal(ServiceUnavailable, resp.StatusCode);
        Assert.Equal(4, stub.Calls);
    }

    private static async Task<HttpResponseMessage> Send(SequenceHandler handler)
    {
        var (client, time) = BuildClient(handler);

        var pending = client.GetAsync("/search/vacancy");


        for (var i = 0; i < 50 && !pending.IsCompleted; i++)
        {
            time.Advance(TimeSpan.FromSeconds(30));
            await Task.Yield();
        }

        return await pending;
    }

    private static (HttpClient client, FakeTimeProvider time) BuildClient(SequenceHandler handler)
    {
        var time = new FakeTimeProvider();

        var services = new ServiceCollection();

        services
            .AddHttpClient("hh", c => c.BaseAddress = new Uri("https://hh.ru"))
            .AddHhResilience(time)
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        
        var client = services
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient("hh");

        return (client, time);
    }
}