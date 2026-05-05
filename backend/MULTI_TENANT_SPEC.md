# SmreaderAPI — Multi-Tenant Architecture Specification v2.0

| Field | Value |
|-------|-------|
| **Project** | SmreaderAPI |
| **Version** | 2.0.0 — Multi-Tenant |
| **Target** | .NET 10.0 |
| **Database** | MySQL 8.x (Master + Per-Tenant-Per-FY) |
| **ORM** | EF Core + Dapper (hybrid) |
| **Auth** | JWT Bearer + Centralized Refresh Tokens |
| **Architecture** | Multi-Tenant Clean Architecture |
| **Tenancy** | Database per Tenant per Financial Year |
| **Caching** | L1 (Memory) + L2 (Redis) with 30-min TTL |
| **Date** | May 5, 2026 |

---

## 1. Multi-Tenant Architecture Overview

### Database Strategy

```
MASTER DATABASE (Control Plane)
├── tenants                → Tenant registry
├── tenant_databases       → FY database mapping
└── refresh_tokens         → Centralized token storage

TENANT DATABASES (Data Plane per FY)
├── smreader_tenant1_fy2024_25
│   ├── users
│   ├── roles
│   └── audit_logs
└── smreader_tenant1_fy2023_24
    ├── users
    ├── roles
    └── audit_logs
```

### Key Principles
- **Master DB** = Control plane (tenant config, centralized auth state)
- **Tenant DB** = Data plane (business data per tenant per FY)
- **JWT carries** `tenant_id` + `fy` for runtime database routing
- **Connection strings cached** with 30-min TTL (L1/L2)
- **RefreshTokens** stored ONLY in master DB (never in tenant DBs)

---

## 2. Authentication Flows

### Login Flow
```
POST /api/auth/login
Body: { "tenantCode": "acme", "email": "user@acme.com", "password": "..." }

1. Look up tenant by code in master DB
2. Get default FY connection string from master DB
3. Connect to tenant DB using resolved connection
4. Validate email/password against tenant's users table
5. Generate JWT with claims: user_id, tenant_id, fy, role
6. Generate & store refresh token in master DB
7. Return { accessToken, refreshToken, expiresAt }
```

### JWT Structure
```json
{
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "1",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "user@acme.com",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Admin",
  "tenant_id": "101",
  "fy": "2024-25",
  "exp": 1717000000
}
```

**Expiry:** 30 minutes (configurable)

### Refresh Token Flow
```
POST /api/auth/refresh-token
Body: { "refreshToken": "..." }

1. Validate token against master DB (not tenant DB)
2. Check: not revoked, not expired
3. Revoke old token (set RevokedAt, ReplacedByToken)
4. Get user from tenant DB (default FY)
5. Generate new JWT + new refresh token
6. Store new refresh token in master DB
7. Return new tokens
```

### Switch Financial Year
```
POST /api/auth/switch-fy
Authorization: Bearer <jwt>
Body: { "financialYear": "2023-24" }

1. Read tenant_id from current JWT
2. Validate requested FY exists for tenant in master DB
3. Get user from requested FY tenant DB
4. Generate new JWT with updated fy claim
5. Generate new refresh token
6. Return new tokens
```

---

## 3. Request Pipeline

```
Request
  ↓
ExceptionMiddleware
  ↓
Serilog Request Logging
  ↓
IP Rate Limiting
  ↓
CORS
  ↓
Authentication (JWT validation)
  ↓
TenantResolutionMiddleware  ← NEW
  • Reads tenant_id + fy from JWT
  • Resolves connection (cache → master DB)
  • Populates ITenantContext
  ↓
Authorization
  ↓
Controller → Service → Repository → Tenant DB
```

### TenantResolutionMiddleware Logic
- **Skips:** `/api/auth/login`, `/api/auth/register`, `/api/auth/refresh-token`, `/health`, `/swagger`
- **Reads:** `tenant_id` and `fy` claims from authenticated user
- **Resolves:** Connection string via `ITenantConnectionResolver`
- **Populates:** Scoped `ITenantContext` with connection details

---

## 4. Connection Resolution Strategy

