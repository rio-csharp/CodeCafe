# CodeCafe Backend Best Practices

This guide reflects the current backend standard for CodeCafe.

`CodeCafe.WebApi` has been retired. New backend work targets `CodeCafe.Api`,
`CodeCafe.Mcp`, and `CodeCafe.Server` only.

## Architecture

The backend is now organized around three adapters and a shared core:

```text
CodeCafe.Server
├─ CodeCafe.Api
├─ CodeCafe.Mcp
├─ CodeCafe.Application
├─ CodeCafe.Domain
└─ CodeCafe.Infrastructure
```

Dependency direction:

```text
Domain <- Application <- Infrastructure
                      <- Api
                      <- Mcp
Server -> Api + Mcp + Application + Infrastructure
```

Rules:

- `CodeCafe.Domain` references nothing outside itself.
- `CodeCafe.Application` references only `CodeCafe.Domain`.
- `CodeCafe.Infrastructure` implements application abstractions.
- `CodeCafe.Api` and `CodeCafe.Mcp` are thin adapters.
- `CodeCafe.Server` is the composed deployment host.

## Project Responsibilities

### `CodeCafe.Domain`

Owns:

- entities
- value objects
- invariants
- domain behavior

Must not own:

- EF Core
- HTTP or MCP types
- configuration

### `CodeCafe.Application`

Owns:

- commands and queries
- MediatR handlers
- validators
- application interfaces
- pipeline behaviors

Must not own:

- ASP.NET Core
- MCP SDK concerns
- persistence implementations

### `CodeCafe.Infrastructure`

Owns:

- EF Core persistence
- identity integrations
- external service implementations
- repository implementations

Must not own:

- primary use-case orchestration
- transport-specific models

### `CodeCafe.Api`

Owns:

- HTTP endpoint registration
- auth cookie flow
- antiforgery
- HTTP error mapping
- rate limiting and health endpoints

Must not own:

- business rules
- direct persistence logic

### `CodeCafe.Mcp`

Owns:

- MCP transport wiring
- tools, resources, and prompts
- MCP result mapping
- MCP-specific upload/import support

Must not own:

- notebook business rules outside shared application behavior
- direct transport bypasses around authorization and validation

### `CodeCafe.Server`

Owns:

- single-host composition of API and MCP
- OpenIddict/OAuth endpoints
- protected resource metadata
- migration startup command

This is the default backend deployment target.

## Auth / OIDC Stays Controller-Based

The OpenIddict authorization-server endpoints (`/connect/authorize`,
`/connect/token`, dynamic client registration) live in MVC controllers under
`CodeCafe.Server/Auth` and intentionally stay there. They are not reshaped into
MediatR slices like the Notes feature.

Reason: these endpoints are inseparable from the OpenIddict ASP.NET Core
integration. They resolve the request with `HttpContext.GetOpenIddictServerRequest()`,
drive authentication through specific schemes, and return results as
`SignIn` / `Forbid` with `AuthenticationProperties` so the OpenIddict server
middleware can complete the protocol. That logic is transport, not a portable
application use case. Pushing it behind a handler would force the handler to take
an `HttpContext` dependency, which inverts the dependency direction the rebuild
is protecting.

Guidance:

- Keep the OAuth/OIDC protocol flow in `CodeCafe.Server/Auth` controllers.
- Application-style auth concerns that are genuinely transport-agnostic
  (for example user registration/login response shaping behind
  `IAuthEndpointService`) may still live in the application/adapter layers.
- Revisit only if a future change makes the flow transport-agnostic.

## Vertical Slice Rules

New backend work should follow feature slices in `CodeCafe.Application`.

Example:

```text
CodeCafe.Application/
  Notes/
    Commands/
    Queries/
    DTOs/
```

Adapter code should map into those slices:

- `CodeCafe.Api/Endpoints/Notes`
- `CodeCafe.Mcp/Tools/Notes`

Do not add new backend behavior to large shared helpers or controller-style classes.

## MediatR and CQRS

All externally triggered business use cases should run through MediatR.

Rules:

- commands change state
- queries do not change state
- validators run before handlers
- transactions wrap commands, not queries

Do not add a generic repository or ceremonial `UnitOfWork` layer on top of EF Core.

## HTTP Adapter Rules

HTTP endpoints should:

1. bind request
2. call MediatR
3. map result to HTTP

They should not:

- query `DbContext` directly
- implement business branching
- duplicate validation logic

## MCP Adapter Rules

MCP is a first-class adapter, not a shortcut around the backend.

Rules:

- tools call the same application behavior as HTTP
- write tools must enforce the same validation and authorization rules
- uploads must stay bounded by actor, byte caps, and expiration
- MCP results should include structured content whenever useful

## Configuration

Committed defaults live in `appsettings.json`.

Local secrets belong in:

```text
src/CodeCafe.Server/appsettings.Development.json
```

Do not commit real credentials, connection strings, private hosts, or per-machine settings.

Prefer strongly typed options and validate critical configuration at startup.

## Testing

Current test layers:

- `CodeCafe.Api.Tests`
- `CodeCafe.Application.Tests`
- `CodeCafe.Architecture.Tests`
- `CodeCafe.Mcp.Tests`
- `CodeCafe.Server.Tests`

Focus:

- domain and application rules below HTTP
- adapter contract tests at API and MCP boundaries
- architecture tests for dependency direction

## Deployment Standard

`CodeCafe.Server` is the backend host used for publish, deploy, and migration flows.

Migration command:

```bash
dotnet CodeCafe.Server.dll migrate
```

The database sync tool and CI publish flow should treat `CodeCafe.Server` as the backend startup project.

## Review Checklist

- Business logic in endpoint code: move it into `Application`.
- Infrastructure dependency in `Domain` or `Application`: fix the dependency direction.
- New generic repository abstraction: challenge it.
- Transport-specific branching in shared use-case code: push it back to the adapter.
- New backend feature added to legacy-style shared helpers: stop and reshape it as a slice.
