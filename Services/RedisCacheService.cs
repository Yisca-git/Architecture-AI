using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Services;

/// <summary>
/// Redis implementation of the caching service
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IConfiguration _configuration;
    private readonly bool _cacheEnabled;
    private readonly TimeSpan _defaultTTL;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger,
        IConfiguration configuration)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _logger = logger;
        _configuration = configuration;
        _cacheEnabled = configuration.GetSection("Redis:Enabled").Get<bool?>() ?? true;
        _defaultTTL = TimeSpan.FromSeconds(configuration.GetSection("Redis:DefaultTTL").Get<int?>() ?? 300);
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        if (!_cacheEnabled)
        {
            _logger.LogDebug("Cache is disabled, skipping GET for key: {Key}", key);
            return null;
        }

        try
        {
            var value = await _database.StringGetAsync(key);
            
            if (!value.HasValue)
            {
                _logger.LogInformation("Cache MISS for key: {Key}", key);
                return null;
            }

            _logger.LogInformation("Cache HIT for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache for key: {Key}. Falling back to database.", key);
            return null; // Graceful degradation - return null to force database fetch
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        if (!_cacheEnabled)
        {
            _logger.LogDebug("Cache is disabled, skipping SET for key: {Key}", key);
            return;
        }

        try
        {
            var serializedValue = JsonSerializer.Serialize(value);
            var expiryTime = expiry ?? _defaultTTL;
            
            await _database.StringSetAsync(key, serializedValue, expiryTime);
            _logger.LogInformation("Cached data for key: {Key} with TTL: {TTL} seconds", key, expiryTime.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}. Continuing without caching.", key);
            // Don't throw - graceful degradation
        }
    }

    public async Task RemoveAsync(string key)
    {
        if (!_cacheEnabled)
        {
            _logger.LogDebug("Cache is disabled, skipping REMOVE for key: {Key}", key);
            return;
        }

        try
        {
            await _database.KeyDeleteAsync(key);
            _logger.LogInformation("Removed cache for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key: {Key}", key);
            // Don't throw - cache invalidation failure is not critical
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        if (!_cacheEnabled)
        {
            _logger.LogDebug("Cache is disabled, skipping REMOVE BY PATTERN for: {Pattern}", pattern);
            return;
        }

        try
        {
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                var keys = server.Keys(pattern: pattern);
                
                foreach (var key in keys)
                {
                    await _database.KeyDeleteAsync(key);
                }
            }
            
            _logger.LogInformation("Removed cache entries matching pattern: {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache by pattern: {Pattern}", pattern);
            // Don't throw - cache invalidation failure is not critical
        }
    }
}
