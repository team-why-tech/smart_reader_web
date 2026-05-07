# SmreaderAPI — Technical Specification

| Field              | Value                                                              |
| ------------------ | ------------------------------------------------------------------ |
| **Project**        | SmreaderAPI                                                        |
| **Version**        | 2.0.0                                                              |
| **Target**         | .NET 9.0                                                           |
| **Database**       | MySQL 8.x (Multi-Tenant)                                           |
| **ORM**            | EF Core + Dapper (dual)                                            |
| **Auth**           | JWT Bearer + Refresh Token Rotation                                |
| **Multi-Tenancy**  | Per-request DB switching via middleware + cached connection strings |
| **Architecture**   | Clean Architecture (Domain → Application → Infrastructure → API)   |
| **Caching**        | L1 In-Memory + L2 Redis                                           |
| **Logging**        | Serilog (Console + File sinks)                                     |
| **Rate Limiting**  | AspNetCoreRateLimit (IP-based)                                     |
| **Testing**        | xUnit + Moq + FluentAssertions                                    |

---

## 1. Multi-Tenant Architecture

### 1.1 Overview

The system uses a **Master DB + Tenant DB** pattern:

- **Master DB** (`smreader_master`): Contains `ca_management` (tenant registry) and `ca_refresh_tokens` (JWT refresh tokens scoped by tenant_id).
- **Tenant DBs** (e.g. `smreader_kadanad`): Per-tenant databases containing `ca_users` and `AuditLogs`. Split by financial year — the system always selects the latest FY database.

### 1.2 Request Flow

```
Login Request (anonymous):
  Client sends { tenantId, email, password }
  → AuthController resolves tenant manually
  → Queries Master DB (ca_management) for DB credentials
  → Builds connection string, caches it (30min TTL)
  → Sets ITenantContext → UserService validates credentials
  → Returns JWT with tenant_id claim

Authenticated Request:
  Client sends JWT Bearer token
  → JWT Authentication extracts claims
  → TenantResolutionMiddleware reads tenant_id claim
  → Resolves connection string (cache → Master DB fallback)
  → Sets ITenantContext
  → Controller → Service → Repository (all use tenant DB)
```

### 1.3 Tenant Resolution Middleware

```
Request
  → ExceptionMiddleware
  → Serilog Request Logging
  → Rate Limiting
  → CORS
  → Authentication (JWT Bearer)
  → TenantResolutionMiddleware
      Skip: /health, /swagger, /auth/login
      JWT claims → extract tenant_id
      Cache check → Master DB fallback
      → ITenantContext.Set(tenantId, connectionString)
  → Authorization
  → Controller Action
Response
```

### 1.4 Connection String Caching

| Layer | Implementation | TTL | Purpose |
|-------|---------------|-----|---------|
| `TenantConnectionStringCache` | `IMemoryCache` | 30 min | Avoid Master DB lookup per request |

Cache key format: `tenant_conn:{tenantId}`

---

## 2. Solution Structure

```
SmreaderAPI/
├── SmreaderAPI.sln
├── Dockerfile
├── docker-compose.yml
├── .dockerignore
├── .env.example
├── scripts/
│   └── init.sql
├── src/
│   ├── SmreaderAPI.Domain/
│   │   ├── Entities/
│   │   │   ├── BaseEntity.cs
│   │   │   ├── Tenant.cs
│   │   │   ├── User.cs
│   │   │   ├── RefreshToken.cs
│   │   │   └── AuditLog.cs
│   │   └── Interfaces/
│   │       ├── IRepository.cs
│   │       ├── ITenantContext.cs
│   │       ├── ITenantRepository.cs
│   │       ├── IUserRepository.cs
│   │       ├── IRefreshTokenRepository.cs
│   │       ├── IAuditLogRepository.cs
│   │       └── IUnitOfWork.cs
│   ├── SmreaderAPI.Application/
│   │   ├── DTOs/
│   │   │   ├── UserDto.cs
│   │   │   ├── AuthDtos.cs
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
│   │   │   ├── SmreaderDbContext.cs
│   │   │   ├── TenantContext.cs
│   │   │   ├── TenantConnectionStringBuilder.cs
│   │   │   └── ExpressionToSqlConverter.cs
│   │   ├── Repositories/
│   │   │   ├── GenericRepository.cs
│   │   │   ├── TenantRepository.cs
│   │   │   ├── UserRepository.cs
│   │   │   ├── RefreshTokenRepository.cs
│   │   │   └── AuditLogRepository.cs
│   │   ├── UnitOfWork/
│   │   │   └── UnitOfWork.cs
│   │   ├── Services/
│   │   │   └── AuthService.cs
│   │   ├── Caching/
│   │   │   ├── CacheService.cs
│   │   │   └── TenantConnectionStringCache.cs
│   │   └── Logging/
│   │       └── SerilogRequestEnricher.cs
│   └── SmreaderAPI.API/
│       ├── Program.cs
│       ├── appsettings.json
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── UsersController.cs
│       │   └── AuditLogController.cs
│       └── Middleware/
│           ├── ExceptionMiddleware.cs
│           └── TenantResolutionMiddleware.cs
└── tests/
    └── SmreaderAPI.UnitTests/
```

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

