# CodeCafe Backend Best Practices

This guide reflects the current backend standard for CodeCafe.

`CodeCafe.WebApi` has been retired. New backend work targets `CodeCafe.Api`,
`CodeCafe.Ai`, `CodeCafe.Mcp`, and `CodeCafe.Server` only.

## Architecture

The backend is now organized around three adapters and a shared core:

```text
CodeCafe.Server
├─ CodeCafe.Api
├─ CodeCafe.Ai
├─ CodeCafe.Mcp
├─ CodeCafe.Application
├─ CodeCafe.Domain
└─ CodeCafe.Infrastructure
```

Dependency direction:

```text
Domain <- Application <- Infrastructure
                      <- Api
                      <- Ai
                      <- Mcp
Server -> Api + Ai + Mcp + Application + Infrastructure
```

Rules:

- `CodeCafe.Domain` references nothing outside itself.
- `CodeCafe.Application` references only `CodeCafe.Domain`.
- `CodeCafe.Infrastructure` implements application abstractions.
- `CodeCafe.Api`, `CodeCafe.Ai`, and `CodeCafe.Mcp` are adapter libraries.
- `CodeCafe.Server` is the only runnable backend host and composition root.

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
- HTTP error mapping
- request/response shaping
- health endpoints

Must not own:

- business rules
- direct persistence logic
- startup hosting
- cookie/auth middleware policy
- rate limiting, CORS, or forwarded-header policy

### `CodeCafe.Ai`

Owns:

- in-app AI assistant endpoints and AG-UI integration
- AI-specific notebook tools
- AI prompt and model orchestration

Must not own:

- notebook business rules outside shared application behavior
- direct persistence logic
- a separate notebook content format
- long-term chat or edit history unless a future requirement explicitly adds it

AI notebook editing must treat TipTap JSON as the canonical content format. Markdown can remain an import format in MCP/API upload flows, but it must not become the AI write format.

### `CodeCafe.Mcp`

Owns:

- tools, resources, and prompts
- MCP result mapping
- MCP-specific upload/import support

Must not own:

- notebook business rules outside shared application behavior
- startup hosting
- deployment-only auth, origin, or rate-limit policy

### `CodeCafe.Server`

Owns:

- single-host composition of API and MCP
- backend middleware and host policy
- CORS, forwarded headers, antiforgery, auth cookies, and rate limiting
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
- `CodeCafe.Ai/Tools` or `CodeCafe.Ai/Endpoints`
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

## AI Adapter Rules

AI is a first-class adapter over the notebook application layer.

Rules:

- assistant reads and writes must call shared application services
- write tools must enforce the same validation and authorization rules as REST and MCP
- generated page content must be validated as TipTap JSON before persistence
- preview flows may diff extracted plain text, but must save TipTap JSON
- direct save requires explicit user intent
- do not add persistent AI chat history or AI edit history without a separate product requirement

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
- adapter contract tests at API and MCP boundaries using the combined `CodeCafe.Server` host
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
