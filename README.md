# CodeCafe

CodeCafe is an open-source notebook workspace for engineers. It combines a React frontend, an ASP.NET Core backend, PostgreSQL persistence, and an OAuth-protected MCP adapter so the same notebooks can be used from the browser and from MCP clients.

## What Exists Today

- Structured notebooks with folders and pages
- Rich-text page editing backed by TipTap JSON
- Private, unlisted, and public sharing
- Search, favorites, archiving, and public notebook browsing
- In-app AI notebook assistant for notebook Q&A, note summaries, outlines, rewrites, and AI draft creation
- OAuth/OpenIddict-backed MCP tools, resources, prompts, and upload sessions

`Codes` is still a planned surface in the UI, but notebooks and the in-app AI assistant are production-ready surfaces today.

## Repository Layout

```text
CodeCafe/
├─ src/
│  ├─ CodeCafe.Domain/
│  ├─ CodeCafe.Application/
│  ├─ CodeCafe.Infrastructure/
│  ├─ CodeCafe.Api/
│  ├─ CodeCafe.Ai/
│  ├─ CodeCafe.Mcp/
│  └─ CodeCafe.Server/
├─ tests/
│  ├─ CodeCafe.Api.Tests/
│  ├─ CodeCafe.Application.Tests/
│  ├─ CodeCafe.Architecture.Tests/
│  ├─ CodeCafe.Infrastructure.Tests/
│  ├─ CodeCafe.Mcp.Tests/
│  └─ CodeCafe.Server.Tests/
├─ frontend/
├─ deploy/
├─ scripts/
└─ tools/
```

`CodeCafe.Server` is the only runnable backend host. `CodeCafe.Api`, `CodeCafe.Ai`, and `CodeCafe.Mcp` are adapter libraries composed by that host.

## Quick Start

1. Install .NET 10, Node 24, and PostgreSQL 16+.
2. Create a local `codecafe` database and user.
3. Add `src/CodeCafe.Server/appsettings.Development.json` with your local connection string.
4. Run `dotnet run --project src/CodeCafe.Server`.
5. Run `npm ci` and `npm run dev` inside `frontend/`.

The detailed setup, testing, and migration flow lives in [docs/development.md](docs/development.md).

## Documentation

- [docs/development.md](docs/development.md): local setup, testing, migrations, runtime config
- [docs/architecture.md](docs/architecture.md): system boundaries, auth model, notebook contract conventions
- [docs/backend-best-practices.md](docs/backend-best-practices.md): backend layering rules, slice guidance, adapter boundaries, review checklist
- [docs/mcp.md](docs/mcp.md): MCP endpoint, scopes, tools, resources, prompts, upload flow
- [docs/operations.md](docs/operations.md): CI/CD, Helm, environments, runtime secrets, database maintenance

## Common Commands

Backend:

```powershell
dotnet restore CodeCafe.slnx
dotnet build CodeCafe.slnx --configuration Release
dotnet test CodeCafe.slnx --configuration Release --no-build
```

Frontend:

```powershell
cd frontend
npm ci
npm run test
npm run lint
npm run build
```

## License

This project is licensed under the MIT License.
