# Redis CLI Quick Reference Guide

This document provides useful Redis commands for working with the EventDress Redis cache.

---

## Accessing the Redis Container

### Enter Redis CLI
```powershell
docker exec -it eventdress-redis redis-cli
```

After entering, authenticate with:
```redis
AUTH MySecureRedisPassword2026!
```

### One-Line Command (with authentication)
```powershell
docker exec -it eventdress-redis redis-cli -a MySecureRedisPassword2026! <COMMAND>
```

---

## Essential Redis Commands

### 1. List All Keys
```redis
KEYS *
```

Example output:
```
1) "categories:all"
2) "dress:123"
```

### 2. Check TTL (Time-to-Live)
```redis
TTL categories:all
```

Returns remaining seconds:
- Positive number: seconds remaining
- `-1`: Key exists but has no expiration
- `-2`: Key does not exist

### 3. Get Cached Value
```redis
GET categories:all
```

Returns the JSON-serialized data.

### 4. Delete Specific Key
```redis
DEL categories:all
```

### 5. Delete All Keys (Clear Cache)
```redis
FLUSHALL
```

**⚠️ Warning**: This deletes ALL data in Redis!

### 6. Check if Key Exists
```redis
EXISTS categories:all
```

Returns `1` if exists, `0` if not.

### 7. Get Key Type
```redis
TYPE categories:all
```

Returns: `string`, `list`, `set`, `hash`, etc.

### 8. View All Keys with Pattern
```redis
KEYS categories:*
```

Finds all keys starting with "categories:"

### 9. Get Multiple Keys
```redis
MGET categories:all dress:1 dress:2
```

### 10. Ping Redis Server
```redis
PING
```

Returns: `PONG` if connected.

---

## Useful Cache Debugging Commands

### Monitor All Commands in Real-Time
```redis
MONITOR
```

Shows every command executed against Redis (press Ctrl+C to stop).

### Get Server Info
```redis
INFO
```

Returns detailed server statistics.

### Get Memory Usage of a Key
```redis
MEMORY USAGE categories:all
```

Returns bytes used by the key.

### View Cache Stats
```redis
INFO stats
```

Shows hit/miss rates and other statistics.

---

## PowerShell Examples (from Windows)

### List all cache keys
```powershell
docker exec -it eventdress-redis redis-cli -a MySecureRedisPassword2026! KEYS *
```

### Check TTL for categories cache
```powershell
docker exec -it eventdress-redis redis-cli -a MySecureRedisPassword2026! TTL categories:all
```

### View cached categories data
```powershell
docker exec -it eventdress-redis redis-cli -a MySecureRedisPassword2026! GET categories:all
```

### Delete categories cache
```powershell
docker exec -it eventdress-redis redis-cli -a MySecureRedisPassword2026! DEL categories:all
```

### Clear ALL cache
```powershell
docker exec -it eventdress-redis redis-cli -a MySecureRedisPassword2026! FLUSHALL
```

---

## Testing Cache Behavior

### 1. Verify Cache Miss → Hit Flow
```powershell
# Step 1: Clear cache
docker exec -it eventdress-redis redis-cli -a MySecureRedisPassword2026! FLUSHALL

# Step 2: Make first request (should be cache MISS)
Invoke-RestMethod -Uri "http://localhost:5216/api/categories" -Method GET -UseBasicParsing

# Step 3: Check if key was created
docker exec -it eventdress-redis redis-cli -a MySecureRedisPassword2026! KEYS *

# Step 4: Check TTL
docker exec -it eventdress-redis redis-cli -a MySecureRedisPassword2026! TTL categories:all

# Step 5: Make second request (should be cache HIT)
Invoke-RestMethod -Uri "http://localhost:5216/api/categories" -Method GET -UseBasicParsing

# Step 6: Check application logs for "Cache HIT" message
```

### 2. Test TTL Expiration
```powershell
# Set a short TTL (modify appsettings.json temporarily)
# "CategoryTTL": 10  (10 seconds instead of 3600)

# Restart application, make request, wait 10 seconds, make another request
# Should see cache MISS after expiration
```

### 3. Test Cache Invalidation
```powershell
# Step 1: Make GET request to populate cache
Invoke-RestMethod -Uri "http://localhost:5216/api/categories" -Method GET -UseBasicParsing

# Step 2: Verify cache exists
docker exec -it eventdress-redis redis-cli -a MySecureRedisPassword2026! EXISTS categories:all

# Step 3: Add new category (triggers invalidation)
Invoke-RestMethod -Uri "http://localhost:5216/api/categories" -Method POST `
  -Body '{"name":"Test Category","description":"Test"}' `
  -ContentType "application/json"

# Step 4: Verify cache was deleted
docker exec -it eventdress-redis redis-cli -a MySecureRedisPassword2026! EXISTS categories:all
# Should return 0 (key deleted)

# Step 5: Next GET will repopulate cache
Invoke-RestMethod -Uri "http://localhost:5216/api/categories" -Method GET -UseBasicParsing
```

---

## Current Cache Configuration

Based on `appsettings.json`:

| Setting | Value | Description |
|---------|-------|-------------|
| **ConnectionString** | `localhost:6379,password=MySecureRedisPassword2026!,abortConnect=false` | Redis connection |
| **Enabled** | `true` | Cache is active |
| **DefaultTTL** | `300` seconds (5 min) | Default expiration |
| **CategoryTTL** | `3600` seconds (1 hour) | Category cache expiration |
| **DressTTL** | `600` seconds (10 min) | Dress cache expiration |
| **ModelTTL** | `900` seconds (15 min) | Model cache expiration |
| **OrderTTL** | `180` seconds (3 min) | Order cache expiration |

---

## Cache Key Patterns

Current implementation uses these key patterns:

| Key | Description | TTL |
|-----|-------------|-----|
| `categories:all` | All categories list | 3600s (1 hour) |

**Future expansions** (when implemented):
- `category:{id}` — Single category by ID
- `dress:{id}` — Single dress by ID
- `model:{id}:sizes` — Available sizes for a model
- `availability:{modelId}:{size}:{date}` — Availability count

---

## Troubleshooting

### Cannot connect to Redis
```powershell
# Check if container is running
docker ps -a --filter "name=eventdress-redis"

# Check container logs
docker logs eventdress-redis

# Restart container
docker restart eventdress-redis
```

### Cache not populating
1. Check application logs for errors
2. Verify `Redis:Enabled` is `true` in appsettings.json
3. Test Redis connection: `docker exec -it eventdress-redis redis-cli -a MySecureRedisPassword2026! PING`

### Cache not invalidating
1. Check application logs for "Invalidated cache" messages
2. Verify the POST/PUT/DELETE methods are calling `_cacheService.RemoveAsync()`
3. Manually delete key and test: `DEL categories:all`

---

## Best Practices

1. **Use KEYS * sparingly in production** — It blocks Redis while scanning
2. **Monitor cache hit rates** — Use `INFO stats` to check effectiveness
3. **Set appropriate TTLs** — Balance freshness vs. performance
4. **Use patterns for invalidation** — `KEYS categories:*` to find related keys
5. **Test cache behavior** — Always verify MISS → HIT → Expiration → Invalidation flows

---

## References

- [Redis Documentation](https://redis.io/docs/)
- [Redis Command Reference](https://redis.io/commands/)
- [StackExchange.Redis GitHub](https://github.com/StackExchange/StackExchange.Redis)
