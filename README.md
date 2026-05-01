# CodeCafe

CodeCafe is an AI-workbench-ready developer knowledge platform. Notes are the
first domain module, with authentication, workspaces, audit, and deep MAF
integration planned as platform capabilities.

## Repository layout

```text
backend/
  src/
    CodeCafe.Api/
    CodeCafe.Application/
    CodeCafe.Contracts/
    CodeCafe.Domain/
    CodeCafe.Infrastructure/
frontend/
  src/
    app/
    features/
    lib/
    shared/
docs/
```

## Backend

Copy `backend/src/CodeCafe.Api/appsettings.Development.example.json` to
`backend/src/CodeCafe.Api/appsettings.Development.json` for local development
settings. The real development settings file is ignored by Git so it can hold
local secrets.

```powershell
dotnet restore backend/CodeCafe.slnx
dotnet run --project backend/src/CodeCafe.Api
dotnet test backend/CodeCafe.slnx
```

The API runs on `http://localhost:5000`.

- Health check: `GET /health`
- System info: `GET /api/system/info`
- Swagger UI: `http://localhost:5000/swagger`

## Frontend

```powershell
cd frontend
npm install
npm run dev
npm run test
```

The frontend runs on `http://localhost:5173` and expects the API base URL from
`VITE_API_BASE_URL`. See `frontend/.env.example`.

## Current milestone

Issue #1 establishes the platform skeleton: Clean Architecture backend projects,
logging, global error handling, health checks, API documentation, and a React app
structure that can grow into Notes, Workspaces, Audit, and AI/MAF features.