### Master DB
- **Static connection** from `appsettings.json` → `ConnectionStrings:MasterConnection`
- Used by `MasterDbContext` and `MasterDapperContext`

### Tenant DB
- **Dynamic connection** resolved per request
- **Cache key:** `tenant:{tenantId}:fy:{fy}`
- **Cache TTL:** 30 minutes
- **Cache layers:** L1 (IMemoryCache) → L2 (Redis) → Master DB fallback
- Used by `SmreaderDbContext` and `TenantDapperContext` (scoped per request)

---

## 5. Domain Entities

### Master Entities (Control Plane)

#### tenants
```sql
Id INT PK
Name VARCHAR(100)
Code VARCHAR(50) UNIQUE
IsActive TINYINT
CreatedAt DATETIME
UpdatedAt DATETIME
```

#### tenant_databases
```sql
Id INT PK
TenantId INT FK → tenants
FinancialYear VARCHAR(10)  -- e.g., "2024-25"
ConnectionString VARCHAR(512)
IsDefault TINYINT
CreatedAt DATETIME
UpdatedAt DATETIME
UNIQUE (TenantId, FinancialYear)
```

#### refresh_tokens
```sql
Id INT PK
UserId INT              -- from tenant DB
TenantId INT FK → tenants
Token VARCHAR(512) INDEXED
ExpiresAt DATETIME
CreatedAt DATETIME
RevokedAt DATETIME NULL
IsRevoked TINYINT
ReplacedByToken VARCHAR(512) NULL
IpAddress VARCHAR(45) NULL
```

### Tenant Entities (Data Plane)

**Note:** RefreshTokens do NOT exist in tenant DBs

#### users
```sql
Id INT PK
Name VARCHAR(100)
Email VARCHAR(255) UNIQUE
PasswordHash VARCHAR(255)
RoleId INT FK → roles
IsActive TINYINT
CreatedAt DATETIME
UpdatedAt DATETIME
```

#### roles
```sql
Id INT PK
Name VARCHAR(50) UNIQUE
Description VARCHAR(255)
CreatedAt DATETIME
UpdatedAt DATETIME
```

#### audit_logs
```sql
Id INT PK
UserId INT NULL FK → users
Action VARCHAR(100)
EntityName VARCHAR(100)
EntityId INT NULL
Timestamp DATETIME
Details TEXT
CreatedAt DATETIME
UpdatedAt DATETIME
```

---

## 6. Service Registration (DI)

| Service | Lifetime | Implementation |
|---------|----------|----------------|
| `MasterDbContext` | Scoped | Static master connection |
| `MasterDapperContext` | Singleton | Static master connection |
| `IMasterUnitOfWork` | Scoped | `MasterUnitOfWork` |
| `ITenantContext` | Scoped | `TenantContext` (populated by middleware) |
| `ITenantConnectionResolver` | Scoped | `TenantConnectionResolver` |
| `SmreaderDbContext` | Scoped | Dynamic via `ITenantContext.ConnectionString` |
| `TenantDapperContext` | Scoped | Dynamic tenant connection |
| `IUnitOfWork` | Scoped | `UnitOfWork` (tenant-scoped) |
| `IAuthService` | Scoped | `AuthService` (updated for multi-tenant) |
| `ICacheService` | Singleton | `CacheService` (unchanged) |

---

## 7. Configuration (appsettings.json)

```json
{
  "ConnectionStrings": {
    "MasterConnection": "Server=localhost;Port=3306;Database=SmreaderMasterDB;Uid=root;Pwd=password;"
  },
  "Jwt": {
    "Key": "YOUR_SECRET_KEY_AT_LEAST_32_CHARACTERS",
    "Issuer": "SmreaderAPI",
    "Audience": "SmreaderAPI",
    "AccessTokenExpiryMinutes": 30,
    "RefreshTokenExpiryDays": 7
  },
  "Cache": {
    "L1ExpiryMinutes": 5,
    "L2ExpiryMinutes": 30,
    "TenantConnectionTtlMinutes": 30
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

---

## 8. API Endpoints

### Authentication

| Endpoint | Method | Auth | Body | Description |
|----------|--------|------|------|-------------|
| `/api/auth/login` | POST | No | `{ tenantCode, email, password }` | Login with tenant selection |
| `/api/auth/register` | POST | No | `{ name, email, password }` | Register new user (uses default tenant/FY) |
| `/api/auth/refresh-token` | POST | No | `{ refreshToken }` | Get new access token |
| `/api/auth/switch-fy` | POST | Yes | `{ financialYear }` | Switch to different financial year |

### Users

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/users` | GET | Yes | Get all users (from current tenant DB) |
| `/api/users/{id}` | GET | Yes | Get user by ID |
| `/api/users/{id}` | PUT | Yes (Admin) | Update user |
| `/api/users/{id}` | DELETE | Yes (Admin) | Delete user |

