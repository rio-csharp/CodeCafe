# Operations

## Deployment Targets

Backend deploy target:

```text
server/Host/CodeCafe.Server/CodeCafe.Server.csproj
```

Frontend deploy target:

```text
clients/web/
```

Container images are built from:

- `server/Host/CodeCafe.Server/Dockerfile`
- `clients/web/Dockerfile`

## GitHub Workflow

### Branch Rules

- Feature work is done on `feature/*`.
- Feature PRs target `release/*`.
- Release PRs target `main`.
- Only `release/* -> main` is allowed.
- `main` is protected by required checks. The required checks include backend/frontend CI, `Enforce release-only merge to main`, and `Release E2E`.

### Workflow Map

| Workflow | Trigger | Purpose |
| --- | --- | --- |
| `check-pr-base.yml` | PRs to `main` or `release/*` | Requires the PR branch to be based on the latest target branch head. |
| `enforce-branch-protection.yml` | PRs to `main` | Rejects anything except same-repository `release/* -> main` PRs. |
| `ci.yml` | Pushes to `main`, `release/*`, `feature/*`; PRs to `main` or `release/*`; manual | Runs backend/frontend verification and publishes deployment inputs when the ref qualifies. |
| `release-e2e.yml` | Pushes to `release/*`; PRs to `release/*`; manual | Runs Playwright E2E against a locally published API and Postgres service. |
| `deploy-pr.yml` | Successful CI runs from `feature/*`; manual | Deploys PR preview environments for open same-repository PRs targeting `release/*`. |
| `cleanup-pr.yml` | Closed PRs to `release/*`; weekly schedule; manual | Deletes a specific PR preview namespace or stale preview namespaces. |
| `deploy-test.yml` | Successful CI runs from `release/*`; manual | Deploys the shared test environment. |
| `deploy-production.yml` | Successful CI runs from `main`; manual | Deploys production from CI artifacts and images. |
| `rollback.yml` | Manual | Rolls production back to the previous or requested Helm revision. |

### CI

`/.github/workflows/ci.yml` always runs the backend and frontend verification jobs:

- restores, builds, and tests the .NET solution
- installs frontend dependencies
- runs frontend tests when a frontend test script exists
- runs frontend lint
- builds the frontend production bundle

CI publishes deployment inputs only when the artifact policy allows it:

- pushes to `main`
- pushes to `release/*`
- pushes to `feature/*` that have an open PR targeting `release/*`
- non-fork PRs targeting `release/*`
- manual runs

Published deployment inputs are:

- backend and frontend GHCR images
- Helm chart artifact
- CD script artifact
- deployment metadata containing the image tag and source ref

### Feature PR Preview Flow

1. A `feature/*` branch is pushed.
2. CI runs backend/frontend checks.
3. If the feature branch has an open PR targeting `release/*`, CI publishes deployment inputs with a `pr-<number>-<sha>` image tag.
4. `deploy-pr.yml` runs after CI succeeds.
5. The preview resolver verifies that the PR is still open, targets `release/*`, comes from this repository, and still points at the CI commit.
6. The preview deployment creates or updates namespace `codecafe-pr-<number>`.
7. The workflow checks again that the PR still points at the deployed commit.
8. If the PR changed while deployment was running, the stale preview is deleted.
9. If the deployment is still current, the workflow updates the PR preview comment.

Closed PRs targeting `release/*` run `cleanup-pr.yml` and delete their preview namespace when it exists. The scheduled cleanup job also removes preview namespaces whose PRs are no longer open.

### Release And Test Flow

1. A `release/*` branch is pushed.
2. CI runs backend/frontend checks and publishes deployment inputs with a `test-<sha>` image tag.
3. `release-e2e.yml` runs Playwright E2E against a local API and Postgres service.
4. `deploy-test.yml` runs after CI succeeds and deploys the shared `codecafe-test` namespace.
5. The test deployment uses `deploy-environment.sh`, which sends the Helm chart and CD scripts to the test host.
6. `deploy-helm.sh` applies secrets, deploys through Helm, waits for frontend/API rollout, and smoke tests frontend, API readiness, and MCP protected-resource metadata.

The test environment has one shared instance. `deploy-test.yml` does not cancel an in-progress Helm deploy; GitHub keeps only the latest pending run for the `deploy-test` concurrency group.

### Release To Production Flow

1. A release PR targets `main`.
2. `check-pr-base.yml` requires the release branch to be based on the latest `main`.
3. `enforce-branch-protection.yml` requires the PR source to be a same-repository `release/*` branch.
4. GitHub branch protection/rulesets require backend/frontend CI, `Enforce release-only merge to main`, and `Release E2E`.
5. After the release PR is merged, CI runs on `main` and publishes deployment inputs with a `production-<sha>` image tag.
6. `deploy-production.yml` runs after CI succeeds and deploys the `codecafe-prod` namespace.

