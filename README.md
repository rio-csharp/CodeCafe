# CodeCafe

CodeCafe is a developer-focused AI workbench for chat, notes, and workspace
workflows. It is designed as an extensible platform that can grow into richer
AI, knowledge, and agent-driven experiences over time.

## Live demo

- App: [https://codes.cafe](https://codes.cafe)
- Test environment: [https://test.codes.cafe](https://test.codes.cafe)

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

## Deployment

Kubernetes deployment notes live in `docs/deployment.md`.
# test
