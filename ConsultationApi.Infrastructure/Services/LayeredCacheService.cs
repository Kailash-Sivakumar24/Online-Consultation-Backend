using System.Text.Json;
using ConsultationApi.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace ConsultationApi.Infrastructure.Services;

public class LayeredCacheService : ICacheService
{
    private readonly IMemoryCache _l1;
    private readonly IConnectionMultiplexer _redis;

    // L1 TTL is capped at 1 minute so stale reads across restarts are short-lived.
    private static readonly TimeSpan L1MaxTtl = TimeSpan.FromMinutes(1);

    private const string InvalidationChannel = "cache:invalidate";

    public LayeredCacheService(IMemoryCache l1, IConnectionMultiplexer redis)
    {
        _l1 = l1;
        _redis = redis;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        if (_l1.TryGetValue(key, out T? l1Value))
            return l1Value;

        var db = _redis.GetDatabase();
        var redisValue = await db.StringGetAsync(key);
        if (!redisValue.HasValue)
            return default;

        var value = JsonSerializer.Deserialize<T>(redisValue!);
        _l1.Set(key, value, L1MaxTtl);
        return value;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiry)
    {
        _l1.Set(key, value, expiry < L1MaxTtl ? expiry : L1MaxTtl);

        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(value);
        await db.StringSetAsync(key, json, expiry);
    }

    public async Task RemoveAsync(string key)
    {
        _l1.Remove(key);

        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(key);

        // Broadcast to all other instances so they flush their L1 for this key.
        await _redis.GetSubscriber().PublishAsync(
            RedisChannel.Literal(InvalidationChannel), key);
    }
}
