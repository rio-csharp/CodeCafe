# Architecture

## System Overview

CodeCafe's production-ready surface today is a structured notebook system for engineers. The same notebook data is available through:

- a React web application in `frontend/`
- a cookie-authenticated REST API in `CodeCafe.Api`
- an OAuth-protected MCP adapter in `CodeCafe.Mcp`

All of that is composed by `CodeCafe.Server`, which is the only runnable backend host.

```text
Browser
  -> React app
  -> /api/*
  -> CodeCafe.Server
       -> CodeCafe.Api
       -> CodeCafe.Application
       -> CodeCafe.Domain
       -> CodeCafe.Infrastructure
       -> PostgreSQL

MCP client
  -> /connect/* for OAuth/OIDC
  -> /mcp
  -> CodeCafe.Server
       -> CodeCafe.Mcp
       -> CodeCafe.Application / Infrastructure
       -> PostgreSQL
```

## Solution Shape

```text
src/
├─ CodeCafe.Domain/          # Entities, invariants, domain rules
├─ CodeCafe.Application/     # Commands, queries, validators, abstractions
├─ CodeCafe.Infrastructure/  # EF Core, identity, persistence implementations
├─ CodeCafe.Api/             # HTTP endpoints and transport models
├─ CodeCafe.Mcp/             # MCP tools, resources, prompts, upload support
└─ CodeCafe.Server/          # Host, middleware, auth, OpenIddict, composition root

tests/
├─ CodeCafe.Api.Tests/
├─ CodeCafe.Application.Tests/
├─ CodeCafe.Architecture.Tests/
├─ CodeCafe.Infrastructure.Tests/
├─ CodeCafe.Mcp.Tests/
└─ CodeCafe.Server.Tests/
```

Dependency direction:

```text
Domain <- Application <- Infrastructure
                      <- Api
                      <- Mcp

Server -> Api + Mcp + Application + Infrastructure
```

## Backend Responsibilities

### `CodeCafe.Domain`

- Owns notebook entities, note-item rules, and core business invariants.
- Must not depend on HTTP, MCP, EF Core, or configuration concerns.

### `CodeCafe.Application`

- Owns commands, queries, validators, MediatR handlers, and shared abstractions.
- Defines use-case behavior that both REST and MCP call into.

### `CodeCafe.Infrastructure`

- Owns EF Core persistence, Identity integrations, and concrete service implementations.
- Supplies the database-backed behavior used by application handlers and read services.

### `CodeCafe.Api`

- Owns minimal API endpoint registration, request/response models, and HTTP-specific mapping.
- Keeps business logic out of the transport layer.

### `CodeCafe.Mcp`

- Owns MCP tools, resources, prompts, upload handling, and MCP result mapping.
- Shares notebook behavior with the REST API instead of creating a parallel write path.

### `CodeCafe.Server`

- Owns middleware, OpenIddict/OAuth endpoints, rate limiting, CORS, antiforgery, readiness, and host policy.
- Is the publish, deploy, migration, and local-run backend target.

## Auth Model

CodeCafe intentionally uses different auth shapes for the browser and for MCP:

- Browser API traffic uses ASP.NET Core Identity cookies.
- Mutating browser API requests also require a CSRF token. The frontend client fetches and retries this automatically.
- Public notebook REST reads under `/api/notes/public/*` are anonymous.
- MCP traffic is authenticated by bearer tokens issued by the built-in OpenIddict server.
- The MCP protected-resource metadata document is exposed at `/.well-known/oauth-protected-resource/mcp`.

## Notebook Model And Contract Conventions

Current notebook behavior is built around a few conventions worth preserving:

- Visibility is `private`, `unlisted`, or `public`.
- `isPublished` is derived from visibility. Clients do not update it directly.
- Notebook detail responses include `items`, so the common read flow does not need a second item fetch just to render the tree.
- `PUT /api/notes/{notebookId}` is treated as a full notebook-settings update, not an implicit partial patch.
- Archived items are owner-only in both REST and MCP.
- Public REST notebook reads are anonymous, but MCP remains authenticated by default even for public-only read tools.

## Notes Request Paths

Typical browser request path:

1. React binds user input and calls `/api/*`.
2. API endpoints bind request models and hand work to application handlers or shared read services.
3. Infrastructure persists notebook state in PostgreSQL.
4. Responses return notebook summaries or notebook details with item trees.

Typical MCP request path:

1. The MCP client authenticates against `/connect/authorize` and `/connect/token`.
2. The client calls `/mcp`.
3. MCP tools and resources enforce scopes, then delegate to the same notebook behavior used by the REST adapter.
4. Responses are returned as structured MCP content.

## Testing Strategy

The current test split mirrors the architecture:

- `CodeCafe.Application.Tests` covers command/query behavior and validators.
- `CodeCafe.Api.Tests` covers HTTP contracts and endpoint behavior.
- `CodeCafe.Mcp.Tests` covers MCP tools, resources, and prompts.
- `CodeCafe.Infrastructure.Tests` covers persistence behavior.
- `CodeCafe.Server.Tests` covers combined-host behavior.
- `CodeCafe.Architecture.Tests` protects dependency direction and solution boundaries.
