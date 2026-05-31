using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ConsultationApi.Infrastructure.BackgroundServices;

public class RedisCacheInvalidationListener : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IMemoryCache _l1;
    private readonly ILogger<RedisCacheInvalidationListener> _logger;

    private const string InvalidationChannel = "cache:invalidate";

    public RedisCacheInvalidationListener(
        IConnectionMultiplexer redis,
        IMemoryCache l1,
        ILogger<RedisCacheInvalidationListener> logger)
    {
        _redis = redis;
        _l1 = l1;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _redis.GetSubscriber();

        await subscriber.SubscribeAsync(
            RedisChannel.Literal(InvalidationChannel),
            (_, message) =>
            {
                var key = message.ToString();
                _l1.Remove(key);
                _logger.LogDebug("L1 cache evicted key '{Key}' via Pub/Sub invalidation.", key);
            });

        // Keep the subscription alive until the host shuts down.
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
