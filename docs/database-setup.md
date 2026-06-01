# CodeCafe Database Setup

This document describes the current database setup for the rebuilt backend.

## Local Development

For local development you need PostgreSQL 16+ and a database named `codecafe`.

Quick setup:

1. Start PostgreSQL.
2. Create the role and database:
   ```sql
   CREATE USER codecafe WITH PASSWORD 'codecafe' CREATEDB;
   CREATE DATABASE codecafe OWNER codecafe;
   ```
3. Configure the connection string in `src/CodeCafe.Server/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=codecafe;Username=codecafe;Password=codecafe"
     }
   }
   ```
4. Run the backend host:
   ```bash
   dotnet run --project src/CodeCafe.Server
   ```

Development startup applies migrations automatically.

## Migration Command

The backend host supports:

```bash
dotnet CodeCafe.Server.dll migrate
```

This is the command used by deployment and repair flows.

## EF Core Tooling

Use `CodeCafe.Infrastructure` as the migrations project and `CodeCafe.Server` as the startup project:

```bash
dotnet ef database update \
  --project src/CodeCafe.Infrastructure/CodeCafe.Infrastructure.csproj \
  --startup-project src/CodeCafe.Server/CodeCafe.Server.csproj \
  --context ApplicationDbContext
```

## Database Sync Tool

The supported console tool is:

```bash
dotnet run --project tools/CodeCafe.DbSync -- migrate-test
dotnet run --project tools/CodeCafe.DbSync -- migrate-prod
```

It opens an SSH tunnel, reads the Kubernetes database secret, and runs EF Core migrations through `CodeCafe.Server`.

## Secrets and Environment Values

Do not commit:

- connection strings
- passwords
- hostnames or internal IPs
- decoded Kubernetes secrets
- per-machine development settings

Keep environment-specific values in GitHub secrets, Kubernetes secrets, or server-local files.
