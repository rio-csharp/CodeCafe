# Architecture

## System Overview

CodeCafe's production-ready surface today is a structured notebook system for engineers. The same notebook data is available through:

- a React web application in `clients/web/`
- a cookie-authenticated REST API served by the module Presentation projects (`CodeCafe.Modules.Notes.Presentation` for notebooks, `CodeCafe.Modules.Identity.Presentation` for auth)
- an OAuth-protected MCP adapter in `CodeCafe.Modules.Mcp`
- an optional in-app AI assistant in `CodeCafe.Modules.Ai`

All of that is composed by `CodeCafe.Server`, which is the only runnable backend host.

```text
Browser
  -> React app
  -> /api/*
  -> CodeCafe.Server
       -> CodeCafe.Modules.Notes.Presentation / Identity.Presentation
       -> CodeCafe.Modules.Ai
       -> module Application layers
       -> module Domain layers
       -> module Infrastructure + CodeCafe.Shared.Infrastructure
       -> PostgreSQL

MCP client
  -> /connect/* for OAuth/OIDC
  -> /mcp
  -> CodeCafe.Server
       -> CodeCafe.Modules.Mcp
       -> module Application / Infrastructure layers
       -> PostgreSQL
```

## Solution Shape

The backend is a modular monolith under `server/`. `CodeCafe.Server` is the only runnable host; Identity, Notes, MCP, and AI are modules composed by that host.

```text
server/
├─ Host/
│  └─ CodeCafe.Server/                 # Host, middleware, auth, OpenIddict, composition root
├─ Shared/
│  ├─ CodeCafe.Shared.Domain/          # Shared kernel: base types and primitives
│  ├─ CodeCafe.Shared.Application/     # Shared application abstractions and behaviors
│  └─ CodeCafe.Shared.Infrastructure/  # Shared ApplicationDbContext, entity configurations, EF Core migrations
├─ Modules/
│  ├─ Identity/                        # Application / Infrastructure / Presentation (no Domain project)
│  ├─ Notes/                           # Domain / Application / Infrastructure / Presentation
│  ├─ Mcp/                             # CodeCafe.Modules.Mcp.Domain + CodeCafe.Modules.Mcp (mixed project)
│  └─ Ai/                              # CodeCafe.Modules.Ai (single project)
├─ tests/
│  ├─ CodeCafe.Api.Tests/
│  ├─ CodeCafe.Application.Tests/
│  ├─ CodeCafe.Architecture.Tests/
│  ├─ CodeCafe.Domain.Tests/
│  ├─ CodeCafe.Infrastructure.Tests/
│  ├─ CodeCafe.Mcp.Tests/
│  └─ CodeCafe.Server.Tests/
└─ CodeCafe.slnx
```

Not every module needs all four projects — but the dependency direction is always:

```text
Domain <- Application <- Infrastructure
                      <- Presentation (endpoints/adapters)

Server -> everything (composition only)
```

`CodeCafe.Shared.Infrastructure` is the only sanctioned shared-persistence reference; it is referenced by module Infrastructure-level projects and the host, never by Domain or Application.

These rules are enforced by `server/tests/CodeCafe.Architecture.Tests/DependencyDirectionTests.cs`.

## Backend Responsibilities

### `CodeCafe.Shared.Domain`

- Owns shared-kernel base types and primitives used across modules.
- Must not depend on HTTP, MCP, EF Core, or configuration concerns.

### `CodeCafe.Shared.Application`

- Owns shared application abstractions, interfaces, and MediatR pipeline behaviors.

### `CodeCafe.Shared.Infrastructure`

- Owns the shared `ApplicationDbContext`, its entity configurations, and EF Core migrations.
- Is referenced only by module Infrastructure-level projects, the MCP adapter, and the host.

### `CodeCafe.Modules.Identity.*`

- Owns users, registration/login behavior, and the controller-based OpenIddict/OIDC endpoints (`/connect/*`, dynamic client registration) in its Presentation project.
- Has Application, Infrastructure, and Presentation projects; it has no Domain project.

### `CodeCafe.Modules.Notes.*`

- Owns notebooks, folders, pages, and sharing. It is the reference module shape with all four projects:
  - `Domain`: notebook entities, note-item rules, and core business invariants; depends on nothing outside itself.
  - `Application`: commands, queries, validators, and MediatR handlers; the use-case behavior that REST, MCP, and AI call into.
  - `Infrastructure`: EF Core persistence and concrete service implementations behind application abstractions.
  - `Presentation`: minimal API endpoint registration, request/response models, and HTTP-specific mapping; keeps business logic out of the transport layer.

### `CodeCafe.Modules.Mcp` / `CodeCafe.Modules.Mcp.Domain`

- Owns MCP tools, resources, prompts, upload handling, and MCP result mapping.
- Shares notebook behavior with the REST API through the same application use cases instead of creating a parallel write path.

### `CodeCafe.Modules.Ai`

- Owns the in-app notebook assistant, AG-UI integration, and AI-specific tools.
- Reads and writes notebooks through shared application services instead of direct persistence.
- Treats TipTap JSON as the canonical notebook content format for AI editing work.
- Does not own long-term chat history or a separate AI persistence model.

### `CodeCafe.Server`

- Owns middleware, OpenIddict server configuration, rate limiting, CORS, antiforgery, readiness, and host policy.
- Is the only composition root and the publish, deploy, migration, and local-run backend target.

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
- Page content is stored as TipTap JSON. Plain text is an extracted presentation/search aid, not the source format.
- Notebook detail responses include `items`, so the common read flow does not need a second item fetch just to render the tree.
- `PUT /api/notes/{notebookId}` is treated as a full notebook-settings update, not an implicit partial patch.
- Archived items are owner-only in both REST and MCP.
- Public REST notebook reads are anonymous, but MCP remains authenticated by default even for public-only read tools.

## AI Editing Direction

The in-app AI assistant should use backend-managed proposals for notebook edits instead of frontend-managed draft text.

AI writing must not introduce Markdown as a new persistence path. Existing import/upload flows may accept Markdown and convert it server-side, but AI notebook editing should produce validated TipTap JSON or structured operations that become validated TipTap JSON before persistence.

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
- `CodeCafe.Domain.Tests` covers domain rules and invariants.
- `CodeCafe.Api.Tests` covers HTTP contracts and endpoint behavior.
- `CodeCafe.Mcp.Tests` covers MCP tools, resources, and prompts.
- `CodeCafe.Infrastructure.Tests` covers persistence behavior.
- `CodeCafe.Server.Tests` covers combined-host behavior.
- `CodeCafe.Architecture.Tests` protects dependency direction and solution boundaries.