#### Tenant (Master DB — `ca_management`)

| Property      | Type       | Column          | Constraints            |
| ------------- | ---------- | --------------- | ---------------------- |
| `Id`          | `int`      | `id`            | PK, auto-increment     |
| `Name`        | `string`   | `name`          | Required               |
| `CompanyName` | `string`   | `company_name`  | Required               |
| `DbName`      | `string?`  | `db_name`       | Tenant database name   |
| `DbUser`      | `string?`  | `db_user`       | Tenant DB username     |
| `DbPwd`       | `string?`  | `db_pwd`        | Tenant DB password     |
| `DateFrom`    | `DateTime` | `date_from`     | FY start date          |
| `DateTo`      | `DateTime` | `date_to`       | FY end date            |

#### User (Tenant DB — `ca_users`)

| Property    | Type      | Column       | Constraints            |
| ----------- | --------- | ------------ | ---------------------- |
| `Id`        | `int`     | `id`         | PK, auto-increment     |
| `Name`      | `string`  | `name`       | Required               |
| `Address`   | `string?` | `address`    | Optional               |
| `Mobile`    | `string`  | `mobile`     | Required               |
| `Email`     | `string`  | `email`      | Required               |
| `Pwd`       | `string`  | `pwd`        | BCrypt hash            |
| `OwnerGuid` | `int`     | `owner_guid` | Default: 0             |
| `Status`    | `int`     | `status`     | 0=inactive, 1=active   |

#### RefreshToken (Master DB — `ca_refresh_tokens`)

| Property    | Type        | Column       | Constraints            |
| ----------- | ----------- | ------------ | ---------------------- |
| `Id`        | `int`       | `id`         | PK, auto-increment     |
| `TenantId`  | `int`       | `tenant_id`  | Required               |
| `UserId`    | `int`       | `user_id`    | User ID in tenant DB   |
| `Token`     | `string`    | `token`      | Required, indexed      |
| `ExpiresAt` | `DateTime`  | `expires_at` | Required               |
| `CreatedAt` | `DateTime`  | `created_at` | Default: UTC now       |
| `RevokedAt` | `DateTime?` | `revoked_at` | Nullable               |
| `IsRevoked` | `bool`      | `is_revoked` | Default: false         |

#### AuditLog (Tenant DB — `AuditLogs`)

| Property     | Type        | Constraints            |
| ------------ | ----------- | ---------------------- |
| `Id`         | `int`       | PK, auto-increment     |
| `UserId`     | `int?`      | FK → ca_users.id       |
| `Action`     | `string`    | Required, max 100      |
| `EntityName` | `string`    | Required, max 100      |
| `EntityId`   | `int?`      | Nullable               |
| `Timestamp`  | `DateTime`  | Default: UTC now       |
| `Details`    | `string?`   | Optional, TEXT         |

### 3.3 Interfaces

#### ITenantContext

```csharp
public interface ITenantContext
{
    int TenantId { get; }
    string ConnectionString { get; }
    bool IsResolved { get; }
    void Set(int tenantId, string connectionString);
}
```

#### IRefreshTokenRepository (standalone — Master DB)

```csharp
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<int> AddAsync(RefreshToken refreshToken);
    Task RevokeTokenAsync(int id);
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(int tenantId, int userId);
}
```

#### ITenantRepository

```csharp
public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(int tenantId);
    Task<Tenant?> GetLatestByIdAsync(int tenantId);
    Task<IEnumerable<Tenant>> GetAllAsync();
}
```

