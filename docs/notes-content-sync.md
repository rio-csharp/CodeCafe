# Notes Content Sync

CodeCafe application code and note content should live in separate repositories:

- `CodeCafe`: application code, Kubernetes manifests, and deployment workflows
- `Notes`: private Markdown content repository

The API reads notes from the filesystem. In Kubernetes, the API pod mounts a
stable host directory and reads notes from `/data/notes`.

## Target Layout

Use the same host path on test and production servers:

```text
/home/deploy/codecafe/notes
```

The Helm chart mounts that directory into the API container as:

```text
/data/notes
```

And sets:

```text
Notes__RootPath=/data/notes
```

Because the application reads directly from the mounted directory, note updates
become visible without rebuilding the CodeCafe images or restarting the pods.

## Why a Separate Notes Repository

This keeps content and application deployment independent:

- application deploys do not need to rebuild when notes change
- note history stays clean and private
- test and production can sync the exact committed note revision
- later writable workspaces can remain separate from the read-only source notes

## Sync Model

The `Notes` repository should deploy content to the servers with GitHub Actions.
The workflow:

1. Checks out the `Notes` repository
2. Packages the repository content
3. Uploads the content to the target server over SSH
4. Mirrors the uploaded content into `/srv/codecafe/notes`

This avoids putting Git credentials on the server. The server does not need to
clone the private repository directly.

## Local Development

For local development, keep the notes path in your local-only configuration.
For example:

```json
{
  "Notes": {
    "RootPath": "D:/Notes"
  }
}
```

Store that in a local-only file such as:

```text
backend/src/CodeCafe.Api/appsettings.Development.local.json
```

That file is ignored by Git.

## Notes Repository Secrets

The `Notes` repository should have its own GitHub secrets for the target
servers, even if they match the CodeCafe repository values.

Recommended secrets:

```text
TEST_SSH_HOST
TEST_SSH_PORT
TEST_SSH_USER
TEST_SSH_PRIVATE_KEY
PRODUCTION_SSH_HOST
PRODUCTION_SSH_PORT
PRODUCTION_SSH_USER
PRODUCTION_SSH_PRIVATE_KEY
```

Recommended variables:

```text
NOTES_CONTENT_PATH=/home/deploy/codecafe/notes
NOTES_SYNC_PRODUCTION_ENABLED=false
```

`NOTES_SYNC_PRODUCTION_ENABLED` lets the production sync workflow stay present
without forcing automatic production sync before you are ready.
