# Deployment

CI builds and tests the backend and frontend in parallel. For deployable refs, CI
also publishes the API and frontend images to GHCR in parallel, then writes a
small deployment metadata artifact containing the image tag.

Deployment workflows do not build application code or Docker images. They
download the Helm chart and deployment metadata produced by CI, then run
the deployment helper script from the CI scripts artifact. The helper performs
the remote `helm upgrade`, migration hook execution, rollout wait, and smoke
test.

CI runs on pushes to `main`, `release/*`, and `feature/*`, and on pull requests
targeting `release/*`.

The workflows deploy the images produced by CI:

- PRs targeting `release/*` deploy to `https://pr-<number>.<PREVIEW_BASE_DOMAIN>`.
- Pushes to `release/*` deploy to test.
- Pushes to `main` deploy to production.
- Production rollback is manual through the `Rollback Production` workflow.

Deploy workflows intentionally do not accept an arbitrary Git ref. Manual
dispatches deploy artifacts from a specific successful CI run id, which keeps
the deployment input tied to an already-built image and Helm chart.

`appsettings.json` is committed with template/default values only and acts as the
configuration reference. Local secrets belong in
`src/CodeCafe.WebApi/appsettings.Development.json`, which is ignored by Git and
excluded from Docker build context and publish output. Deployed values should be
provided through environment variables, GitHub secrets, Kubernetes secrets, or
Helm values.

The API expects the PostgreSQL connection string in:

```text
ConnectionStrings__DefaultConnection
```

Deploy workflows create a Kubernetes secret named `<release>-api-config` in the
target namespace and mount it into the API pod through `envFrom`.

The long-lived manually created database secrets are named `codecafe-db-secret`
in `codecafe-test` and `codecafe-prod`. Manual Helm runs may reference those
secrets directly through `api.envFromSecrets`. CI/CD copies only the
`ConnectionStrings__DefaultConnection` key from that server-side Kubernetes
Secret into the release-scoped Secret. GitHub does not need database connection
string secrets for deploys.

For OAuth/OpenIddict, the deploy workflows read GitHub Environment Secrets for
the `test` and `production` environments and recreate a Kubernetes Secret named
`codecafe-oauth-secret` in the target namespace on every deploy.

Configure these environment secrets:

```text
OAUTH_CERT_BASE64
OAUTH_CERT_PASSWORD
```

`OAUTH_CERT_BASE64` should contain a single-line base64-encoded PFX. Test and
production should use different environment-secret values.

## Database Migrations

The Helm chart creates a pre-install/pre-upgrade Job that runs
`dotnet CodeCafe.WebApi.dll migrate` using the API image before the Deployment
is rolled out. Migration execution is protected by a PostgreSQL advisory lock.

PR previews currently share the test database, so PR preview deployments disable
the migration hook with `--set api.migration.enabled=false`. This prevents one
PR from changing the shared test schema for all other PRs. Schema-changing PRs
should be validated after merge/deploy to test, or moved to isolated per-PR
databases before enabling PR migrations.

If production data is synced to test, the sync workflow restores production into
the test database and then runs the current test API image's migration command so
the test schema is brought back up to the deployed test application version.

For manual repair or one-off operations, the database console tool can still
apply migrations through an SSH tunnel:

```sh
dotnet run --project tools/CodeCafe.DbSync -- migrate-test
dotnet run --project tools/CodeCafe.DbSync -- migrate-prod
```

Both commands read the Kubernetes database secret over SSH, open a local tunnel
to PostgreSQL, and run `dotnet ef database update` against that tunnel.

## CORS

The backend reads allowed frontend origins from:

```json
{
  "Cors": {
    "AllowedOrigins": []
  }
}
```

For local development, if no CORS origins are configured and the API runs in the
`Development` environment, the API allows the Vite dev server origins:

- `http://localhost:5173`
- `https://localhost:5173`
- `http://127.0.0.1:5173`
- `https://127.0.0.1:5173`

Deployed environments are configured by Helm. CORS values must be the frontend
page origin, not the API origin:

- PR preview frontend: `https://pr-<number>.<PREVIEW_BASE_DOMAIN>`
- PR preview API: `https://api-pr-<number>.<PREVIEW_BASE_DOMAIN>`
- Test: `https://<TEST_FRONTEND_HOST>`
- Production: `https://<PRODUCTION_FRONTEND_HOST>`

These values are injected into the API container as
`Cors__AllowedOrigins__0`. Add more indexed values only when the API must accept
multiple frontend origins in the same environment.

If the frontend and API are served from exactly the same origin, for example the
same scheme, host, and port, CORS is not involved for browser requests. Local
development usually still needs CORS because Vite and the API run on different
ports, such as `http://localhost:5173` calling `https://localhost:7239`.

## Required GitHub Variables

Repository variables:

- `IMAGE_NAMESPACE`
- `PREVIEW_BASE_DOMAIN`
- `TEST_SSH_PORT`
- `TEST_SSH_USER`
- `TEST_FRONTEND_HOST`
- `TEST_API_HOST`
- `PRODUCTION_SSH_PORT`
- `PRODUCTION_SSH_USER`
- `PRODUCTION_FRONTEND_HOST`
- `PRODUCTION_API_HOST`

Repository secrets:

- `TEST_SSH_HOST`
- `TEST_SSH_PRIVATE_KEY`
- `TEST_SSH_KNOWN_HOSTS`
- `PRODUCTION_SSH_HOST`
- `PRODUCTION_SSH_PRIVATE_KEY`
- `PRODUCTION_SSH_KNOWN_HOSTS`

`TEST_SSH_KNOWN_HOSTS` and `PRODUCTION_SSH_KNOWN_HOSTS` must contain the
expected OpenSSH known_hosts line for the deployment host. The workflows use
`StrictHostKeyChecking=yes`; they should fail closed if the host key is missing
or changes unexpectedly.

## Cluster Assumptions

The test cluster is expected to have a wildcard TLS secret named
`codecafe-test-wildcard-tls` in the `codecafe-shared` namespace.

The production cluster is expected to have a wildcard TLS secret named
`codecafe-production-wildcard-tls` in the `codecafe-shared` namespace.

The deployment user must be able to run `kubectl` and `helm`. Production follows
the previous project pattern and runs them through:

```sh
sudo KUBECONFIG=/etc/rancher/k3s/k3s.yaml
```

If GHCR packages are private, configure image pull credentials in the cluster or
set `imagePullSecrets` in the Helm values.

Ingress defaults target Traefik's `websecure` entrypoint. If a cluster uses a
different HTTPS enforcement model, override `frontend.ingress.annotations` and
`api.ingress.annotations` in Helm values instead of disabling TLS at the
application layer.

Cloudflare only changes the public edge for HTTP/TLS. It does not remove the
need for OpenIddict signing and encryption material in production. If the API
is the OAuth authorization server, keep its token-signing keys in Kubernetes
secrets or another secret store. Do not rely on a shared symmetric `SigningKey`
for production MCP auth.

OAuth client registrations are application configuration, not deploy workflow
inputs. The deploy workflows only set the issuer and frontend base URL. If you
need to support more MCP clients than Claude Code, add them under
`AuthorizationServer:PublicClients` in environment-specific app configuration
or secret-backed environment variables.
