# SmreaderAPI — Technical Specification

| Field              | Value                                                              |
| ------------------ | ------------------------------------------------------------------ |
| **Project**        | SmreaderAPI                                                        |
| **Version**        | 1.0.0                                                              |
| **Target**         | .NET 10.0 (Preview)                                                |
| **Database**       | MySQL 8.x                                                          |
| **ORM**            | Dapper (micro-ORM) + LINQ expression-to-SQL converter              |
| **Auth**           | JWT Bearer + Refresh Token Rotation                                |
| **Architecture**   | Clean Architecture (Domain → Application → Infrastructure → API)   |
| **Caching**        | L1 In-Memory + L2 Redis                                           |
| **Logging**        | Serilog (Console + File sinks)                                     |
| **Rate Limiting**  | AspNetCoreRateLimit (IP-based)                                     |
| **Testing**        | xUnit + Moq + FluentAssertions                                    |
| **CI/CD**          | GitHub Actions → IIS on VM                                         |
| **Containerization** | Docker (multi-stage) + docker-compose                            |
| **Date**           | April 25, 2026                                                     |

---

## 1. Solution Structure

```
SmreaderAPI/
├── SmreaderAPI.sln
├── Dockerfile
├── docker-compose.yml
├── docker-compose.override.yml
├── .dockerignore
├── .env.example
├── .github/
│   └── workflows/
│       ├── ci.yml
│       ├── cd.yml
│       └── pr-checks.yml
├── scripts/
│   └── init.sql
├── src/
│   ├── SmreaderAPI.Domain/
│   │   ├── Entities/
│   │   │   ├── BaseEntity.cs
│   │   │   ├── User.cs
│   │   │   ├── Role.cs
│   │   │   ├── RefreshToken.cs
│   │   │   └── AuditLog.cs
│   │   └── Interfaces/
│   │       ├── IRepository.cs
│   │       ├── IUserRepository.cs
│   │       ├── IRoleRepository.cs
│   │       ├── IRefreshTokenRepository.cs
│   │       ├── IAuditLogRepository.cs
│   │       └── IUnitOfWork.cs
│   ├── SmreaderAPI.Application/
│   │   ├── DTOs/
│   │   │   ├── UserDto.cs
│   │   │   ├── AuthDtos.cs
│   │   │   ├── RoleDto.cs
│   │   │   ├── AuditLogDto.cs
│   │   │   └── ApiResponse.cs
│   │   ├── Interfaces/
│   │   │   ├── IUserService.cs
│   │   │   ├── IAuthService.cs
│   │   │   ├── IAuditService.cs
│   │   │   └── ICacheService.cs
│   │   ├── Mappings/
│   │   │   └── MappingExtensions.cs
│   │   └── Services/
│   │       ├── UserService.cs
│   │       └── AuditService.cs
│   ├── SmreaderAPI.Infrastructure/
│   │   ├── Data/
│   │   │   ├── DapperContext.cs
│   │   │   └── ExpressionToSqlConverter.cs
│   │   ├── Repositories/
│   │   │   ├── GenericRepository.cs
│   │   │   ├── UserRepository.cs
│   │   │   ├── RoleRepository.cs
│   │   │   ├── RefreshTokenRepository.cs
│   │   │   └── AuditLogRepository.cs
│   │   ├── UnitOfWork/
│   │   │   └── UnitOfWork.cs
│   │   ├── Services/
│   │   │   └── AuthService.cs
│   │   ├── Caching/
│   │   │   └── CacheService.cs
│   │   └── Logging/
│   │       └── SerilogRequestEnricher.cs
│   └── SmreaderAPI.API/
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── appsettings.Staging.json
│       ├── appsettings.Production.json
│       ├── web.config
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── UsersController.cs
│       │   ├── RolesController.cs
│       │   └── AuditLogController.cs
│       └── Middleware/
│           └── ExceptionMiddleware.cs
└── tests/
    └── SmreaderAPI.UnitTests/
        ├── SmreaderAPI.UnitTests.csproj
        ├── Repositories/
        │   ├── GenericRepositoryTests.cs
        │   └── UserRepositoryTests.cs
        ├── Services/
        │   ├── UserServiceTests.cs
        │   ├── AuthServiceTests.cs
        │   ├── AuditServiceTests.cs
        │   └── CacheServiceTests.cs
        ├── Controllers/
        │   ├── AuthControllerTests.cs
        │   └── UsersControllerTests.cs
        └── Infrastructure/
            └── ExpressionToSqlConverterTests.cs
```

---

## 2. Project Dependencies

### 2.1 Project References

| Project                          | References                                |
| -------------------------------- | ----------------------------------------- |
| `SmreaderAPI.Domain`             | *(none)*                                  |
| `SmreaderAPI.Application`        | `SmreaderAPI.Domain`                      |
| `SmreaderAPI.Infrastructure`     | `SmreaderAPI.Application`                 |
| `SmreaderAPI.API`                | `SmreaderAPI.Infrastructure`, `SmreaderAPI.Application` |
| `SmreaderAPI.UnitTests`          | `SmreaderAPI.Domain`, `SmreaderAPI.Application`, `SmreaderAPI.Infrastructure`, `SmreaderAPI.API` |

