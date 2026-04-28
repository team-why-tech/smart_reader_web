# SmreaderAPI

A full-stack user management application built with .NET 10 and React 19, following Clean Architecture principles.

## Tech Stack

### Backend
- **.NET 10** with Clean Architecture (Domain → Application → Infrastructure → API)
- **MySQL 8.x** with Dapper micro-ORM
- **JWT** Bearer authentication with refresh token rotation
- **Redis** + in-memory two-tier caching
- **Serilog** structured logging
- **IP-based rate limiting** via AspNetCoreRateLimit
- **Docker** multi-stage build + docker-compose

### Frontend
- **React 19** with TypeScript
- **Vite** for dev server and builds
- **Tailwind CSS v4** for styling
- **React Router v7** with lazy-loaded routes and role-based guards
- **react-hook-form** + **zod** for form validation
- **Axios** with interceptors for API calls and token refresh

## Project Structure

```
├── backend/
│   ├── src/
│   │   ├── SmreaderAPI.Domain/          # Entities, repository interfaces
│   │   ├── SmreaderAPI.Application/     # DTOs, service interfaces, mappings
│   │   ├── SmreaderAPI.Infrastructure/  # Repositories, caching, auth
│   │   └── SmreaderAPI.API/             # Controllers, middleware, DI
│   ├── tests/
│   │   └── SmreaderAPI.UnitTests/       # xUnit + Moq + FluentAssertions
│   ├── Dockerfile
│   └── docker-compose.yml
└── frontend/
    └── src/
        ├── api/          # Axios client and resource modules
        ├── components/   # Layout, route guards, shared UI
        ├── context/      # Auth context provider
        ├── hooks/        # useTheme
        ├── pages/        # Dashboard, Users, Roles, AuditLogs, Login, Register
        ├── routes/       # Public, protected, admin route configs
        ├── types/        # TypeScript types mirroring backend DTOs
        └── utils/        # Token helpers
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [MySQL 8.x](https://www.mysql.com/)
- [Redis](https://redis.io/) (optional for L2 caching)
- [Docker](https://www.docker.com/) (optional)

## Getting Started

### Using Docker

```bash
cd backend
docker compose up
```

This starts the API, MySQL, and Redis together.

### Manual Setup

**Backend:**

```bash
cd backend
dotnet build
dotnet run --project src/SmreaderAPI.API
```

The API runs on the port configured in `launchSettings.json`.

**Frontend:**

```bash
cd frontend
npm install
npm run dev
```

The dev server runs at `http://localhost:5173` and proxies `/api` requests to the backend.

### Running Tests

```bash
cd backend
dotnet test
```

## API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | No | Register a new user |
| POST | `/api/auth/login` | No | Login and receive tokens |
| POST | `/api/auth/refresh-token` | No | Refresh access token |
| POST | `/api/auth/revoke-token` | Yes | Revoke refresh token |
| GET | `/api/users` | Yes | List all users |
| GET | `/api/users/{id}` | Yes | Get user by ID |
| PUT | `/api/users/{id}` | Yes | Update user |
| DELETE | `/api/users/{id}` | Admin | Delete user |
| GET | `/api/roles` | Yes | List all roles |
| POST | `/api/roles` | Admin | Create role |
| PUT | `/api/roles/{id}` | Admin | Update role |
| DELETE | `/api/roles/{id}` | Admin | Delete role |
| GET | `/api/auditlog` | Admin | List audit logs |

## Environment Configuration

Backend settings are in `appsettings.{Environment}.json`. Key configuration:

- `ConnectionStrings:DefaultConnection` — MySQL connection string
- `Jwt:Key` / `Jwt:Issuer` / `Jwt:Audience` — JWT settings
- `Redis:ConnectionString` — Redis connection string

## License

Private — all rights reserved.
