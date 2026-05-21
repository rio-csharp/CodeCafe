# CodeCafe Database Setup

This document describes how the database integration is configured without
recording environment-specific host names, IP addresses, passwords, or private
network details. Keep concrete infrastructure values in GitHub Variables,
GitHub Secrets, Kubernetes Secrets, or server-local files such as `.pgpass`.

## Architecture

- Production and test use separate PostgreSQL databases.
- PR previews currently share the test database.
- CI/CD injects database connection strings into Kubernetes Secrets.
- Test and production deployments run EF Core migrations through a Helm
  pre-install/pre-upgrade Job before rolling the API Deployment.
- PR preview deployments disable the migration Job because previews share the
  test database.
- Production-to-test data sync is one-way and overwrites the test database after
  taking a test backup.

## Local Development

For local development you need a running PostgreSQL 16+ instance and a database named `codecafe`.

Quick setup:

1. **Start PostgreSQL** (see [README.md](../README.md) for installation options).
2. **Create the application role and database:**
   ```sql
   CREATE USER codecafe WITH PASSWORD 'codecafe' CREATEDB;
   CREATE DATABASE codecafe OWNER codecafe;
   ```
3. **Configure the connection string** in `src/CodeCafe.WebApi/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=codecafe;Username=codecafe;Password=codecafe"
     }
   }
   ```
4. **Run the API.** Migrations apply automatically in Development mode on startup.

> Do not commit `appsettings.Development.json`. It is already gitignored.

## Required Configuration

Repository variables:

- `TEST_SSH_HOST`
- `TEST_SSH_PORT`
- `TEST_SSH_USER`
- `PRODUCTION_SSH_HOST`
- `PRODUCTION_SSH_PORT`
- `PRODUCTION_SSH_USER`
- `TEST_FRONTEND_HOST`
- `TEST_API_HOST`
- `PRODUCTION_FRONTEND_HOST`
- `PRODUCTION_API_HOST`
- `PREVIEW_BASE_DOMAIN`
- `IMAGE_NAMESPACE`

Repository secrets:

- `TEST_SSH_PRIVATE_KEY`
- `TEST_SSH_KNOWN_HOSTS`
- `PRODUCTION_SSH_PRIVATE_KEY`
- `PRODUCTION_SSH_KNOWN_HOSTS`

Do not commit concrete host addresses, `.pgpass` contents, private keys, or
decoded Kubernetes Secret values.

## Kubernetes Secrets

CI/CD creates a release-scoped API config Secret named:

```text
<release>-api-config
```

It contains:

```text
ConnectionStrings__DefaultConnection=<PostgreSQL connection string>
```

Manual cluster setup must create a long-lived Secret named `codecafe-db-secret`
in the test and production namespaces. Manual Helm runs may reference it through
`api.envFromSecrets`, but CI/CD overrides `api.envFromSecrets[0]` with the
release-scoped Secret copied from the server-side `codecafe-db-secret`.

GitHub Actions do not need database connection string secrets. The database
connection string stays in Kubernetes.

Example manual shape:

```bash
kubectl create secret generic codecafe-db-secret \
  --from-literal=ConnectionStrings__DefaultConnection='<connection-string>' \
  --namespace '<namespace>'
```

Manual cluster setup must also create a long-lived Secret named
`codecafe-oauth-secret` in the test and production namespaces. Deployments copy
these keys into the same release-scoped API config Secret:

```text
AuthorizationServer__SigningCertificateBase64=<base64-pfx>
AuthorizationServer__SigningCertificatePassword=<optional-password>
AuthorizationServer__EncryptionCertificateBase64=<base64-pfx>
AuthorizationServer__EncryptionCertificatePassword=<optional-password>
```

Example manual shape:

```bash
kubectl create secret generic codecafe-oauth-secret \
  --from-literal=AuthorizationServer__SigningCertificateBase64='<base64-pfx>' \
  --from-literal=AuthorizationServer__SigningCertificatePassword='<password>' \
  --from-literal=AuthorizationServer__EncryptionCertificateBase64='<base64-pfx>' \
  --from-literal=AuthorizationServer__EncryptionCertificatePassword='<password>' \
  --namespace '<namespace>'
```

The test and production namespaces must each have `codecafe-oauth-secret`
before deploys can pass.

