# Deployment

CI builds and tests the backend and frontend in parallel. For deployable refs, CI
also publishes the API and frontend images to GHCR in parallel, then writes a
small deployment metadata artifact containing the image tag.

Deployment workflows do not build application code or Docker images. They
download the Helm chart and deployment metadata produced by CI, then run
`helm upgrade`.

The workflows deploy the images produced by CI:

- PRs targeting `release/*` deploy to `https://pr-<number>.<PREVIEW_BASE_DOMAIN>`.
- Pushes to `release/*` deploy to test.
- Pushes to `main` deploy to production.
- Production rollback is manual through the `Rollback Production` workflow.

`appsettings.json` is committed with template/default values only and acts as the
configuration reference. Local secrets belong in
`src/CodeCafe.WebApi/appsettings.Development.json`, which is ignored by Git and
excluded from Docker build context and publish output. Deployed values should be
provided through environment variables, GitHub secrets, Kubernetes secrets, or
Helm values.

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
- `PRODUCTION_SSH_HOST`
- `PRODUCTION_SSH_PRIVATE_KEY`

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