Production deploy and rollback share the `production-deployment` concurrency group so they do not run at the same time.

### Manual Deploys And Rollback

Manual test, PR preview, and production deploys require a CI workflow run id. The resolver verifies that the run is a successful `CI` run from the expected branch family before downloading artifacts.

Manual production rollback uses `rollback.yml`. It copies rollback scripts to the production host, runs Helm rollback, waits for frontend/API rollout, and smoke tests frontend plus API readiness.

## Helm And Deploy Script

Helm chart location:

```text
deploy/helm/codecafe
```

Deployment helper:

```text
scripts/cd/deploy-helm.sh
```

The deploy script:

- creates or updates the target namespace
- copies the shared TLS secret into the target namespace
- seeds the OAuth secret when certificate values are provided
- builds an API config secret from database and OAuth settings
- deploys frontend and API images through Helm
- waits for frontend `/`, API readiness, AI status, and MCP protected-resource metadata to respond

## Runtime Configuration

### Backend

Important backend runtime settings come from environment variables or secrets:

- `ConnectionStrings__DefaultConnection`
- `AuthorizationServer__Issuer`
- `AuthorizationServer__FrontendBaseUrl`
- signing/encryption certificate values or paths
- MCP settings such as audience, scopes, and allowed origins
- AI settings when the in-app assistant is enabled:
  - `Ai__Enabled=true`
  - `Ai__Model`
  - `Ai__ApiKey`
  - optional limits such as `Ai__MaxToolResults`, `Ai__MaxToolContentChars`, `Ai__MaxDraftPromptChars`, `Ai__MaxDraftContextChars`, and `Ai__MaxDraftOutputTokens`

The Helm chart exposes non-sensitive AI values under `api.ai`. Keep `Ai__ApiKey` in a Kubernetes secret or pass it to `scripts/cd/deploy-helm.sh` through `AI_API_KEY`; do not commit it to values files.

The MCP adapter is enabled by default (`Mcp:Enabled` is `true` in `appsettings.json`). The Helm chart exposes `api.mcp.enabled` (default `true`, preserving that behavior) and `api.mcp.allowedOrigins`; set `api.mcp.enabled=false` — or `Mcp__Enabled=false` outside Helm — to turn the `/mcp` endpoint and its OAuth protected-resource metadata off. Allowed origins are passed through as `Mcp__AllowedOrigins__<index>` and must be absolute HTTP or HTTPS origins.

`scripts/cd/deploy-helm.sh` supports:

- `AI_ENABLED`, default `false`
- `AI_MODEL`, required when `AI_ENABLED=true`
- `AI_API_KEY`, required when `AI_ENABLED=true`
- `AI_BASE_URL`, optional OpenAI-compatible base URL; root URLs are normalized to `/v1`
- `AI_ENDPOINT_PATH`, `AI_STATUS_ENDPOINT_PATH`, `AI_DRAFT_ENDPOINT_PATH`, and AI limit overrides

Current deployments may still expose draft-specific variables because the transitional draft endpoint is still part of the runtime status and configuration surface. New AI notebook editing work should still target backend-managed TipTap JSON proposals and direct-save flows rather than treating Markdown drafts as the long-term architecture.

`AI_STATUS_ENDPOINT_PATH` is also passed to the frontend runtime config so browser capability discovery stays aligned with the backend endpoint. The deploy smoke test checks frontend `/`, API readiness, AI status, and MCP protected-resource metadata.

### Frontend

The frontend image writes `window.__CODECAFE_CONFIG__` at container startup through:

```text
clients/web/docker-entrypoint.d/10-write-runtime-config.sh
```

That runtime config provides `apiBaseUrl` and `aiStatusEndpointPath` without rebuilding the frontend image per environment.

## Database Migrations

Published-host migration command:

```powershell
dotnet CodeCafe.Server.dll migrate
```

Development startup applies migrations automatically. Helm also includes an API migration job template.

Migration `20260718224927_AddNotebookTrigramIndexes` enables the `pg_trgm` extension and creates GIN trigram indexes on `Notebooks.Title`, `NotebookItems.Title`, and `NotebookItems.PlainTextContent`. Enabling the extension requires a database role with `CREATE EXTENSION` privilege. The indexes are created non-concurrently, so the migration holds a write lock on `NotebookItems` while it runs. This only matters for large installs; small databases finish in milliseconds. For large installs, run the migration in a low-traffic window.

## Database Maintenance

The maintained database helper lives in:

```text
server/tools/CodeCafe.DbSync
```

Supported commands:

- `check`
- `migrate-prod`
- `migrate-test`
- `prod-to-local`
- `local-to-test`

## Shutdown And Readiness

Readiness depends on:

- process health
- database readiness outside test environment
- a drain state used during rolling deploys

The API deployment template uses a pre-stop delay so the server can fail readiness before shutdown completes, giving Kubernetes time to stop routing new traffic.
