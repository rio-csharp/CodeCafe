# Deployment

CI builds and tests the backend and frontend in parallel. For deployable refs, CI
also publishes the API and frontend images to GHCR in parallel, then writes a
small deployment metadata artifact containing the image tag.

Deployment workflows do not build application code or Docker images. They
download the Helm chart and deployment metadata produced by CI, then run
`helm upgrade`.

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
