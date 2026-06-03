# Development

## Prerequisites

- .NET SDK 10.0.x
- Node.js 24.x
- PostgreSQL 16+

## Local Setup

### 1. Create The Database

Use PostgreSQL to create a local role and database:

```sql
CREATE USER codecafe WITH PASSWORD 'codecafe' CREATEDB;
CREATE DATABASE codecafe OWNER codecafe;
GRANT ALL PRIVILEGES ON DATABASE codecafe TO codecafe;
```

### 2. Configure The Backend

Create `src/CodeCafe.Server/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=codecafe;Username=codecafe;Password=codecafe"
  }
}
```

That is enough for most local work. In Development, the host fills in default values for:

- `AuthorizationServer:Issuer` as `https://localhost:7239/`
- `AuthorizationServer:FrontendBaseUrl` as `http://localhost:5173`
- `Cors:AllowedOrigins` for the Vite dev server

### 3. Configure The Frontend

Create `frontend/.env` from `frontend/.env.example`.

Default direct-to-backend mode:

```env
VITE_API_BASE_URL=https://localhost:7239
```

If you prefer to use the Vite proxy instead, leave it empty:

```env
VITE_API_BASE_URL=
```

## Run The App

### Backend

Restore and run the combined backend host:

```powershell
dotnet restore CodeCafe.slnx
dotnet run --project src/CodeCafe.Server
```

The checked-in development launch profile uses:

- HTTP: `http://localhost:5042`
- HTTPS: `https://localhost:7239`

Health endpoints:

```text
GET http://localhost:5042/health/live
GET http://localhost:5042/health/ready
```

In Development, migrations run automatically at startup.

### Frontend

```powershell
cd frontend
npm ci
npm run dev
```

The frontend runs on `http://localhost:5173`.

If `VITE_API_BASE_URL=` is blank, `/api` requests are proxied to `http://localhost:5042`.

## Testing And Quality Checks

### Backend

```powershell
dotnet build CodeCafe.slnx --configuration Release
dotnet test CodeCafe.slnx --configuration Release --no-build
```

On Windows, if multiple backend test projects compete for `obj` files, prefer running test projects serially instead of in parallel.

### Frontend

```powershell
cd frontend
npm run test
npm run lint
npm run build
```

For browser automation:

```powershell
cd frontend
npm run e2e
```

E2E assumes the frontend is available on `http://localhost:5173` and the backend is reachable on `http://localhost:5042`.

## EF Core And Migrations

Use `CodeCafe.Infrastructure` as the migrations project and `CodeCafe.Server` as the startup project:

```powershell
dotnet ef database update `
  --project src/CodeCafe.Infrastructure/CodeCafe.Infrastructure.csproj `
  --startup-project src/CodeCafe.Server/CodeCafe.Server.csproj `
  --context ApplicationDbContext
```

Manual migration execution from the published server host:

```powershell
dotnet CodeCafe.Server.dll migrate
```

## Development Notes

- `CodeCafe.Server` is the only backend entrypoint for local run, publish, deploy, and migrations.
- The frontend runtime reads `window.__CODECAFE_CONFIG__.apiBaseUrl` first, then falls back to `VITE_API_BASE_URL`.
- Browser API writes use CSRF protection. The shared frontend API client handles token fetch and retry automatically.
- `scripts/README.md` documents the maintained database-sync helper in `tools/CodeCafe.DbSync`.