### Roles

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/roles` | GET | Yes | Get all roles |
| `/api/roles/{id}` | GET | Yes | Get role by ID |
| `/api/roles` | POST | Yes (Admin) | Create role |
| `/api/roles/{id}` | PUT | Yes (Admin) | Update role |
| `/api/roles/{id}` | DELETE | Yes (Admin) | Delete role |

### Audit Logs

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/auditlog` | GET | Yes (Admin) | Get all audit logs |
| `/api/auditlog/entity/{entityName}/{entityId}` | GET | Yes (Admin) | Get logs for entity |
| `/api/auditlog/user/{userId}` | GET | Yes (Admin) | Get logs for user |

---

## 9. Security

### Current Implementation
✅ Refresh tokens in master DB only (centralized)  
✅ Refresh token rotation on every use  
✅ Short-lived JWT (30 min)  
✅ Tenant isolation enforced at middleware level  
✅ BCrypt password hashing (work factor 12)  

### Current Constraints
⚠️ Connection strings plain text in master DB (encryption deferred)  
⚠️ Refresh tokens in localStorage (HttpOnly cookie migration deferred)  
⚠️ Users exist only in default FY database (not synced across FYs)  

---

## 10. Limitations & Future Improvements

### Current Limitations
- Users exist only in tenant's default FY database
- User data not synced across FY databases
- Password changes in one FY don't propagate to others
- Tenant list is pre-configured (no self-registration)
- FY database creation is manual DBA work

### Planned Improvements
- [ ] Encrypt connection strings (AES/Data Protection API)
- [ ] Move users to master DB (centralized auth)
- [ ] HttpOnly secure cookies for refresh tokens
- [ ] Automatic FY database provisioning
- [ ] Multi-device session management
- [ ] Token usage audit logging
- [ ] Public tenant listing API (optional)

---

## 11. Deployment

### Migration from Single DB to Multi-Tenant

```bash
# 1. Create master database
mysql -u root -p < scripts/master_init.sql

# 2. Run migration script
mysql -u root -p < scripts/migrate_to_multitenant.sql

# 3. Update appsettings.json with MasterConnection

# 4. Restart API application

# 5. Test login with tenantCode: "default"
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"tenantCode":"default","email":"admin@example.com","password":"password"}'

# 6. Verify JWT contains tenant_id and fy claims
```

### Creating New Tenant FY Database

```bash
# 1. Create database using template
mysql -u root -p < scripts/tenant_init.sql
# (Replace {DATABASE_NAME} with actual name, e.g., smreader_tenant2_fy2024_25)

# 2. Register in master DB
INSERT INTO tenant_databases (TenantId, FinancialYear, ConnectionString, IsDefault)
VALUES (2, '2024-25', 'Server=localhost;Port=3306;Database=smreader_tenant2_fy2024_25;Uid=root;Pwd=password;', 1);

# 3. Invalidate cache
# Connection will be resolved on next request
```

---

## 12. Testing Checklist

- [ ] Master DB created successfully
- [ ] Tenant DB created from template
- [ ] Login with tenantCode returns JWT with tenant_id + fy
- [ ] Refresh token validated against master DB
- [ ] Switch FY generates new JWT with updated fy
- [ ] Tenant isolation: user from tenant1 cannot access tenant2 data
- [ ] Connection string caching works (check logs)
- [ ] Cache miss falls back to master DB
- [ ] Middleware skips unauthenticated endpoints
- [ ] Protected endpoints resolve correct tenant DB

---

**End of Specification**