#### IRepository\<T\> (Generic)

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    IQueryable<T> AsQueryable();
    Task<int> AddAsync(T entity);
    Task<int> UpdateAsync(T entity);
    Task<int> DeleteAsync(int id);
    Task<IEnumerable<T>> QueryAsync(string sql, object? param = null);
}
```

#### Entity-Specific Repositories

| Interface                  | Additional Methods                                |
| -------------------------- | ------------------------------------------------- |
| `IUserRepository`          | `GetByEmailAsync(email)`, `GetByMobileAsync(mobile)` |
| `IAuditLogRepository`      | `GetByEntityAsync(entityName, entityId)`, `GetByUserIdAsync(userId)` |

> [!NOTE]
> `IRefreshTokenRepository` is standalone (not part of `IUnitOfWork`) since it operates on the Master DB.

#### IUnitOfWork (tenant-scoped)

```csharp
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IAuditLogRepository AuditLogs { get; }
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
```

---

## 4. Application Layer

### 4.1 DTOs

```csharp
// Auth
public record TenantLoginDto(int TenantId, string Email, string Password);
public record LoginDto(string Email, string Password);
public record TokenResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public record RefreshTokenRequestDto(string RefreshToken);

// User
public record UserDto(int Id, string Name, string Email, string? Mobile, int Status);
public record CreateUserDto(string Name, string Email, string Password, string? Mobile);
public record UpdateUserDto(string? Name, string? Email, string? Mobile, int? Status);

// Audit
public record AuditLogDto(int Id, int? UserId, string Action, string EntityName, int? EntityId, DateTime Timestamp, string? Details);

// Generic
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
    Task<ApiResponse<TokenResponseDto>> LoginAsync(TenantLoginDto dto);
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
    string GenerateJwtToken(User user, int tenantId);
    string GenerateRefreshToken();
    Task<bool> ValidateRefreshTokenAsync(string token);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
```

### 4.3 UserService Behavior

| Method             | Behavior                                                                     |
| ------------------ | ---------------------------------------------------------------------------- |
| `LoginAsync`       | Validate against tenant DB ca_users → BCrypt verify → generate JWT with tenant_id → store refresh token |
| `RefreshTokenAsync` | Validate token → revoke old → generate new pair → store new refresh token   |
| `GetByIdAsync`     | Check cache (tenant-scoped key) → query tenant DB → populate cache           |
| `GetAllAsync`      | Query tenant DB → return DTOs                                                |
| `UpdateAsync`      | Transaction → update → invalidate cache → audit log                         |
| `DeleteAsync`      | Transaction → delete → invalidate cache → audit log                         |

Cache key format: `tenant:{tenantId}:user:{userId}`

---

## 5. Infrastructure Layer

### 5.1 DapperContext

```csharp
public class DapperContext
{
    // Tenant DB connection (uses ITenantContext.ConnectionString)
    public IDbConnection CreateConnection();

    // Master DB connection (uses DefaultConnection from appsettings)
    public IDbConnection CreateMasterConnection();
}
```

- **Scoped** lifetime (depends on ITenantContext)
- `CreateConnection()` throws if tenant not resolved

### 5.2 SmreaderDbContext (EF Core)

- Dynamically switches database in `OnConfiguring()` using `ITenantContext.ConnectionString`
- Maps `ca_users` and `ca_refresh_tokens` column names in `OnModelCreating()`
- **Scoped** lifetime

### 5.3 TenantConnectionStringBuilder

- Parses host/port from Master DB connection string
- Builds tenant connection string using `MySqlConnectionStringBuilder`
- All tenant DBs assumed on same MySQL server as Master DB
- **Singleton** lifetime

### 5.4 TenantConnectionStringCache

- Wraps `IMemoryCache` with 30-minute TTL
- Key format: `tenant_conn:{tenantId}`
- **Singleton** lifetime

### 5.5 RefreshTokenRepository (Master DB)

- Standalone Dapper repository using `CreateMasterConnection()`
- Not part of `GenericRepository<T>` or `IUnitOfWork`
- All queries include `tenant_id` for cross-tenant isolation
- Injected directly into `UserService` and `AuthService`

### 5.6 TenantRepository

- Queries Master DB via Dapper (uses `CreateMasterConnection()`)
- `GetLatestByIdAsync()`: Returns tenant with latest `date_to` (latest FY)
- Explicit SQL column aliasing for ca_management

### 5.7 AuthService

**JWT Claims:**

| Claim                       | Value          |
| --------------------------- | -------------- |
| `ClaimTypes.NameIdentifier` | User.Id        |
| `ClaimTypes.Email`          | User.Email     |
| `ClaimTypes.Name`           | User.Name      |
| `tenant_id`                 | TenantId       |

**Password Hashing:** BCrypt with work factor 12.

### 5.8 CacheService

```
Request → L1 (IMemoryCache, 5 min TTL)
              ↓ miss
          L2 (Redis IDistributedCache, 30 min TTL)
              ↓ miss
          Database (via repository)
              ↓ result
          Write to L1 + L2 (write-through)