### 2.2 NuGet Packages

| Project           | Package                                            | Purpose                              |
| ----------------- | -------------------------------------------------- | ------------------------------------ |
| **Domain**        | *(none)*                                           |                                      |
| **Application**   | `FluentValidation`                                 | DTO validation                       |
| **Infrastructure**| `Dapper`                                           | Micro-ORM                            |
|                   | `MySqlConnector`                                   | MySQL ADO.NET driver                 |
|                   | `Microsoft.Extensions.Configuration.Abstractions`  | Config access                        |
|                   | `System.IdentityModel.Tokens.Jwt`                  | JWT generation/validation            |
|                   | `Microsoft.AspNetCore.Authentication.JwtBearer`    | JWT middleware                       |
|                   | `BCrypt.Net-Next`                                  | Password hashing                     |
|                   | `Microsoft.Extensions.Caching.Memory`              | L1 in-memory cache                   |
|                   | `Microsoft.Extensions.Caching.StackExchangeRedis`  | L2 Redis cache                       |
|                   | `Serilog.AspNetCore`                               | Structured logging                   |
|                   | `Serilog.Sinks.Console`                            | Console output                       |
|                   | `Serilog.Sinks.File`                               | Rolling file output                  |
|                   | `Serilog.Enrichers.Environment`                    | MachineName enrichment               |
|                   | `Serilog.Enrichers.Thread`                         | ThreadId enrichment                  |
|                   | `AspNetCoreRateLimit`                              | IP-based rate limiting               |
| **API**           | `Swashbuckle.AspNetCore`                           | Swagger/OpenAPI                      |
|                   | `Microsoft.Extensions.Diagnostics.HealthChecks`    | Health check endpoint                |
| **UnitTests**     | `xunit`                                            | Test framework                       |
|                   | `Moq`                                              | Mocking                              |
|                   | `FluentAssertions`                                 | Assertion library                    |
|                   | `Microsoft.NET.Test.Sdk`                           | Test host                            |

---

## 3. Domain Layer

### 3.1 Base Entity

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### 3.2 Entities

#### User

| Property       | Type       | Constraints                        |
| -------------- | ---------- | ---------------------------------- |
| `Id`           | `int`      | PK, auto-increment                 |
| `Name`         | `string`   | Required, max 100                  |
| `Email`        | `string`   | Required, max 255, unique index    |
| `PasswordHash` | `string`   | Required, BCrypt hash              |
| `RoleId`       | `int`      | FK → Roles.Id                      |
| `CreatedAt`    | `DateTime` | Default: UTC now                   |
| `UpdatedAt`    | `DateTime?`| Nullable                           |
| `IsActive`     | `bool`     | Default: true                      |

#### Role

| Property      | Type       | Constraints                        |
| ------------- | ---------- | ---------------------------------- |
| `Id`          | `int`      | PK, auto-increment                 |
| `Name`        | `string`   | Required, max 50, unique           |
| `Description` | `string?`  | Optional, max 255                  |
| `CreatedAt`   | `DateTime` | Default: UTC now                   |

#### RefreshToken

| Property     | Type        | Constraints                        |
| ------------ | ----------- | ---------------------------------- |
| `Id`         | `int`       | PK, auto-increment                 |
| `UserId`     | `int`       | FK → Users.Id                      |
| `Token`      | `string`    | Required, max 512, indexed         |
| `ExpiresAt`  | `DateTime`  | Required                           |
| `CreatedAt`  | `DateTime`  | Default: UTC now                   |
| `RevokedAt`  | `DateTime?` | Nullable                           |
| `IsRevoked`  | `bool`      | Default: false                     |

#### AuditLog

| Property     | Type        | Constraints                        |
| ------------ | ----------- | ---------------------------------- |
| `Id`         | `int`       | PK, auto-increment                 |
| `UserId`     | `int?`      | FK → Users.Id, nullable            |
| `Action`     | `string`    | Required, max 100                  |
| `EntityName` | `string`    | Required, max 100                  |
| `EntityId`   | `int?`      | Nullable                           |
| `Timestamp`  | `DateTime`  | Default: UTC now                   |
| `Details`    | `string?`   | Optional, TEXT                     |

### 3.3 Repository Interfaces

#### IRepository\<T\> (Generic)

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<int> AddAsync(T entity);
    Task<int> UpdateAsync(T entity);
    Task<int> DeleteAsync(int id);
    Task<IEnumerable<T>> QueryAsync(string sql, object? param = null);
}
```

| Method        | Description                                                                 |
| ------------- | --------------------------------------------------------------------------- |
| `GetByIdAsync`| SELECT by primary key                                                       |
| `GetAllAsync` | SELECT all rows from entity table                                           |
| `FindAsync`   | Converts LINQ `Expression<Func<T, bool>>` to parameterized SQL WHERE clause|
| `AddAsync`    | INSERT, returns new row ID                                                  |
| `UpdateAsync` | UPDATE by ID, returns affected row count                                    |
| `DeleteAsync` | DELETE by ID, returns affected row count                                    |
| `QueryAsync`  | Execute raw Dapper SQL with optional parameters                             |

#### Entity-Specific Repositories

| Interface                  | Additional Methods                                                  |
| -------------------------- | ------------------------------------------------------------------- |
| `IUserRepository`          | `GetByEmailAsync(string email)`, `GetByRoleAsync(int roleId)`      |
| `IRoleRepository`          | `GetByNameAsync(string name)`                                       |
| `IRefreshTokenRepository`  | `GetByTokenAsync(string token)`, `RevokeTokenAsync(int id)`, `GetActiveTokensByUserAsync(int userId)` |
| `IAuditLogRepository`      | `GetByEntityAsync(string entityName, int entityId)`, `GetByUserIdAsync(int userId)` |

### 3.4 IUnitOfWork

```csharp
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IAuditLogRepository AuditLogs { get; }

    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
