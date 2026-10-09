# CodeCafe

CodeCafe is a notebook application with a React single-page client and an ASP.NET Core API.
The API persists data in PostgreSQL and also exposes health, OpenAPI, and MCP endpoints.

## Repository layout

- `clients/web/` — React 19, TypeScript, Vite, and Tailwind CSS.
- `server/src/` — Domain, Application, Infrastructure, and Host projects targeting .NET 10.
- `server/tests/` — domain, application, infrastructure, and HTTP host tests.
- `docs/` — architecture decisions and historical frontend milestone specifications.

## Prerequisites

- Node.js from `.node-version`.
- pnpm 10.33.4, matching `clients/web/package.json`.
- .NET SDK compatible with `global.json`.
- Docker with Compose, for local PostgreSQL and infrastructure tests.

Enable Corepack before installing frontend packages:

```bash
corepack enable
corepack prepare pnpm@10.33.4 --activate
```

## Run locally

Start PostgreSQL from the repository root:

```bash
docker compose up -d --wait postgres
```

The Compose service binds PostgreSQL to loopback only. Override the host port when 5432 is
already occupied:

```bash
CODECAFE_POSTGRES_PORT=55432 docker compose up -d --wait postgres
```

In one terminal, configure development-only values and start the API:

```bash
cd server
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=codecafe;Username=codecafe;Password=codecafe'
export Auth__Jwt__SigningKey='codecafe-local-development-signing-key-change-me'
dotnet run --project src/CodeCafe.Host
```

If the Compose port was overridden, use the same port in `ConnectionStrings__DefaultConnection`.
The signing key above is an example for local development only. Production must supply its own
secret through the deployment environment or secret store; do not commit it to configuration.

In another terminal, install and start the web client:

```bash
cd clients/web
pnpm install --frozen-lockfile
pnpm dev
```

Open `http://localhost:5173`. Vite proxies `/api` to the API at
`http://localhost:5258`.

Stop the development database with `docker compose down`. Add `--volumes` only when you also
intend to delete the local database contents.

## Verify changes

Frontend checks, from `clients/web/`:

```bash
pnpm lint
pnpm typecheck
pnpm test
pnpm build
```

Backend checks, from `server/`:

```bash
dotnet build CodeCafe.slnx
dotnet test --solution CodeCafe.slnx --minimum-expected-tests 1 --no-banner
```

Infrastructure tests start disposable PostgreSQL containers, so Docker must be available.
CI runs the same frontend and backend gates on every pull request and on pushes to `main` or a
`release/**` branch. Repository settings determine whether those checks are required before merging.

## Browser smoke check

`scripts/browser-smoke.js` accepts a Playwright `page` through an external browser runtime
(for example, the `browser_run_code_unsafe` filename option). It checks registration, notebook
creation, page editing and persistence, logout, anonymous reading, keyboard navigation, and
responsive layouts against the real local API. Run it only with a disposable local database;
it creates test users and notebooks. Browser checks are not yet part of CI, and no `e2e` npm
script or additional browser dependency is installed.

## Deployment boundary

`docker-compose.yml` is a local database convenience, not a production deployment definition.
This repository currently does not contain production container images or orchestration manifests;
the deployment system must provide and version those artifacts alongside the application release.