```

---

## 6. API Layer

### 6.1 Endpoints

#### Auth — `AuthController`

| Method | Route                     | Auth      | Request Body            | Response                        |
| ------ | ------------------------- | --------- | ----------------------- | ------------------------------- |
| POST   | `/api/auth/login`         | Anonymous | `TenantLoginDto`        | `ApiResponse<TokenResponseDto>` |
| POST   | `/api/auth/refresh-token` | JWT       | `RefreshTokenRequestDto`| `ApiResponse<TokenResponseDto>` |

#### Users — `UsersController`

| Method | Route             | Auth          | Request Body    | Response                          |
| ------ | ----------------- | ------------- | --------------- | --------------------------------- |
| GET    | `/api/users`      | `[Authorize]` | —               | `ApiResponse<IEnumerable<UserDto>>` |
| GET    | `/api/users/{id}` | `[Authorize]` | —               | `ApiResponse<UserDto>`            |
| PUT    | `/api/users/{id}` | `[Authorize]` | `UpdateUserDto` | `ApiResponse<UserDto>`            |
| DELETE | `/api/users/{id}` | `[Authorize]` | —               | `ApiResponse<bool>`               |

#### Audit Logs — `AuditLogController`

| Method | Route                          | Auth          | Response                              |
| ------ | ------------------------------ | ------------- | ------------------------------------- |
| GET    | `/api/auditlogs`               | `[Authorize]` | `ApiResponse<IEnumerable<AuditLogDto>>` |
| GET    | `/api/auditlogs/user/{userId}` | `[Authorize]` | `ApiResponse<IEnumerable<AuditLogDto>>` |

#### Health

| Method | Route     | Auth      | Response              |
| ------ | --------- | --------- | --------------------- |
| GET    | `/health` | Anonymous | `Healthy`/`Unhealthy` |

### 6.2 Dependency Injection

| Service                        | Lifetime  | Implementation               |
| ------------------------------ | --------- | ---------------------------- |
| `ITenantContext`               | Scoped    | `TenantContext`              |
| `TenantConnectionStringBuilder`| Singleton | `TenantConnectionStringBuilder` |
| `TenantConnectionStringCache`  | Singleton | `TenantConnectionStringCache`|
| `ITenantRepository`            | Scoped    | `TenantRepository`           |
| `DapperContext`                | Scoped    | `DapperContext`              |
| `SmreaderDbContext`            | Scoped    | `SmreaderDbContext`          |
| `IUnitOfWork`                  | Scoped    | `UnitOfWork` (tenant-scoped) |
| `IRefreshTokenRepository`      | Scoped    | `RefreshTokenRepository` (Master DB) |
| `IUserService`                 | Scoped    | `UserService`                |
| `IAuthService`                 | Scoped    | `AuthService`                |
| `IAuditService`                | Scoped    | `AuditService`               |
| `ICacheService`                | Singleton | `CacheService`               |

---

## 7. Database Schema

### 7.1 Master Database (`smreader_master`)

```sql
CREATE TABLE `ca_management` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(255) NOT NULL,
  `company_name` varchar(255) NOT NULL,
  `db_name` varchar(50) DEFAULT NULL,
  `db_user` varchar(50) DEFAULT NULL,
  `db_pwd` varchar(250) DEFAULT NULL,
  `date_from` date DEFAULT '1900-01-01',
  `date_to` date DEFAULT '1900-01-01',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
