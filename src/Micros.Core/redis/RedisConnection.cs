using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Micros.Core.redis;

public class RedisConnection : IAsyncDisposable
{
    private readonly Lazy<Task<IConnectionMultiplexer>> _multiplexer;

    public RedisConnection(IOptions<RedisOptions> options)
    {
        var connectionString = $"{options.Value.Host}:{options.Value.Port}";
        _multiplexer =
            new Lazy<Task<IConnectionMultiplexer>>(async () =>
                await ConnectionMultiplexer.ConnectAsync(connectionString));
    }

    public async Task<IDatabase> GetDatabaseAsync()
    {
        var mux = await _multiplexer.Value;
        return mux.GetDatabase();
    }

    public async ValueTask DisposeAsync()
    {
        if (_multiplexer.IsValueCreated)
            await (await _multiplexer.Value).DisposeAsync();
    }
}