## PostgreSQL Requirements

Each environment should provide:

- PostgreSQL 16 or newer.
- A database named `codecafe`.
- A non-superuser application role named `codecafe`.
- The `codecafe` role as owner of the `codecafe` database.
- Server-local `.pgpass` for maintenance scripts, with file mode `600`.
- Network access from the K3s node/pod network to PostgreSQL.
- No public PostgreSQL access unless explicitly protected by firewall and
  `pg_hba.conf`.

The application role should not be a PostgreSQL superuser. Use a separate
administrator role for server maintenance if needed.

## Helm Deployment

The chart supports database Secret injection through:

```yaml
api:
  envFromSecrets:
    - <secret-name>
```

The API migration Job is enabled by default:

```yaml
api:
  migration:
    enabled: true
    backoffLimit: 1
    ttlSecondsAfterFinished: 300
```

PR preview workflow disables the migration Job:

```bash
--set api.migration.enabled=false
```

This is intentional while PR preview and test share the same database.

## Migration Flow

The API image supports a migration command:

```bash
dotnet CodeCafe.WebApi.dll migrate
```

Helm runs this command as a pre-install/pre-upgrade Job for test and production
deployments. The Job receives the same release-scoped Secret as the API pod, so
it also uses the server-side database connection string copied from
`codecafe-db-secret`. The migration runner uses a PostgreSQL advisory lock so
concurrent deployments do not apply migrations at the same time.

Current applied migrations should include:

```text
20260517112535_InitialIdentity
20260517114056_AddDataProtectionKeys
20260518101500_MakeUserEmailUnique
```

For manual repair, use the local console tool after exporting the host
environment variables:

```bash
dotnet run --project tools/CodeCafe.DbSync -- migrate-test
dotnet run --project tools/CodeCafe.DbSync -- migrate-prod
```

The tool reads the Kubernetes database Secret over SSH, opens a local tunnel,
and runs `dotnet ef database update` against that tunnel.

## Database Sync Tool

The maintained database utility lives at:

```text
tools/CodeCafe.DbSync
```

Commands:

```bash
dotnet run --project tools/CodeCafe.DbSync -- check
dotnet run --project tools/CodeCafe.DbSync -- migrate-test
dotnet run --project tools/CodeCafe.DbSync -- migrate-prod
dotnet run --project tools/CodeCafe.DbSync -- prod-to-local
dotnet run --project tools/CodeCafe.DbSync -- local-to-test
```

Required local environment variables for remote commands:

```text
PROD_HOST
TEST_HOST
```

Optional overrides:

```text
PROD_SSH_PORT
PROD_SSH_USER
PROD_DB_PORT
PROD_DB_USER
PROD_DB
TEST_SSH_PORT
TEST_SSH_USER
TEST_DB_PORT
TEST_DB_USER
TEST_DB
LOCAL_HOST
LOCAL_DB_PORT
LOCAL_DB_USER
LOCAL_DB
SSH_KEY_PATHS
```

Password inputs:

- `PROD_DB_PASSWORD` for production-to-local sync.
- `LOCAL_DB_PASSWORD` for local restores/dumps.
- Server-side `.pgpass` for test restore operations.

## Production To Test Sync

The workflow `.github/workflows/sync-prod-to-test-db.yml` runs on a schedule and
can also be triggered manually with an explicit confirmation string.

Flow:

1. Stream a custom-format production `pg_dump` through SSH to the test server.
2. Back up the existing test database on the test server.
3. Drop and recreate the test database.
4. Restore the production dump into test.
5. Run the current test API image migration command to bring the schema back to
   the deployed test application version.

The workflow should not print dump contents, connection strings, passwords, or
decoded Secret values.

## Security Checklist

- Do not commit public IP addresses, internal IP addresses, hostnames, passwords,
  private keys, `.pgpass` contents, or decoded Kubernetes Secrets.
- Keep database connection strings in GitHub Secrets or Kubernetes Secrets.
- Keep SSH host keys in `*_SSH_KNOWN_HOSTS` and use `StrictHostKeyChecking=yes`.
- Keep the application database role non-superuser.
- Disable PR preview migrations while PR and test share a database.
- Take a test database backup before any production-to-test restore.
- Avoid writing production dumps to the GitHub runner filesystem.