```

- Single `IDbConnection` + `IDbTransaction` shared across all repositories
- Repositories are lazy-initialized
- `Dispose()` cleans up connection and transaction

---

## 4. Application Layer

### 4.1 DTOs

#### UserDto / CreateUserDto / UpdateUserDto

```csharp
public record UserDto(int Id, string Name, string Email, string RoleName, bool IsActive, DateTime CreatedAt);
public record CreateUserDto(string Name, string Email, string Password, int RoleId);
public record UpdateUserDto(string? Name, string? Email, int? RoleId, bool? IsActive);
```

#### AuthDtos

```csharp
public record LoginDto(string Email, string Password);
public record RegisterDto(string Name, string Email, string Password);
public record TokenResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public record RefreshTokenRequestDto(string RefreshToken);
```

#### RoleDto / AuditLogDto

```csharp
public record RoleDto(int Id, string Name, string? Description);
public record CreateRoleDto(string Name, string? Description);
public record AuditLogDto(int Id, int? UserId, string Action, string EntityName, int? EntityId, DateTime Timestamp, string? Details);
```

#### ApiResponse\<T\>

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
}
```

### 4.2 Service Interfaces

#### IUserService

```csharp
public interface IUserService
{
    Task<ApiResponse<TokenResponseDto>> RegisterAsync(RegisterDto dto);
    Task<ApiResponse<TokenResponseDto>> LoginAsync(LoginDto dto);
    Task<ApiResponse<TokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto);
    Task<ApiResponse<UserDto>> GetByIdAsync(int id);
    Task<ApiResponse<IEnumerable<UserDto>>> GetAllAsync();
    Task<ApiResponse<UserDto>> UpdateAsync(int id, UpdateUserDto dto);
    Task<ApiResponse<bool>> DeleteAsync(int id);
}
```

#### IAuthService

```csharp
public interface IAuthService
{
    string GenerateJwtToken(User user, string roleName);
    string GenerateRefreshToken();
    Task<bool> ValidateRefreshTokenAsync(string token);
}
```

#### IAuditService

```csharp
public interface IAuditService
{
    Task LogActionAsync(int? userId, string action, string entityName, int? entityId, string? details = null);
    Task<ApiResponse<IEnumerable<AuditLogDto>>> GetByUserAsync(int userId);
    Task<ApiResponse<IEnumerable<AuditLogDto>>> GetAllAsync();
}
```

#### ICacheService

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
}
```

### 4.3 Mapping

Manual extension methods — no AutoMapper dependency:

```csharp
public static class MappingExtensions
{
    public static UserDto ToDto(this User user, string roleName) => ...;
    public static AuditLogDto ToDto(this AuditLog log) => ...;
    public static RoleDto ToDto(this Role role) => ...;
}
```

### 4.4 UserService (Application — pure orchestration)

Depends only on interfaces: `IUnitOfWork`, `IAuthService`, `ICacheService`, `ILogger<T>`.
No external library dependencies — all infrastructure concerns are abstracted behind interfaces.

| Method            | Behavior                                                                           |
| ----------------- | ---------------------------------------------------------------------------------- |
| `RegisterAsync`   | Check duplicate email → hash password → insert User → generate tokens → audit log  |
| `LoginAsync`      | Find by email → verify BCrypt hash → generate tokens → store refresh token → audit |
| `RefreshTokenAsync` | Validate token → revoke old → generate new pair → store new refresh token        |
| `GetByIdAsync`    | Check cache → query DB → populate cache → return DTO                               |
| `GetAllAsync`     | Query DB → return DTOs                                                             |
| `UpdateAsync`     | Begin transaction → update → invalidate cache → commit → audit log                 |
| `DeleteAsync`     | Begin transaction → delete → invalidate cache → commit → audit log                 |

### 4.5 AuditService (Application — pure orchestration)

Depends only on interfaces: `IUnitOfWork`, `ILogger<T>`.

| Method            | Behavior                                                       |
| ----------------- | -------------------------------------------------------------- |
| `LogActionAsync`  | Insert AuditLog entry via `IUnitOfWork.AuditLogs.AddAsync`     |
| `GetByUserAsync`  | Query audit logs by UserId → return DTOs                       |
| `GetAllAsync`     | Query all audit logs → return DTOs                             |

---

## 5. Infrastructure Layer

### 5.1 DapperContext

```csharp
public class DapperContext
{
    private readonly string _connectionString;

