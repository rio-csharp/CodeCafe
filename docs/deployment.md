# Deployment

CodeCafe uses Kubernetes deployments for the permanent test environment and PR
preview environments. Production uses a separate Kubernetes cluster/server.

For the full test server runbook, see
[`docs/test-environment-setup.md`](test-environment-setup.md).

For note content sync from the separate `Notes` repository, see
[`docs/notes-content-sync.md`](notes-content-sync.md).

## Test Server Prerequisites

- k3s installed and healthy
- Traefik enabled, which is the k3s default ingress controller
- Ports `80/tcp` and `443/tcp` open on the server firewall
- Cloudflare DNS records pointing at the test server
- A shared Kubernetes TLS secret named `codecafe-test-wildcard-tls`

## Cloudflare DNS

Point these records to the test server public IP:

```text
<TEST_FRONTEND_HOST>      A      <test-server-ip>    Proxied
<TEST_API_HOST>           A      <test-server-ip>    Proxied
*.<PREVIEW_BASE_DOMAIN>   A      <test-server-ip>    Proxied
```

PR previews use:

```text
pr-<number>.<PREVIEW_BASE_DOMAIN>
api-pr-<number>.<PREVIEW_BASE_DOMAIN>
```

## GitHub Variables and Secrets

Create these repository variables for the test cluster:

```text
TEST_FRONTEND_HOST
TEST_API_HOST
PREVIEW_BASE_DOMAIN
TEST_SSH_HOST
TEST_SSH_PORT
TEST_SSH_USER
TEST_APP_USERNAME
```

Create these repository variables for the production cluster:

```text
PRODUCTION_FRONTEND_HOST
PRODUCTION_API_HOST
PRODUCTION_SSH_HOST
PRODUCTION_SSH_PORT
PRODUCTION_SSH_USER
PRODUCTION_APP_USERNAME
```

Create these repository secrets:

```text
TEST_SSH_PRIVATE_KEY
TEST_APP_PASSWORD
PRODUCTION_SSH_PRIVATE_KEY
PRODUCTION_APP_PASSWORD
```

Create this additional repository variable:

```text
IMAGE_NAMESPACE
```

Keep local copies in `deploy/secrets.env`. That file is ignored by Git. Use
`deploy/secrets.example.env` as the committed format reference.

The test and production workflows use SSH to connect to the target server and
run `kubectl` and `helm` there. Do not expose the Kubernetes API publicly for
GitHub Actions.

The deploy workflows also create or update a Kubernetes secret named
`<release>-api-auth` in the target namespace. The API reads its login
credentials from that secret via `Authentication__Username` and
`Authentication__Password` environment variables.

## Notes Content

The deployed API reads notes from a mounted server directory instead of from the
application image.

Default paths:

```text
server host path:   /home/deploy/codecafe/notes
container path:     /data/notes
```

The separate `Notes` repository is responsible for syncing Markdown files into
that server path. This keeps note updates independent from CodeCafe application
deploys.

## TLS

Cloudflare Universal SSL protects visitor traffic to Cloudflare. The Kubernetes
ingress still needs a certificate for Cloudflare-to-origin traffic when the zone
uses `Full (strict)`.

For the first version, use a Cloudflare Origin Certificate and create one shared
wildcard TLS secret. The GitHub workflows copy this secret into each deployment
namespace. Later, replace this with cert-manager.

Create a Cloudflare Origin Certificate that covers:

```text
<TEST_FRONTEND_HOST>
<TEST_API_HOST>
*.<PREVIEW_BASE_DOMAIN>
```

Then create the shared secret:

```bash
kubectl create namespace codecafe-shared
kubectl create secret tls codecafe-test-wildcard-tls \
  --cert=cloudflare-origin.pem \
  --key=cloudflare-origin.key \
  --namespace codecafe-shared
```

The workflows copy this secret into `codecafe-test` and each `codecafe-pr-*`
namespace before deploying.

To replace an existing certificate, use:

```bash
kubectl create secret tls codecafe-test-wildcard-tls \
  --cert=cloudflare-origin.pem \
  --key=cloudflare-origin.key \
  --namespace codecafe-shared \
  --dry-run=client -o yaml | kubectl apply -f -
```

For production, create a separate shared TLS secret in the production cluster:

```bash
kubectl create namespace codecafe-shared
kubectl create secret tls codecafe-production-wildcard-tls \
  --cert=origin.pem \
  --key=origin.key \
  --namespace codecafe-shared
```

Use a production origin certificate that covers the production frontend and API
hosts.

## Workflows

- `CI`: runs backend and frontend checks on every push, so each commit gets a
  GitHub check status.
- `CI`: detects whether the pushed branch has an open PR. If it does, CI
  uploads API and frontend build artifacts after backend and frontend checks
  pass. The PR image jobs download those artifacts and package them into thin
  runtime images in parallel.
- `CI`: triggers the separate `Deploy PR Preview` workflow after PR images are
  published.
- `Deploy PR Preview`: deploys the existing PR images published by CI to
  `pr-<number>.<PREVIEW_BASE_DOMAIN>`.
- `Deploy PR Preview`: writes a PR comment with preview URL formats only. It
  does not expose the real preview base domain.
- `Cleanup PR Preview`: deletes the PR namespace when the PR closes.
- `Deploy Test`: deploys `main` to `<TEST_FRONTEND_HOST>`.
- `Deploy Production`: deploys `main` to `<PRODUCTION_FRONTEND_HOST>`.
- `Rollback Production`: manually rolls the production Helm release back to the
  previous revision, or to a specific Helm revision if one is provided.

`Deploy Test` and `Deploy Production` both support manual runs. Use the
`git_ref` input to deploy a branch, tag, or commit SHA.

Production deploys run automatically when changes merge to `main`. If a
production deploy is bad, use `Rollback Production` for the fastest rollback
because it reuses the previous Helm release revision and does not rebuild
images. If you need to redeploy a specific commit instead, manually run
`Deploy Production` and set `git_ref` to the target branch, tag, or commit SHA.