```

-- Refresh tokens (in Master DB, scoped by tenant_id)
CREATE TABLE IF NOT EXISTS `ca_refresh_tokens` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `tenant_id` int(11) NOT NULL,
  `user_id` int(11) NOT NULL,
  `token` varchar(512) NOT NULL,
  `expires_at` datetime NOT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `revoked_at` datetime DEFAULT NULL,
  `is_revoked` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  INDEX `IX_RefreshTokens_Token` (`token`),
  INDEX `IX_RefreshTokens_Tenant_User` (`tenant_id`, `user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
```

### 7.2 Tenant Database (e.g. `smreader_kadanad`)

```sql
-- Pre-existing
CREATE TABLE `ca_users` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(255) NOT NULL DEFAULT '',
  `address` tinytext,
  `mobile` varchar(50) NOT NULL DEFAULT '',
  `email` varchar(200) NOT NULL DEFAULT '',
  `pwd` varchar(100) NOT NULL DEFAULT '',
  `owner_guid` int(11) NOT NULL DEFAULT '0',
  `status` int(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- Created by init.sql
CREATE TABLE `AuditLogs` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `UserId` int(11) DEFAULT NULL,
  `Action` varchar(100) NOT NULL,
  `EntityName` varchar(100) NOT NULL,
  `EntityId` int(11) DEFAULT NULL,
  `Timestamp` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Details` text,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  INDEX `IX_AuditLogs_UserId` (`UserId`),
  CONSTRAINT `FK_AuditLogs_Users` FOREIGN KEY (`UserId`)
    REFERENCES `ca_users`(`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
```

---

## 8. Configuration

### 8.1 appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=smreader_master;Uid=root;Pwd=yourpassword;"
  },
  "Jwt": {
    "Key": "YOUR_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG!!",
    "Issuer": "SmreaderAPI",
    "Audience": "SmreaderAPI",
    "AccessTokenExpiryMinutes": 30,
    "RefreshTokenExpiryDays": 7
  },
  "Redis": { "ConnectionString": "localhost:6379" },
  "Cache": { "L1ExpiryMinutes": 5, "L2ExpiryMinutes": 30 },
  "Cors": { "AllowedOrigins": ["http://localhost:3000"] },
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "GeneralRules": [
      { "Endpoint": "*", "Period": "1m", "Limit": 100 },
      { "Endpoint": "post:/api/auth/*", "Period": "1m", "Limit": 10 }
    ]
  }
}
```

### 8.2 Environment Variables (Production)

| Variable                               | Purpose                |
| -------------------------------------- | ---------------------- |
| `ConnectionStrings__DefaultConnection` | Master DB conn string  |
| `Jwt__Key`                             | JWT signing key        |
| `Redis__ConnectionString`              | Redis connection       |
| `ASPNETCORE_ENVIRONMENT`               | `Production`           |

---

## 9. Security

| Area               | Implementation                                                  |
| ------------------ | --------------------------------------------------------------- |
| SQL Injection      | Parameterized queries (Dapper + EF Core)                        |
| Password Storage   | BCrypt with work factor 12                                      |
| JWT                | HMAC-SHA256, 30-min access tokens, tenant_id claim              |
| Refresh Tokens     | Cryptographically random, stored in Master DB with tenant_id, rotated on use |
| Tenant Isolation   | Per-request connection switching, tenant-scoped cache keys      |
| CORS               | Production: restricted origins                                  |
| Rate Limiting      | IP-based, strict on auth endpoints                              |
| Error Responses    | No stack traces in production                                   |

---

## 10. Unit Tests (36 tests)

| Test File                          | Tests                                                               |
| ---------------------------------- | ------------------------------------------------------------------- |
| `UserServiceTests.cs`              | Login (valid, invalid, deactivated), GetById (cache hit/miss), Delete |
| `AuthServiceTests.cs`              | JWT generation, refresh token uniqueness, token validation          |
| `AuditServiceTests.cs`             | Log creation, query by user, query all                              |
| `CacheServiceTests.cs`             | L1/L2 hit/miss, invalidation                                       |
| `ExpressionToSqlConverterTests.cs` | ==, !=, AND, OR, LIKE, IS NULL, >, boolean                         |
| `AuthControllerTests.cs`           | Login 200, Login 401, Tenant not found 404                         |
| `UsersControllerTests.cs`          | GetAll, GetById, GetById 404, Delete                               |