    public DapperContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public IDbConnection CreateConnection() => new MySqlConnection(_connectionString);
}
```

### 5.2 ExpressionToSqlConverter

Converts `Expression<Func<T, bool>>` to a parameterized SQL `WHERE` clause.

**Supported operations:**

| Expression Type        | SQL Output                         | Example                                       |
| ---------------------- | ---------------------------------- | --------------------------------------------- |
| `==`                   | `Column = @p0`                     | `x => x.Email == "a@b.com"`                   |
| `!=`                   | `Column != @p0`                    | `x => x.Email != "a@b.com"`                   |
| `>`, `<`, `>=`, `<=`  | `Column > @p0`                     | `x => x.Id > 5`                               |
| `&&`                   | `(Left) AND (Right)`               | `x => x.IsActive && x.RoleId == 1`            |
| `||`                   | `(Left) OR (Right)`                | `x => x.RoleId == 1 \|\| x.RoleId == 2`      |
| `Contains`             | `Column LIKE @p0` (`%value%`)      | `x => x.Name.Contains("john")`                |
| `StartsWith`           | `Column LIKE @p0` (`value%`)       | `x => x.Name.StartsWith("J")`                 |
| `EndsWith`             | `Column LIKE @p0` (`%value`)       | `x => x.Name.EndsWith("son")`                 |
| `== null`              | `Column IS NULL`                   | `x => x.UpdatedAt == null`                     |
| `!= null`              | `Column IS NOT NULL`               | `x => x.UpdatedAt != null`                     |
| Boolean member         | `Column = @p0` (value: true)       | `x => x.IsActive`                             |

**Returns:** `(string WhereClause, DynamicParameters Parameters)`

All values are parameterized — no string concatenation for SQL injection prevention.

### 5.3 GenericRepository\<T\>

| Method        | Implementation                                                                  |
| ------------- | ------------------------------------------------------------------------------- |
| `GetByIdAsync`| `SELECT * FROM {table} WHERE Id = @Id`                                         |
| `GetAllAsync` | `SELECT * FROM {table}`                                                         |
| `FindAsync`   | `SELECT * FROM {table} WHERE {ExpressionToSqlConverter.Convert(predicate)}`     |
| `AddAsync`    | `INSERT INTO {table} (...) VALUES (...); SELECT LAST_INSERT_ID();`              |
| `UpdateAsync` | `UPDATE {table} SET ... WHERE Id = @Id`                                        |
| `DeleteAsync` | `DELETE FROM {table} WHERE Id = @Id`                                           |
| `QueryAsync`  | Pass-through to `Dapper.QueryAsync<T>(sql, param)`                              |

- Table name resolved from `[Table("name")]` attribute or `typeof(T).Name + "s"` convention
- Uses shared `IDbConnection` + `IDbTransaction` from UnitOfWork
- Column mapping uses Dapper's default (property name = column name)

### 5.4 UnitOfWork

```
┌──────────────────────────────────────────────────┐
│                   UnitOfWork                      │
│                                                   │
│  IDbConnection ──────────────────────────────┐    │
│  IDbTransaction ─────────────────────────┐   │    │
│                                          │   │    │
│  ┌─────────────┐  ┌──────────────┐       │   │    │
│  │UserRepository│  │RoleRepository│  ... (shared) │
│  └─────────────┘  └──────────────┘               │
│                                                   │
│  BeginTransactionAsync()                          │
│  CommitAsync()                                    │
│  RollbackAsync()                                  │
│  Dispose()                                        │
└──────────────────────────────────────────────────┘
```

- Scoped lifetime (per HTTP request)
- Repositories lazy-initialized with shared connection/transaction
- `BeginTransactionAsync()`: opens connection, begins transaction
- `CommitAsync()`: commits transaction
- `RollbackAsync()`: rolls back transaction
- `Dispose()`: disposes transaction + connection

### 5.5 AuthService (Infrastructure — external library dependency)

Kept in Infrastructure because it depends on `System.IdentityModel.Tokens.Jwt` and `BCrypt.Net-Next`.

| Method                  | Behavior                                                                  |
| ----------------------- | ------------------------------------------------------------------------- |
| `GenerateJwtToken`      | Creates JWT with claims: `UserId`, `Email`, `Role`. Configurable expiry  |
| `GenerateRefreshToken`  | Generates 64-byte cryptographically random Base64 string                  |
| `ValidateRefreshTokenAsync` | Checks token exists, not revoked, not expired                         |

**JWT Claims:**

| Claim                          | Value                    |
| ------------------------------ | ------------------------ |
| `ClaimTypes.NameIdentifier`    | User.Id                  |
| `ClaimTypes.Email`             | User.Email               |
| `ClaimTypes.Role`              | Role.Name                |
| `ClaimTypes.Name`              | User.Name                |

**Password Hashing:** BCrypt with work factor 12.

### 5.6 CacheService (Infrastructure — external library dependency)

Kept in Infrastructure because it depends on `IMemoryCache` and `IDistributedCache` (Redis).

```
Request → L1 (IMemoryCache, 1-5 min TTL)
              ↓ miss
          L2 (Redis IDistributedCache, 15-30 min TTL)
              ↓ miss
          Database (via repository)
              ↓ result
          Write to L1 + L2 (write-through)
