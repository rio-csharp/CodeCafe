# CodeCafe

CodeCafe is an open-source ASP.NET Core Web API and React project scaffold.

## Structure

```text
CodeCafe/
├─ src/
│  ├─ CodeCafe.Domain/          # Enterprise entities and domain rules
│  ├─ CodeCafe.Application/     # Use cases, abstractions, DTOs, validation
│  ├─ CodeCafe.Infrastructure/  # Persistence, external services, implementations
│  └─ CodeCafe.WebApi/          # ASP.NET Core API host and HTTP endpoints
├─ tests/
│  ├─ CodeCafe.Application.Tests/
│  └─ CodeCafe.WebApi.Tests/
└─ frontend/                    # React app, developed in the same repository
```

## Getting Started

### Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 10.0.x | Required to build and run the backend |
| Node.js | 24+ | Managed via nvm (nvm-windows on Windows) |
| PostgreSQL | 16+ | Required for identity, data protection, and notes persistence |

### 1. Install .NET 10 SDK

**Windows:**
```powershell
# Using the Microsoft install script
Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1
powershell -ExecutionPolicy Bypass -File dotnet-install.ps1 -Channel 10.0 -InstallDir "$env:USERPROFILE\.dotnet"
# Add to your PATH: %USERPROFILE%\.dotnet
```

**macOS/Linux:**
```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0 --install-dir ~/.dotnet
# Add to your PATH: export PATH="$HOME/.dotnet:$PATH"
```

Verify:
```bash
dotnet --version  # Should print 10.0.xxx
```

### 2. Install Node.js via nvm

**Windows (nvm-windows):**
```powershell
nvm install 24
nvm use 24
```

**macOS/Linux:**
```bash
nvm install 24
nvm use 24
```

Verify:
```bash
node --version  # Should print v24.x.x
npm --version
```

### 3. Install PostgreSQL 16+

**Windows (binary zip):**
1. Download the PostgreSQL 16 binaries zip from https://www.postgresql.org/download/windows/
2. Extract to a folder such as `C:\tools\postgresql\pgsql`
3. Initialize the database cluster:
   ```powershell
   & "C:\tools\postgresql\pgsql\bin\initdb.exe" -D "C:\tools\postgresql\data" --username=postgres --encoding=UTF8
   ```
4. Start the server:
   ```powershell
   & "C:\tools\postgresql\pgsql\bin\pg_ctl" -D "C:\tools\postgresql\data" -l "C:\tools\postgresql\logfile" start
   ```
5. Add `C:\tools\postgresql\pgsql\bin` to your PATH.

**macOS (Homebrew):**
```bash
brew install postgresql@16
brew services start postgresql@16
```

**Linux (apt):**
```bash
sudo apt update
sudo apt install postgresql-16
sudo systemctl start postgresql
```

### 4. Create the Database and User

Connect as the `postgres` superuser and run:

```sql
CREATE USER codecafe WITH PASSWORD 'codecafe' CREATEDB;
CREATE DATABASE codecafe OWNER codecafe;
GRANT ALL PRIVILEGES ON DATABASE codecafe TO codecafe;
```

### 5. Configure the Backend

Create `src/CodeCafe.WebApi/appsettings.Development.json` (this file is gitignored):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=codecafe;Username=codecafe;Password=codecafe"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "Auth": {
    "RegistrationEnabled": true
  },
  "Cors": {
    "AllowedOrigins": []
  },
  "AllowedHosts": "*"
}
```

> In Development mode, CORS automatically allows the Vite dev server (`http://localhost:5173`).

### 6. Build and Run the Backend

```powershell
dotnet restore CodeCafe.slnx
dotnet build CodeCafe.slnx --configuration Release
dotnet test CodeCafe.slnx --configuration Release
```

Run the API (migrations apply automatically in Development mode):

```powershell
dotnet run --project src/CodeCafe.WebApi
```

The API will start on:
- HTTP: `http://localhost:5042`
- HTTPS: `https://localhost:7239`

Health endpoints:

```http
GET http://localhost:5042/health/live
GET http://localhost:5042/health/ready
```

### 7. Configure and Run the Frontend

```powershell
cd frontend
copy .env.example .env   # On Windows
# cp .env.example .env   # On macOS/Linux
npm ci
npm run dev
```

The Vite dev server starts on `http://localhost:5173` and proxies `/api` requests to the backend at `http://localhost:5042`.

> **Note:** The `.env.example` sets `VITE_API_BASE_URL=https://localhost:7239`. For local development with the Vite proxy, you can leave this value empty so API calls use relative URLs:
> ```
> VITE_API_BASE_URL=
> ```

### 8. Verify End-to-End

1. Open `http://localhost:5173` in your browser.
2. Register a new account via the UI (or `POST /api/auth/register`).
3. Log in via the UI (or `POST /api/auth/login`).
4. Confirm you can access protected routes.

## Architecture

The backend follows Clean Architecture dependency direction:

```text
Domain <- Application <- Infrastructure
                      <- WebApi
```

`Domain` has no dependencies on other application projects. `Application` depends only on `Domain`.
`Infrastructure` implements application abstractions. `WebApi` composes the app through dependency injection.

Backend development guidelines are documented in [docs/backend-best-practices.md](docs/backend-best-practices.md).
Frontend development guidelines are documented in [frontend/BEST_PRACTICES.md](frontend/BEST_PRACTICES.md).
The Notes MCP design and full implementation plan are documented in [docs/notes-mcp-design.md](docs/notes-mcp-design.md) and [docs/mcp-server-setup-plan.md](docs/mcp-server-setup-plan.md).
Local Claude Code connection steps for the current MCP implementation are documented in [docs/mcp-connection.md](docs/mcp-connection.md).

## Frontend

The `frontend/` directory contains the React app. Frontend implementation is handled by another AI collaborator in this same repository. Backend work should preserve API contracts and document breaking changes.

## Deployment

CI/CD workflow notes and required GitHub variables are documented in [docs/deployment.md](docs/deployment.md).

## License

This project is licensed under the MIT License.
