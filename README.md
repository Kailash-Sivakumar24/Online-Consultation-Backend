# Online Consultation Backend Service

A .NET 8 backend for an online doctor consultation platform.

## Tech Stack

- **Runtime**: .NET 8 / ASP.NET Core Web API
- **ORM**: Entity Framework Core 8 with PostgreSQL (Npgsql)
- **Auth**: JWT (HS256) + BCrypt password hashing
- **Real-Time**: SignalR (strongly-typed hub)
- **Validation**: FluentValidation
- **Mapping**: AutoMapper
- **Logging**: Serilog (console + rolling file)
- **Testing**: xUnit, Moq, Testcontainers (PostgreSQL), FluentAssertions

## Project Structure

```
ConsultationApi/           # ASP.NET Core Web API host
ConsultationApi.Core/      # Domain entities, DTOs, interfaces, exceptions
ConsultationApi.Infrastructure/  # EF Core, repositories, business services
ConsultationApi.Tests/     # xUnit unit + integration tests
```

## Prerequisites

- .NET 8 SDK
- PostgreSQL 14+
- Docker (for integration tests with Testcontainers)

## Setup

### 1. Clone and restore

```bash
git clone <repo-url>
cd "Online Consultation Backend Service"
dotnet restore
```

### 2. Configure secrets (local development)

Never commit secrets. Use `dotnet user-secrets` for local development:

```bash
cd ConsultationApi
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=consultation;Username=postgres;Password=yourpassword"
dotnet user-secrets set "JwtSettings:SecretKey" "your-minimum-32-character-secret-key-here"
dotnet user-secrets list || dotnet user-secrets list --project ConsultationApi
```

### 3. Required Environment Variables (production)

| Variable | Description |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `JwtSettings__SecretKey` | JWT signing key (min 32 chars) |
| `JwtSettings__Issuer` | JWT issuer (default: `ConsultationApi`) |
| `JwtSettings__Audience` | JWT audience (default: `ConsultationApiUsers`) |
| `JwtSettings__AccessTokenExpiryMinutes` | Access token TTL (default: 15) |
| `JwtSettings__RefreshTokenExpiryDays` | Refresh token TTL (default: 7) |

## Running Migrations

Install the EF Core tools if not already installed:

```bash
dotnet tool install --global dotnet-ef
```

Create a migration (from the solution root):

```bash
dotnet ef migrations add InitialCreate --project ConsultationApi.Infrastructure --startup-project ConsultationApi
```

Apply migrations:

```bash
dotnet ef database update --project ConsultationApi.Infrastructure --startup-project ConsultationApi
```

> **Note**: Never modify an already-applied migration. Always create a new one for schema changes.

## Running the API

```bash
cd ConsultationApi
dotnet run
```

Swagger UI is available at `https://localhost:xxxx/swagger` in Development mode.

## Running Tests

### Unit tests only (no Docker needed)

```bash
dotnet test --filter "FullyQualifiedName~Unit"
```

### All tests (follow Integration test steps given below to run All tests - no Docker)

```bash
dotnet test
```

### Integration tests (use local PostgreSQL — no Docker)

You can run integration tests against a local PostgreSQL instance by setting the `INTEGRATION_DB` environment variable. The test harness will use that connection string and skip Testcontainers.

1) Create database and user (PowerShell, replace passwords):

```powershell
# If PostgreSQL requires the superuser password for these commands, set it for this session:
$env:PGPASSWORD = 'your-postgres-password'

& 'C:\Program Files\PostgreSQL\18\bin\psql.exe' -U postgres -h localhost -p 5432 -c "CREATE DATABASE integration_tests;"
& 'C:\Program Files\PostgreSQL\18\bin\psql.exe' -U postgres -h localhost -p 5432 -c "CREATE USER integration_user WITH PASSWORD 'YourStrongP@ss';"

# Grant schema privileges to "integration_user"
& 'C:\Program Files\PostgreSQL\18\bin\psql.exe' -U postgres -d integration_tests -c "GRANT ALL PRIVILEGES ON SCHEMA public TO integration_user;"
& 'C:\Program Files\PostgreSQL\18\bin\psql.exe' -U postgres -d integration_tests -c "GRANT ALL PRIVILEGES ON DATABASE integration_tests TO integration_user;"
& 'C:\Program Files\PostgreSQL\18\bin\psql.exe' -U postgres -d integration_tests -c "GRANT CREATE ON DATABASE integration_tests TO integration_user;"

# remove temporary superuser password from the environment
Remove-Item Env:PGPASSWORD
```

