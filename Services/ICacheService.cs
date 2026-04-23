namespace Services;

/// <summary>
/// Interface for distributed caching operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get a cached value by key
    /// </summary>
    /// <typeparam name="T">Type of the cached object</typeparam>
    /// <param name="key">Cache key</param>
    /// <returns>Cached value or null if not found</returns>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Set a value in cache with optional expiration
    /// </summary>
    /// <typeparam name="T">Type of object to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expiry">Optional expiration time (uses default if not provided)</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;

    /// <summary>
    /// Remove a cached value by key
    /// </summary>
    /// <param name="key">Cache key to remove</param>
    Task RemoveAsync(string key);

    /// <summary>
    /// Remove all cached values matching a pattern (e.g., "categories:*")
    /// </summary>
    /// <param name="pattern">Pattern to match keys for removal</param>
    Task RemoveByPatternAsync(string pattern);
}
