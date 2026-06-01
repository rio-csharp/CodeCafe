# Deployment

The backend deployment target is now `CodeCafe.Server`.

`CodeCafe.Server` composes:

- `CodeCafe.Api`
- `CodeCafe.Mcp`
- OpenIddict/OAuth endpoints

## CI Publish Target

CI publishes the backend from:

```text
src/CodeCafe.Server/CodeCafe.Server.csproj
```

The backend container entrypoint is:

```bash
dotnet CodeCafe.Server.dll
```

## Local Secrets

Committed `appsettings.json` files contain defaults only.

Local backend secrets belong in:

```text
src/CodeCafe.Server/appsettings.Development.json
```

## Database Migrations

The deployment migration command is:

```bash
dotnet CodeCafe.Server.dll migrate
```

The Helm migration job and the production-to-test sync repair flow should both use this command.

## Backend Image Role

The deployed backend image now represents the combined server host, even if deployment metadata still uses the historical `api` naming convention.

That backend image is responsible for:

- REST API routes
- MCP routes
- OAuth/OpenIddict endpoints
- migration startup command
