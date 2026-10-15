# Deployment

CI/CD mirrors the 3.x pipeline's behaviour, rebuilt against this codebase. Everything here is
enforced by `.github/workflows`, `deploy/helm/codecafe`, and `scripts/`.

## What runs where

| Workflow | Trigger | Effect |
|---|---|---|
| `ci.yml` | push to `main` / `release/**` / `feature/**`, PR to `main` / `release/**` | builds and tests both halves; publishes images and the Helm chart when the artifact policy says so |
| `check-pr-base.yml` | PR opened/synchronized | fails a PR that is not rebased onto the latest target branch |
| `enforce-branch-protection.yml` | PR to `main` | only `release/*` from this repository may merge into `main` |
| `deploy-test.yml` | CI succeeds on `release/**` | deploys `codecafe-test` |
| `deploy-pr.yml` | CI succeeds on `feature/**`, when an open PR targets a release branch | deploys `codecafe-pr-<n>` and comments the URL on the PR |
| `cleanup-pr.yml` | PR closed, weekly schedule | deletes preview namespaces |
| `deploy-production.yml` | CI succeeds on `main` | deploys `codecafe-prod` |
| `rollback.yml` | manual | `helm rollback` on production, then the smoke test |

There is deliberately no E2E workflow: the 3.x `release-e2e` job drove Playwright specs that this
codebase does not have yet.

`ci.yml` ends in an aggregated `ci` job so branch protection only has to require one check. The
publishing jobs (`helm-chart`, `deployment-metadata`) are skipped when the artifact policy says
nothing should be published, so the aggregate accepts `skipped` for those — but on a publishing run
both must have succeeded, otherwise the deploy workflow would download an artifact that is not
there. `cd-scripts` publishes nothing and runs unconditionally: `scripts/cd` holds the only scripts
that execute on the deploy host, so a syntax error there would otherwise surface halfway through a
deployment. Which jobs fall into which group lives in `scripts/ci/assert-ci-jobs.sh`, which also runs
standalone if you want to reproduce a decision locally.

## Topology

`scripts/cd/deploy-environment.sh` runs on the runner and `ssh`/`scp`s a Helm chart plus the
scripts in `scripts/cd` to the deploy host; `scripts/cd/deploy-helm.sh` runs **on that host**
against a k3s cluster (Traefik ingress, one namespace per environment, `kubectl`/`helm` invoked
with the k3s kubeconfig). Nothing is executed from a PR-built artifact: deploy workflows check out a
protected branch into `.trusted/` and run `scripts/cd` from there.

**The browser talks to two origins.** The SPA is served from the frontend host and sends every
`/api/...` call to the API host. `shared/api` resolves that origin in one place (`apiUrl`), reading
it from runtime configuration — the deployment mounts a ConfigMap over the image's default
`/config.js`, and `deploy-helm.sh` sets it to `https://$API_HOST`. Consequences, all of them wanted:

- the frontend image is still environment-agnostic and still needs no start-up injection, so its
  root filesystem stays read-only (unlike the 3.x chart, which wrote `config.js` at boot);
- the API host is the only CORS origin that matters, and `deploy-helm.sh` sets
  `api.cors.allowedOrigins[0]` to the frontend host. The policy exposes `Content-Disposition`
  because exports are downloaded cross-origin;
- the CSP cannot name the API origin (it is deployment-specific), so `connect-src` allows `https:`.

The frontend nginx keeps a `/api/` proxy to the fixed Service name `codecafe-api`
(`api.serviceAliasName`) as a same-origin fallback for an image started without the mounted config;
deployed environments never exercise it. Server-Sent Events (`POST /api/notebooks/{id}/ai/chat`) go
to the API host directly, and the fallback location sets `proxy_buffering off` anyway — with
buffering on, the stream would never be flushed.

## Migrations

`dotnet CodeCafe.Host.dll migrate` applies migrations and exits before Kestrel starts. Kubernetes
runs it as a `pre-install,pre-upgrade` hook Job and the deployments set
`Database__MigrateOnStartup=false`, so replicas never call `MigrateAsync` concurrently — the
production chart runs two of them. PR previews set `api.migration.enabled=false` and run against the
test database, so a preview never migrates anything.

The application still migrates on start-up by default (`Database:MigrateOnStartup`), which is what
local development and the non-Kubernetes path rely on.

## Configuration the deployment owns

`scripts/cd/deploy-helm.sh` builds a `<release>-api-config` Secret from the database connection
string (`codecafe-db-secret` in the target namespace), the AI key, `AllowedHosts`, and
`Auth__Jwt__SigningKey`. That last one is minted on first install and **reused** on every later
deploy, because the API validates it at start-up (≥ 32 bytes) and rotating it would invalidate every
session. It is not a GitHub secret on purpose: a fresh environment needs no manual step.

Repository-level configuration the workflows expect (already present):

- environments `test` and `production`;
- variables `IMAGE_NAMESPACE`, `PREVIEW_BASE_DOMAIN`, `AI_MODEL`, `AI_BASE_URL`, and the
  `TEST_*` / `PRODUCTION_*` host, port and user pairs;
- secrets `TEST_SSH_*`, `PRODUCTION_SSH_*`, `AI_API_KEY`.

## Local checks

```bash
bash -n scripts/cd/*.sh
bash scripts/ci/validate-helm-chart.sh
```

Both images need `.artifacts/` (gitignored) prepared the same way CI does it:

```bash
dotnet publish server/src/CodeCafe.Host/CodeCafe.Host.csproj -c Release \
  --output .artifacts/api --no-build -p:UseAppHost=false
(cd clients/web && pnpm build) && mkdir -p .artifacts/frontend && cp -R clients/web/dist/. .artifacts/frontend/
docker build -f server/src/CodeCafe.Host/Dockerfile -t codecafe-api .
docker build -f clients/web/Dockerfile -t codecafe-frontend .
```