2) Set the connection string for the current session (PowerShell):

```powershell
$env:INTEGRATION_DB = "Host=localhost;Port=5432;Database=integration_tests;Username=integration_user;Password=your-postgres-password"

# To persist for new shells (optional):
[Environment]::SetEnvironmentVariable('INTEGRATION_DB', $env:INTEGRATION_DB, 'User')
```

3) Ensure JWT secret is long enough (HS256 requires >256 bits). Generate and set a secure secret for the session:

```powershell
# generate a 64‑byte (512‑bit) base64 secret, set it for this session, persist it for your user, and print it
$rng=[System.Security.Cryptography.RandomNumberGenerator]::Create()
$bytes=New-Object byte[] 64
$rng.GetBytes($bytes)
$secret=[Convert]::ToBase64String($bytes)
$env:JwtSettings__SecretKey=$secret
[Environment]::SetEnvironmentVariable('JwtSettings__SecretKey',$secret,'User')
Write-Host "Secret (base64): $secret"

# verify decoded length (should be 64 bytes):
[Convert]::FromBase64String($env:JwtSettings__SecretKey).Length
```

4) Run integration tests (uses the in-session `INTEGRATION_DB` and `JwtSettings__SecretKey`):

```powershell
dotnet test ./ConsultationApi.Tests/ConsultationApi.Tests.csproj --filter "FullyQualifiedName~Integration" --verbosity normal
```

### With coverage report

```bash
dotnet test --collect:"XPlat Code Coverage"
# Then generate HTML report with reportgenerator:
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```
```or bash (coverage report for only Core and Infrastructure layer)
dotnet test --settings ConsultationApi.Tests/coverage.runsettings
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

## API Endpoints

### Authentication
| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | — | Register as Patient or Doctor |
| POST | `/api/auth/login` | — | Login, returns JWT + refresh token |
| POST | `/api/auth/refresh` | — | Rotate tokens |
| POST | `/api/auth/logout` | JWT | Revoke refresh token |

### Doctors
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/doctors` | — | List with filters (specialization, minRating, maxFee) |
| GET | `/api/doctors/{id}` | — | Doctor profile |
| GET | `/api/doctors/{id}/slots` | — | Available booking slots |

### Appointments
| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/appointments` | Patient | Book appointment |
| GET | `/api/appointments` | JWT | My appointments |
| GET | `/api/appointments/{id}` | JWT | Appointment detail |
| PUT | `/api/appointments/{id}/confirm` | Doctor/Admin | Confirm |
| PUT | `/api/appointments/{id}/cancel` | Patient/Admin | Cancel |

### Sessions
| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/sessions/{id}/start` | Doctor | Start session |
| POST | `/api/sessions/{id}/end` | Doctor | End session |
| POST | `/api/sessions/{id}/messages` | JWT | Send message |
| GET | `/api/sessions/{id}/messages` | JWT | Get all messages |

### Prescriptions
| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/prescriptions` | Doctor | Issue prescription |
| GET | `/api/prescriptions/{id}` | JWT | Get prescription |
| GET | `/api/patients/me/prescriptions` | Patient | My prescriptions |

### Reviews & Notifications
| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/reviews` | Patient | Submit review |
| GET | `/api/notifications` | JWT | My notifications |
| PUT | `/api/notifications/{id}/read` | JWT | Mark as read |

## Real-Time (SignalR)

Connect to `/hubs/consultation?access_token=<JWT>`.

Client methods:
- `ReceiveMessage(ChatMessageDto)` — new chat message in session
- `ReceiveNotification(NotificationDto)` — new notification
- `SessionStatusChanged(sessionId, status)` — session started/ended

## Seeded Test Accounts

| Email | Password | Role |
|---|---|---|
| admin@consultation.com | Admin@123 | Admin |
| doctor@consultation.com | Doctor@123 | Doctor |
| patient@consultation.com | Patient@123 | Patient |

> These are only created on first migration. Change passwords after deployment.

## Logs

Application logs are written to `logs/consultation-YYYYMMDD.log` with daily rolling and also to stdout.
