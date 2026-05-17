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
│  └─ CodeCafe.Application.Tests/
└─ frontend/                    # React app, developed in the same repository
```

## Getting Started

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/CodeCafe.WebApi
```

The API includes liveness and readiness endpoints:

```http
GET /health/live
GET /health/ready
```

## Architecture

The backend follows Clean Architecture dependency direction:

```text
Domain <- Application <- Infrastructure
                      <- WebApi
```

`Domain` has no dependencies on other application projects. `Application` depends only on `Domain`.
`Infrastructure` implements application abstractions. `WebApi` composes the app through dependency injection.

Backend development guidelines are documented in [docs/backend-best-practices.md](docs/backend-best-practices.md).

## Frontend

The `frontend/` directory contains the React app. Frontend implementation is handled by another AI collaborator in this same repository. Backend work should preserve API contracts and document breaking changes.

## Deployment

CI/CD workflow notes and required GitHub variables are documented in [docs/deployment.md](docs/deployment.md).

## License

This project is licensed under the MIT License.
