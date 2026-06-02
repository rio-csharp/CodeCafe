# Operations

## Deployment Targets

Backend deploy target:

```text
src/CodeCafe.Server/CodeCafe.Server.csproj
```

Frontend deploy target:

```text
frontend/
```

Container images are built from:

- `src/CodeCafe.Server/Dockerfile`
- `frontend/Dockerfile`

## Environments And Branch Flow

Current GitHub Actions flow:

- `feature/*` branches can produce preview environments when attached to PRs targeting `release/*`
- pushes to `release/*` feed the test deployment flow
- pushes to `main` feed the production deployment flow
- only `release/* -> main` is allowed for merges into `main`

## CI Pipeline

`/.github/workflows/ci.yml` does the following:

- restores, builds, and tests the .NET solution
- installs frontend dependencies
- runs frontend tests, lint, and production build
- runs Playwright E2E against the built backend and frontend
- publishes API, frontend, Helm, and CI-script artifacts when the ref qualifies
- builds and pushes GHCR images for the backend and frontend

## Helm And Deploy Script

Helm chart location:

```text
deploy/helm/codecafe
```

Deployment helper:

```text
scripts/ci/deploy-helm.sh
```

The deploy script:

- creates or updates the target namespace
- copies the shared TLS secret into the target namespace
- seeds the OAuth secret when certificate values are provided
- builds an API config secret from database and OAuth settings
- deploys frontend and API images through Helm
- waits for frontend `/`, API readiness, and MCP protected-resource metadata to respond

## Runtime Configuration

### Backend

Important backend runtime settings come from environment variables or secrets:

- `ConnectionStrings__DefaultConnection`
- `AuthorizationServer__Issuer`
- `AuthorizationServer__FrontendBaseUrl`
- signing/encryption certificate values or paths
- MCP settings such as audience, scopes, and allowed origins

### Frontend

The frontend image writes `window.__CODECAFE_CONFIG__` at container startup through:

```text
frontend/docker-entrypoint.d/10-write-runtime-config.sh
```

That runtime config provides `apiBaseUrl` without rebuilding the frontend image per environment.

## Database Migrations

Published-host migration command:

```powershell
dotnet CodeCafe.Server.dll migrate
```

Development startup applies migrations automatically. Helm also includes an API migration job template.

## Database Maintenance

The maintained database helper lives in:

```text
tools/CodeCafe.DbSync
```

Supported commands:

- `check`
- `migrate-prod`
- `migrate-test`
- `prod-to-local`
- `local-to-test`

There is also a scheduled/manual workflow at `/.github/workflows/sync-prod-to-test-db.yml` for refreshing the test database from production.

## Shutdown And Readiness

Readiness depends on:

- process health
- database readiness outside test environment
- a drain state used during rolling deploys

The API deployment template uses a pre-stop delay so the server can fail readiness before shutdown completes, giving Kubernetes time to stop routing new traffic.