```

| Method              | Behavior                                                      |
| ------------------- | ------------------------------------------------------------- |
| `GetAsync<T>`       | Check L1 → L2 → return null if both miss                     |
| `SetAsync<T>`       | Write to both L1 (short TTL) and L2 (long TTL)               |
| `RemoveAsync`       | Remove from both L1 and L2                                    |
| `RemoveByPrefixAsync` | Remove all keys matching prefix from both layers           |

Serialization: `System.Text.Json` for Redis (de)serialization.

---

## 6. API Layer

### 6.1 Endpoints

#### Auth — `AuthController`

| Method | Route                      | Auth     | Request Body           | Response               |
| ------ | -------------------------- | -------- | ---------------------- | ---------------------- |
| POST   | `/api/auth/register`       | Anonymous| `RegisterDto`          | `ApiResponse<TokenResponseDto>` (201) |
| POST   | `/api/auth/login`          | Anonymous| `LoginDto`             | `ApiResponse<TokenResponseDto>` (200) |
| POST   | `/api/auth/refresh-token`  | Anonymous| `RefreshTokenRequestDto`| `ApiResponse<TokenResponseDto>` (200) |

#### Users — `UsersController`

| Method | Route                | Auth         | Request Body    | Response                       |
| ------ | -------------------- | ------------ | --------------- | ------------------------------ |
| GET    | `/api/users`         | `[Authorize]`| —               | `ApiResponse<IEnumerable<UserDto>>` |
| GET    | `/api/users/{id}`    | `[Authorize]`| —               | `ApiResponse<UserDto>`         |
| PUT    | `/api/users/{id}`    | `[Authorize]`| `UpdateUserDto` | `ApiResponse<UserDto>`         |
| DELETE | `/api/users/{id}`    | `[Authorize]`| —               | `ApiResponse<bool>`            |

#### Roles — `RolesController`

| Method | Route                | Auth                      | Request Body    | Response                       |
| ------ | -------------------- | ------------------------- | --------------- | ------------------------------ |
| GET    | `/api/roles`         | `[Authorize(Roles="Admin")]` | —            | `ApiResponse<IEnumerable<RoleDto>>` |
| GET    | `/api/roles/{id}`    | `[Authorize(Roles="Admin")]` | —            | `ApiResponse<RoleDto>`         |
| POST   | `/api/roles`         | `[Authorize(Roles="Admin")]` | `CreateRoleDto`| `ApiResponse<RoleDto>`         |
| PUT    | `/api/roles/{id}`    | `[Authorize(Roles="Admin")]` | `CreateRoleDto`| `ApiResponse<RoleDto>`         |
| DELETE | `/api/roles/{id}`    | `[Authorize(Roles="Admin")]` | —            | `ApiResponse<bool>`            |

#### Audit Logs — `AuditLogController`

| Method | Route                         | Auth                      | Response                              |
| ------ | ----------------------------- | ------------------------- | ------------------------------------- |
| GET    | `/api/auditlogs`              | `[Authorize(Roles="Admin")]` | `ApiResponse<IEnumerable<AuditLogDto>>` |
| GET    | `/api/auditlogs/user/{userId}`| `[Authorize(Roles="Admin")]` | `ApiResponse<IEnumerable<AuditLogDto>>` |

#### Health — built-in

| Method | Route      | Auth     | Response                                  |
| ------ | ---------- | -------- | ----------------------------------------- |
| GET    | `/health`  | Anonymous| `Healthy` / `Unhealthy` (MySQL + Redis)   |

### 6.2 Middleware Pipeline

```
Request
  → ExceptionMiddleware (global try/catch → ApiResponse with 500)
  → Serilog Request Logging
  → SerilogRequestEnricher (add UserId + IP to log context)
  → Rate Limiting (AspNetCoreRateLimit)
  → CORS
  → Authentication (JWT Bearer)
  → Authorization
  → Controller Action
