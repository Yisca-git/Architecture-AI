# Running the Application with Docker

This guide explains how to run the Event Dress Rental API with Docker.

## Prerequisites

- Docker Desktop installed and running
- SQL Server accessible (either local or remote)

---

## Quick Start

### **Option 1: Redis Only (Current Setup)**

Run Redis in Docker, API on your machine:

```powershell
# Start Redis
docker-compose up -d redis

# Start API (separate terminal)
cd WebApiShop
dotnet run
```

API will be available at: `http://localhost:5216`

---

### **Option 2: Full Docker Setup (API + Redis)**

Run both Redis and the API in Docker containers:

#### **1. Configure Database Connection**

Create `.env` file in the project root (copy from `.env.example`):

```powershell
Copy-Item .env.example .env
```

Edit `.env` and set your SQL Server connection string:

**For local SQL Server:**
```env
SQL_CONNECTION_STRING=Server=host.docker.internal,1433;Database=EventDressRental;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

**For Azure SQL:**
```env
SQL_CONNECTION_STRING=Server=tcp:yourserver.database.windows.net,1433;Database=EventDressRental;User Id=yourusername;Password=yourpassword;Encrypt=True;
```

> **Note**: `host.docker.internal` allows Docker containers to access services running on your Windows host machine.

#### **2. Build and Run**

```powershell
# Build and start all services
docker-compose up -d --build

# View logs
docker-compose logs -f

# Check running containers
docker ps
```

#### **3. Access the Application**

- **API**: `http://localhost:5216`
- **Swagger**: `http://localhost:5216/swagger` (if enabled)
- **Redis**: `localhost:6379`

#### **4. Test the API**

```powershell
# Get all categories
Invoke-RestMethod -Uri "http://localhost:5216/api/categories" -Method GET -UseBasicParsing

# Check Redis cache
docker exec -it eventdress-redis redis-cli -a MySecureRedisPassword2026! KEYS *
```

---

## Docker Commands Reference

### **Start Services**

```powershell
# Start all services
docker-compose up -d

# Start only Redis
docker-compose up -d redis

# Start with logs visible
docker-compose up
```

### **Stop Services**

```powershell
# Stop all services
docker-compose down

# Stop and remove volumes (⚠️ deletes Redis data)
docker-compose down -v
```

### **Rebuild After Code Changes**

```powershell
# Rebuild API container
docker-compose up -d --build api

# Rebuild all containers
docker-compose up -d --build
```

### **View Logs**

```powershell
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api
docker-compose logs -f redis

# Last 50 lines
docker-compose logs --tail=50 api
```

### **Container Management**

```powershell
# List running containers
docker ps

# Restart a service
docker-compose restart api

# Execute command in container
docker exec -it eventdress-api bash
docker exec -it eventdress-redis redis-cli
```

---

## Configuration

### **Environment Variables**

The API container uses these environment variables (set in `docker-compose.yml`):

| Variable | Description | Example |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production` |
| `ASPNETCORE_URLS` | HTTP listening URL | `http://+:8080` |
| `Redis__ConnectionString` | Redis connection | `redis:6379,password=...` |
| `ConnectionStrings__Home` | SQL Server connection | From `.env` file |

> **Note**: In Docker, the Redis connection uses `redis` (service name) instead of `localhost`.

### **Ports**

| Service | Internal Port | External Port |
|---------|---------------|---------------|
| API | 8080 | 5216 |
| Redis | 6379 | 6379 |

---

## Networking

Containers communicate via `eventdress-network`:

- **API → Redis**: Uses service name `redis:6379`
- **API → SQL Server**: Uses `host.docker.internal` (for local DB) or public endpoint (for cloud DB)
- **External → API**: Uses `localhost:5216`

---

## Troubleshooting

### **"Cannot connect to Redis"**

1. Check Redis is running:
   ```powershell
   docker ps --filter "name=eventdress-redis"
   ```

2. Test Redis connection:
   ```powershell
   docker exec -it eventdress-redis redis-cli -a MySecureRedisPassword2026! PING
   ```

3. Check API logs for errors:
   ```powershell
   docker-compose logs api
   ```

### **"Cannot connect to SQL Server"**

1. Verify connection string in `.env` file
2. Ensure SQL Server allows remote connections
3. For local SQL Server, use `host.docker.internal` instead of `localhost`
4. Check firewall allows port 1433

### **"Port already in use"**

Stop existing services:
```powershell
# Stop containers
docker-compose down

# Or change ports in docker-compose.yml
# ports:
#   - "5217:8080"  # Use 5217 instead of 5216
```

### **Rebuild after code changes not reflecting**

```powershell
# Force rebuild without cache
docker-compose build --no-cache api
docker-compose up -d api
```

---

## Development vs Production

### **Development (Current)**
- Run API with `dotnet run` for hot reload
- Only Redis in Docker
- Faster iteration cycle

### **Production (Docker)**
- Both API and Redis in Docker
- Consistent environment
- Easy deployment
- Better isolation

---

## Advanced: Custom appsettings for Docker

If you prefer file-based configuration over environment variables:

1. Create `WebApiShop/appsettings.Docker.json`:
```json
{
  "ConnectionStrings": {
    "Home": "Server=host.docker.internal,1433;Database=EventDressRental;..."
  },
  "Redis": {
    "ConnectionString": "redis:6379,password=MySecureRedisPassword2026!,abortConnect=false"
  }
}
```

2. Uncomment volume in `docker-compose.yml`:
```yaml
volumes:
  - ./WebApiShop/appsettings.Docker.json:/app/appsettings.Production.json:ro
```

---

## Health Checks

Check service health:

```powershell
# API health (add health check endpoint if needed)
Invoke-RestMethod -Uri "http://localhost:5216/health"

# Redis health
docker exec -it eventdress-redis redis-cli -a MySecureRedisPassword2026! PING
```

---

## Cleanup

Remove all containers, networks, and volumes:

```powershell
# Stop and remove everything
docker-compose down -v

# Remove unused Docker resources
docker system prune -a
```
