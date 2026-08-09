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

Create `server/src/CodeCafe.Host/appsettings.Development.json`:

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

To enable the in-app AI notebook assistant locally, add the AI settings to the same file or use user secrets/environment variables:

```json
{
  "Ai": {
    "Enabled": true,
    "Model": "gpt-4.1-mini",
    "ApiKey": "<your OpenAI API key>",
    "BaseUrl": ""
  }
}
```

When enabled, the backend exposes:

- `GET /api/ai/status` for frontend capability discovery
- `POST /api/ai/assistant` for the AG-UI notebook chat assistant
- `POST /api/ai/edits` for backend-managed notebook edit proposals and direct-save flows
- `POST /api/ai/drafts` as the current transitional draft-generation endpoint

The assistant reads notebooks through the signed-in user's session. Today, the AI status payload still includes both the assistant endpoint and the transitional draft endpoint so the existing frontend can discover both capabilities.

New AI write work should not treat Markdown drafts as the long-term write format. Generated notebook changes should ultimately be TipTap JSON or structured operations that the backend validates as TipTap JSON before saving.

### 3. Configure The Frontend

Create `clients/web/.env` from `clients/web/.env.example`.

Default direct-to-backend mode:

```env
VITE_API_BASE_URL=https://localhost:7239
VITE_AI_STATUS_ENDPOINT_PATH=/api/ai/status
```

If you prefer to use the Vite proxy instead, leave it empty:

```env
VITE_API_BASE_URL=
```

## Run The App

### Backend

Restore and run the combined backend host:

```powershell
dotnet restore server/CodeCafe.slnx
dotnet run --project server/src/CodeCafe.Host
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
cd clients/web
npm ci
npm run dev
```

The frontend runs on `http://localhost:5173`.

If `VITE_API_BASE_URL=` is blank, `/api` requests are proxied to `http://localhost:5042`.
Keep `VITE_AI_STATUS_ENDPOINT_PATH` aligned with `Ai:StatusEndpointPath` when you customize the AI status endpoint.
For OpenAI-compatible routers, set `Ai:BaseUrl` to the provider base URL; root URLs are normalized to `/v1`.

## Testing And Quality Checks

### Backend

```powershell
dotnet build server/CodeCafe.slnx --configuration Release
dotnet test server/CodeCafe.slnx --configuration Release --no-build
```

On Windows, if multiple backend test projects compete for `obj` files, prefer running test projects serially instead of in parallel.

### Frontend

```powershell
cd clients/web
npm run test
npm run lint
npm run build
```

For browser automation:

```powershell
cd clients/web
npm run e2e
```

E2E assumes the frontend is available on `http://localhost:5173` and the backend is reachable on `http://localhost:5042`.

## EF Core And Migrations

Use `CodeCafe.Shared.Infrastructure` as the current migrations project and `CodeCafe.Server` as the startup project:

```powershell
dotnet ef database update `
  --project server/Shared/CodeCafe.Shared.Infrastructure/CodeCafe.Shared.Infrastructure.csproj `
  --startup-project server/src/CodeCafe.Host/CodeCafe.Host.csproj `
  --context ApplicationDbContext
```

Manual migration execution from the published server host:

```powershell
dotnet CodeCafe.Host.dll migrate
```

## Development Notes

- `CodeCafe.Server` is the only backend entrypoint for local run, publish, deploy, and migrations.
- The frontend runtime reads `window.__CODECAFE_CONFIG__.apiBaseUrl` first, then falls back to `VITE_API_BASE_URL`.
- Browser API writes use CSRF protection. The shared frontend API client handles token fetch and retry automatically.
- AI is disabled by default. Set `Ai:Enabled`, `Ai:Model`, and `Ai:ApiKey` only in local user secrets, environment variables, or deployment secrets.
- Current AI configuration still includes `Ai:DraftEndpointPath`, `Ai:MaxDraftPromptChars`, `Ai:MaxDraftContextChars`, and `Ai:MaxDraftOutputTokens` for the transitional draft flow.
- Notebook page content is stored as TipTap JSON. Markdown is allowed only as an import/upload input that is converted before persistence.
