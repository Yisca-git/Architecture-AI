# Event Dress Rental API - Copilot Onboarding Guide

## Project Overview
**EventDressRental** is an ASP.NET Core Web API for managing dress rentals for events. Handles users, dress inventory, orders, and rental tracking with multiple dress sizes and models across categories.

## Architecture (Layered)
```
DTOs → Entities → Repositories → Services → API Controllers
```
- **Repository Pattern**: `IXRepository` interface + `XRepository` implementation for each entity
- **Service Layer**: Business logic using repositories and AutoMapper for DTO conversion
- **Dependency Injection**: All services in `Program.cs` with scoped lifetime
- **Async-First**: All methods async (`Task`/`Task<T>`)

## Tech Stack
- **.NET 9.0**, **EF Core 9.0.11** (SQL Server)
- **AutoMapper 12.0.0**, **Swagger/OpenAPI 9.0.6**, **NLog 6.1.0**
- **xUnit + Moq** (testing), **zxcvbn-core** (password validation)

## Project Layout
```
DTOs/              Data Transfer Objects (no dependencies)
Entities/          Domain Models (auto-generated, #nullable disable)
Repositories/      Data Access Interface + Implementation + DbContext
Services/          Business Logic + AutoMapping.cs
WebApiShop/        Controllers, Middleware, Config, nlog.config
Tests/             UnitTest (Moq) + IntegrationTest (real DB)
נספחים/             final_skript.sql, connection strings
```

## Code Conventions
**Naming** (enforced by `.editorconfig`):
- Types: `PascalCase` (error) | Methods: `PascalCase` (warning)
- Parameters/locals: `camelCase` | Private fields: `_camelCase`

**Standard Repository Methods:**
```csharp
Task<T?> GetById(int id)              // → null if not found
Task<List<T>> GetAll()
Task<bool> IsExistsById(int id)
Task<T> Add(T entity)                 // SaveChangesAsync required
Task Update(T entity)                 // SaveChangesAsync required
Task Delete(int id)
```

**Service Layer Rules:**
- Constructor inject: repository, `IMapper`, other services
- Use `_mapper.Map<Source, Dest>()` for conversions
- Return DTOs only, never entities
- Always null-check before returning

**Critical Rules:**
1. **Async/await always** - never use `Task.Result` or sync calls
2. **SaveChangesAsync() required** after Add/Update/Delete in repositories
3. **AutoMapper profiles** in `Services/AutoMapping.cs` with `.ReverseMap()`
4. **Entities are auto-generated** - use EF Core Power Tools for schema updates (don't edit)
5. **DTOs only in API** - never expose entities to controllers

## Setup & Build
```powershell
dotnet build EventDressRental.sln              # Build
dotnet test Tests/Tests.csproj                 # Test (unit + integration)
dotnet run --project WebApiShop/EventDressRental.csproj  # Run API
```

**Prerequisites:** .NET 9.0 SDK, SQL Server, connection string in `appsettings.Development.json`

**Database:** Execute `נספחים/final_skript.sql` then verify "Home" connection string exists

**API Access:** `http://localhost:5000/swagger` (Swagger UI), `/openapi/v1.json` (OpenAPI spec)

## Adding a New Entity
1. **Entity**: Auto-generate via EF Core Power Tools
2. **DTOs**: Create in `DTOs/` (e.g., `NewEntityDTO.cs`, `NewNewEntityDTO.cs`)
3. **Repository**: `INewEntityRepository` interface + `NewEntityRepository` impl + add DbSet to context
4. **Service**: `INewEntityService` interface + `NewEntityService` with DI(repository, mapper)
5. **Register DI** in `Program.cs`: `AddScoped<INewEntityRepository, NewEntityRepository>()`
6. **AutoMapper**: Add to `Services/AutoMapping.cs`: `CreateMap<NewEntity, NewEntityDTO>().ReverseMap()`
7. **Controller**: Create in `WebApiShop/Controllers/`
8. **Tests**: Unit tests (Moq context) in `TestRepository/UnitTest`, integration tests in `IntegrationTest`

## Testing Pattern
**Unit Tests** (mocks):
```csharp
var mock = new Mock<EventDressRentalContext>();
mock.Setup(x => x.Users).ReturnsDbSet(new List<User> { ... });
var repo = new UserRepository(mock.Object);
```

**Integration Tests** (real DB via `DatabaseFixture`):
```csharp
public class Tests : IClassFixture<DatabaseFixture>
{
    public Tests(DatabaseFixture fixture) => _context = fixture.Context;
}
```

## Middleware & Logging
- **ErrorHandlingMiddleware**: Catches exceptions, returns 500, logs via NLog
- **RatingMiddleware**: Tracks all requests (Host, Method, Path, etc.) → RATING table
- **Logging**: Use `ILogger<T>` in controllers, config in `nlog.config`, log login/registration/errors

## Key Notes
1. UTF-8-BOM enforced (`.editorconfig`)
2. CORS allows only `http://localhost:4200` (Angular) - update in `Program.cs` if needed
3. Connection string "Home" vs "School" in config - use "Home"
4. Swagger enabled in Development mode only
5. See **[repository-layer-instructions.md](.github/repository-layer-instructions.md)** for detailed Repository patterns

## Quick Reference
- **Build**: `dotnet build EventDressRental.sln`
- **Swagger**: `http://localhost:5000/swagger` (after running)
- **Config**: `WebApiShop/appsettings.Development.json`
- **Repository Details**: See [repository-layer-instructions.md](.github/repository-layer-instructions.md)
- **Test DB**: Connection in `Tests/TestRepository/IntegrationTest/DataBaseFixture.cs`