Response
```

### 6.3 ExceptionMiddleware

Catches all unhandled exceptions and returns a consistent `ApiResponse`:

```json
{
  "success": false,
  "message": "An unexpected error occurred.",
  "data": null,
  "errors": ["Detailed error in Development only"]
}
```

- **Development**: includes exception details + stack trace in `errors`
- **Production**: generic message only — no sensitive data leaked

### 6.4 Dependency Injection Registration

| Service                    | Lifetime | Implementation          | Assembly          |
| -------------------------- | -------- | ----------------------- | ----------------- |
| `DapperContext`            | Singleton| `DapperContext`          | Infrastructure    |
| `IUnitOfWork`              | Scoped   | `UnitOfWork`            | Infrastructure    |
| `IUserRepository`          | Scoped   | `UserRepository`        | Infrastructure    |
| `IRoleRepository`          | Scoped   | `RoleRepository`        | Infrastructure    |
| `IRefreshTokenRepository`  | Scoped   | `RefreshTokenRepository`| Infrastructure    |
| `IAuditLogRepository`      | Scoped   | `AuditLogRepository`    | Infrastructure    |
| `IUserService`             | Scoped   | `UserService`           | **Application**   |
| `IAuthService`             | Scoped   | `AuthService`           | Infrastructure    |
| `IAuditService`            | Scoped   | `AuditService`          | **Application**   |
| `ICacheService`            | Singleton| `CacheService`          | Infrastructure    |

---

## 7. Cross-Cutting Concerns

### 7.1 Rate Limiting (AspNetCoreRateLimit)

| Rule Set        | Endpoint Pattern         | Limit              | Period   |
| --------------- | ------------------------ | ------------------- | -------- |
| General         | `*`                      | 100 requests        | 1 minute |
| Auth (strict)   | `/api/auth/*`            | 10 requests         | 1 minute |
| Health (exempt)  | `/health`               | Whitelisted         | —        |

- IP-based rate limiting via `IpRateLimitOptions`
- Rate limit counters backed by Redis (`IDistributedCache`)
- Response headers: `X-Rate-Limit-Limit`, `X-Rate-Limit-Remaining`, `X-Rate-Limit-Reset`
- HTTP 429 Too Many Requests when limit exceeded

### 7.2 Logging (Serilog)

**Sinks:**

| Sink    | Format                  | Config                                     |
| ------- | ----------------------- | ------------------------------------------ |
| Console | Structured, colored     | `outputTemplate` with timestamp + level    |
| File    | Structured, JSON        | Rolling daily, path: `logs/log-{Date}.txt` |

**Enrichers:**

| Enricher          | Log Property   |
| ----------------- | -------------- |
| `RequestId`       | `RequestId`    |
| `MachineName`     | `MachineName`  |
| `ThreadId`        | `ThreadId`     |
| `SerilogRequestEnricher` | `UserId`, `ClientIp` |

**Log Levels:** Configured per namespace in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "Microsoft.AspNetCore": "Warning"
      }
    }
  }
}
```

### 7.3 CORS

| Environment  | Policy                                                  |
| ------------ | ------------------------------------------------------- |
| Development  | Allow all origins, methods, headers                     |
| Production   | Restrict to origins from `appsettings.json` → `Cors:AllowedOrigins` |

- `AllowCredentials()` enabled for JWT cookie support
- Exposed headers: `X-Rate-Limit-*`

---

## 8. Database Schema

### 8.1 Tables

```sql
CREATE TABLE Roles (
    Id          INT AUTO_INCREMENT PRIMARY KEY,
    Name        VARCHAR(50) NOT NULL UNIQUE,
    Description VARCHAR(255),
    CreatedAt   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Users (
    Id           INT AUTO_INCREMENT PRIMARY KEY,
    Name         VARCHAR(100) NOT NULL,
    Email        VARCHAR(255) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    RoleId       INT NOT NULL,
    CreatedAt    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt    DATETIME NULL,
    IsActive     TINYINT(1) NOT NULL DEFAULT 1,
    UNIQUE INDEX IX_Users_Email (Email),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);

CREATE TABLE RefreshTokens (
    Id         INT AUTO_INCREMENT PRIMARY KEY,
    UserId     INT NOT NULL,
    Token      VARCHAR(512) NOT NULL,
    ExpiresAt  DATETIME NOT NULL,
    CreatedAt  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    RevokedAt  DATETIME NULL,
    IsRevoked  TINYINT(1) NOT NULL DEFAULT 0,
    INDEX IX_RefreshTokens_Token (Token),
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE TABLE AuditLogs (
    Id         INT AUTO_INCREMENT PRIMARY KEY,
    UserId     INT NULL,
    Action     VARCHAR(100) NOT NULL,
    EntityName VARCHAR(100) NOT NULL,
    EntityId   INT NULL,
    Timestamp  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Details    TEXT NULL,
    INDEX IX_AuditLogs_UserId (UserId),
    INDEX IX_AuditLogs_Entity (EntityName, EntityId),
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
);
```

### 8.2 Seed Data

```sql
INSERT INTO Roles (Name, Description) VALUES ('Admin', 'System administrator');
INSERT INTO Roles (Name, Description) VALUES ('User', 'Standard user');
```

---

## 9. Configuration

### 9.1 appsettings.json Structure

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=SmreaderDB;Uid=root;Pwd=yourpassword;"
  },
  "Jwt": {
    "Key": "YOUR_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG",
    "Issuer": "SmreaderAPI",
    "Audience": "SmreaderAPI",
    "AccessTokenExpiryMinutes": 30,
    "RefreshTokenExpiryDays": 7
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Cache": {
    "L1ExpiryMinutes": 5,
    "L2ExpiryMinutes": 30
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "https://yourdomain.com"]
  },
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "HttpStatusCode": 429,
    "GeneralRules": [
      { "Endpoint": "*", "Period": "1m", "Limit": 100 },
      { "Endpoint": "post:/api/auth/*", "Period": "1m", "Limit": 10 }
    ]
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/log-.txt", "rollingInterval": "Day" } }
    ]
  }
}
```

### 9.2 Environment-Specific Overrides

| File                             | Purpose                                                     |
| -------------------------------- | ----------------------------------------------------------- |
| `appsettings.Development.json`   | Local MySQL/Redis connection strings, verbose logging       |
| `appsettings.Staging.json`       | Staging server config (placeholders)                        |
| `appsettings.Production.json`    | Minimal — secrets injected via environment variables        |

### 9.3 Environment Variables (Production)

| Variable                              | Purpose                   |
| ------------------------------------- | ------------------------- |
| `ConnectionStrings__DefaultConnection`| MySQL connection string    |
| `Jwt__Key`                            | JWT signing key            |
| `Redis__ConnectionString`             | Redis connection string    |
| `ASPNETCORE_ENVIRONMENT`              | `Production`               |

---

## 10. Docker

### 10.1 Dockerfile (Multi-Stage)

```
Stage 1: build (SDK image)
  → Copy .csproj files → dotnet restore (layer cached)
  → Copy source → dotnet publish -c Release

Stage 2: runtime (ASP.NET runtime image)
  → Copy published output from Stage 1
  → Non-root user (appuser)
  → EXPOSE 8080
  → ENTRYPOINT ["dotnet", "SmreaderAPI.API.dll"]
```

### 10.2 docker-compose.yml

| Service | Image            | Port  | Depends On     | Volume                        |
| ------- | ---------------- | ----- | -------------- | ----------------------------- |
| `api`   | Build from `./`  | 8080  | mysql, redis   | —                             |
| `mysql` | `mysql:8.0`      | 3306  | —              | `mysql_data:/var/lib/mysql`, `./scripts/init.sql:/docker-entrypoint-initdb.d/init.sql` |
| `redis` | `redis:7-alpine` | 6379  | —              | `redis_data:/data`            |

### 10.3 .env.example

```env
MYSQL_ROOT_PASSWORD=yourpassword
MYSQL_DATABASE=SmreaderDB
JWT_KEY=YOUR_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG
REDIS_CONNECTION=redis:6379
ASPNETCORE_ENVIRONMENT=Development
```

---

## 11. CI/CD — GitHub Actions

### 11.1 CI Pipeline (`.github/workflows/ci.yml`)

```
Trigger: push to main/develop, PR to main
┌─────────────────────────────────────────────┐
│  Job: build                                 │
│  ├── Checkout                               │
│  ├── Setup .NET 10 SDK                      │
│  ├── dotnet restore                         │
│  ├── dotnet build --configuration Release   │
│  ├── dotnet test --collect:"XPlat Code Coverage" │
│  └── Upload test results + coverage artifact │
├─────────────────────────────────────────────┤
│  Job: code-quality (parallel)               │
│  ├── Checkout                               │
│  ├── Setup .NET 10 SDK                      │
│  └── dotnet format --verify-no-changes      │
└─────────────────────────────────────────────┘
```

### 11.2 CD Pipeline (`.github/workflows/cd.yml`)

```
Trigger: push to main (after CI), workflow_dispatch
┌───────────────────────────────────────────────────┐
│  Job: build-and-publish                           │
│  ├── Checkout                                     │
│  ├── dotnet publish -c Release -o ./publish       │
│  └── Upload publish artifact                      │
├───────────────────────────────────────────────────┤
│  Job: build-docker (parallel)                     │
│  ├── Checkout                                     │
│  ├── Docker build + tag (SHA + latest)            │
│  └── Push to GitHub Container Registry            │
├───────────────────────────────────────────────────┤
│  Job: deploy-to-iis (depends on build-and-publish)│
│  ├── Download publish artifact                    │
│  ├── SSH to VM                                    │
│  ├── Stop IIS site                                │
│  ├── Copy files to IIS directory                  │
│  ├── Start IIS site                               │
│  └── Health check: curl /health                   │
└───────────────────────────────────────────────────┘
```

**Required GitHub Secrets:**

| Secret              | Purpose                          |
| ------------------- | -------------------------------- |
| `DEPLOY_HOST`       | VM hostname/IP                   |
| `DEPLOY_USERNAME`   | SSH/Web Deploy username          |
| `DEPLOY_PASSWORD`   | SSH/Web Deploy password or key   |
| `DEPLOY_IIS_SITE`   | IIS site name                    |
| `GHCR_TOKEN`        | GitHub Container Registry token  |

### 11.3 PR Checks (`.github/workflows/pr-checks.yml`)

```
Trigger: pull_request to main
  → build + test + format check
  → Blocks merge on failure (via branch protection rules)
```

### 11.4 IIS Hosting — web.config

```xml
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\SmreaderAPI.API.dll"
                  stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout"
                  hostingModel="InProcess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

---

## 12. Unit Tests

### 12.1 Test Project Setup

- Framework: **xUnit** + **Moq** + **FluentAssertions**
- Project: `tests/SmreaderAPI.UnitTests`
- References: all 4 src projects

### 12.2 Test Coverage Matrix

| Test File                          | Class Under Test         | Test Cases                                                                         |
| ---------------------------------- | ------------------------ | ---------------------------------------------------------------------------------- |
| `GenericRepositoryTests.cs`        | `GenericRepository<T>`   | CRUD operations with mocked `IDbConnection`                                        |
| `UserRepositoryTests.cs`           | `UserRepository`         | `GetByEmailAsync` returns correct user, returns null for unknown email              |
| `ExpressionToSqlConverterTests.cs` | `ExpressionToSqlConverter` | See §12.3 below                                                                  |
| `UserServiceTests.cs`              | `UserService`            | Register (success + duplicate email), Login (valid + invalid), CRUD, cache invalidation |
| `AuthServiceTests.cs`              | `AuthService`            | JWT contains correct claims, refresh token is unique, expired token rejected        |
| `AuditServiceTests.cs`             | `AuditService`           | `LogActionAsync` creates correct `AuditLog` entry                                  |
| `CacheServiceTests.cs`             | `CacheService`           | L1 hit, L1 miss → L2 hit, both miss, invalidation clears both                     |
| `AuthControllerTests.cs`           | `AuthController`         | Register → 201, Login → 200 with token, invalid login → 401                       |
| `UsersControllerTests.cs`          | `UsersController`        | Authorized CRUD succeeds, unauthorized → 401                                       |

### 12.3 ExpressionToSqlConverter Test Cases

| Input Expression                              | Expected SQL                            | Parameters        |
| --------------------------------------------- | --------------------------------------- | ----------------- |
| `x => x.Email == "test@test.com"`             | `WHERE Email = @p0`                     | `@p0 = "test@test.com"` |
| `x => x.IsActive && x.RoleId == 1`           | `WHERE (IsActive = @p0) AND (RoleId = @p1)` | `@p0 = true, @p1 = 1` |
| `x => x.Name.Contains("john")`               | `WHERE Name LIKE @p0`                   | `@p0 = "%john%"`  |
| `x => x.Name.StartsWith("J")`                | `WHERE Name LIKE @p0`                   | `@p0 = "J%"`      |
| `x => x.Name.EndsWith("son")`                | `WHERE Name LIKE @p0`                   | `@p0 = "%son"`    |
| `x => x.UpdatedAt == null`                    | `WHERE UpdatedAt IS NULL`               | *(none)*           |
| `x => x.UpdatedAt != null`                    | `WHERE UpdatedAt IS NOT NULL`           | *(none)*           |
| `x => x.Id > 5`                              | `WHERE Id > @p0`                        | `@p0 = 5`         |
| `x => x.RoleId == 1 \|\| x.RoleId == 2`     | `WHERE (RoleId = @p0) OR (RoleId = @p1)` | `@p0 = 1, @p1 = 2` |

---

## 13. Security Considerations

| Area                  | Implementation                                                   |
| --------------------- | ---------------------------------------------------------------- |
| SQL Injection          | All queries parameterized (Dapper + ExpressionToSqlConverter)   |
| Password Storage       | BCrypt with work factor 12 — never stored in plaintext          |
| JWT                    | HMAC-SHA256 signing, short-lived access tokens (30 min)         |
| Refresh Tokens         | Cryptographically random, stored hashed, rotated on use         |
| CORS                   | Production: restricted origins only                             |
| Rate Limiting          | IP-based, strict on auth endpoints                              |
| Error Responses        | No stack traces in production responses                         |
| Secrets                | Environment variables in production, not in source control      |
| Dependencies           | No known vulnerable packages at time of specification           |
| HTTPS                  | Enforced via IIS binding (not in application code)              |

---

## 14. Acceptance Criteria

| #  | Criterion                                                                                   | Verification Method       |
| -- | ------------------------------------------------------------------------------------------- | ------------------------- |
| 1  | Solution compiles with zero errors and warnings                                             | `dotnet build`            |
| 2  | API starts and Swagger UI loads at `/swagger`                                               | `dotnet run` + browser    |
| 3  | Register → Login → JWT → Refresh Token flow works end to end                               | Swagger / curl            |
| 4  | CRUD on `/api/users` requires valid JWT                                                     | 401 without token         |
| 5  | Admin-only endpoints reject non-Admin tokens                                                | 403 with User role token  |
| 6  | `FindAsync(x => x.Email == "...")` generates parameterized SQL                              | Unit test                 |
| 7  | UnitOfWork commit/rollback work correctly                                                   | Unit test                 |
| 8  | AuditLog entries created on user create/update/delete                                       | DB query after mutation   |
| 9  | All unit tests pass                                                                         | `dotnet test`             |
| 10 | Rate limit returns 429 after exceeding threshold                                            | Rapid curl requests       |
| 11 | Cache hit returns data without DB query (verify via logs)                                   | Serilog output            |
| 12 | Serilog writes structured logs to console + file with enriched properties                   | `logs/` directory         |
| 13 | CORS preflight returns correct headers                                                      | `OPTIONS` request         |
| 14 | Docker image builds and `docker-compose up` starts all services                             | Docker CLI                |
| 15 | CI pipeline runs build + test on push                                                       | GitHub Actions UI         |
| 16 | CD pipeline deploys to IIS and health check passes                                          | GitHub Actions + `/health`|
| 17 | `scripts/init.sql` executes without errors on fresh MySQL                                   | MySQL CLI                 |
| 18 | No secrets committed to source control                                                      | `.gitignore` review       |
| 19 | Health endpoint returns MySQL + Redis connectivity status                                   | `GET /health`             |
| 20 | Non-root Docker user in production image                                                    | `docker inspect`          |
