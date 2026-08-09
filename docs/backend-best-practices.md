# CodeCafe Backend Best Practices

This guide reflects the current backend standard for CodeCafe.

> **Note (2026-07):** This document was rewritten to match the modular-monolith
> layout under `server/`. The older `src/CodeCafe.*` clean-architecture layout
> it previously described no longer exists.

## Architecture

The backend is a modular monolith. `CodeCafe.Server` is the only runnable host:

```text
server/
├─ Host/
│  └─ CodeCafe.Server                 — composition root + host policy
├─ Shared/
│  ├─ CodeCafe.Shared.Domain          — shared kernel: base types, primitives
│  ├─ CodeCafe.Shared.Application     — shared application abstractions/behaviors
│  └─ CodeCafe.Shared.Infrastructure  — shared persistence: ApplicationDbContext,
│                                       entity configurations, EF Core migrations
├─ Modules/
│  ├─ Identity/                       — users, auth endpoints, dynamic client registration
│  ├─ Notes/                          — notebooks, folders, pages, sharing
│  ├─ Mcp/                            — MCP adapter (tools, resources, prompts, uploads)
│  └─ Ai/                             — in-app AI assistant + AI edit/draft endpoints
├─ tests/                             — Api / Application / Domain / Infrastructure /
│                                       Architecture / Mcp / Server test projects
└─ CodeCafe.slnx
```

Module layering (Notes is the reference shape):

```text
CodeCafe.Modules.<X>.Domain         — entities, value objects, invariants
CodeCafe.Modules.<X>.Application    — commands/queries, MediatR handlers, validators, interfaces
CodeCafe.Modules.<X>.Infrastructure — EF Core persistence + external integrations
CodeCafe.Modules.<X>.Presentation   — HTTP endpoints, request/response shaping
```

Not every module needs all four projects (Identity has no Domain; Ai is a
single project; Mcp is Domain + one mixed project) — but the dependency
direction is always:

```text
Domain <- Application <- Infrastructure
                      <- Presentation (endpoints/adapters)
Server  -> everything (composition only)
```

Rules:

- `Domain` references nothing outside itself.
- `Application` references only its own Domain (+ shared application).
- `Infrastructure` implements application abstractions (EF Core, gateways).
- Endpoint/Presentation projects map transport <-> application, nothing more.
- `CodeCafe.Server` is the only composition root: middleware pipeline, CORS,
  forwarded headers, antiforgery, auth cookies, rate limiting, OpenIddict
  server config, health endpoints, migration startup command.
- Cross-module references should go through shared contracts, not by reaching
  into another module's Infrastructure. The shared `ApplicationDbContext` lives
  in `CodeCafe.Shared.Infrastructure` (with its entity configurations and EF Core
  migrations) and is the only sanctioned shared-persistence reference.

## Auth / OIDC Stays Controller-Based

The OpenIddict authorization-server endpoints (`/connect/authorize`,
`/connect/token`, dynamic client registration) live in MVC controllers under
the Identity module's Presentation (`HostAuth/`) and intentionally stay there.
They are inseparable from the OpenIddict ASP.NET Core integration:
`HttpContext.GetOpenIddictServerRequest()`, scheme-specific sign-in, and
`SignIn`/`Forbid` results are transport concerns, not portable use cases.

Guidance:

- Keep the OAuth/OIDC protocol flow in those controllers.
- Transport-agnostic auth concerns (registration/login response shaping)
  belong in application services behind interfaces.

## Vertical Slice Rules

New backend work should follow feature slices in the module's Application
project, e.g.:

```text
CodeCafe.Modules.Notes.Application/
  Notes/
    Commands/
    Queries/
    DTOs/
```

Adapters map into those slices:

- `CodeCafe.Modules.Notes.Presentation/...` (HTTP)
- `CodeCafe.Modules.Mcp/Tools/...` (MCP)
- `CodeCafe.Modules.Ai/...` (AI)

Do not add new backend behavior to large shared helpers or controller-style
classes. Do not let adapter projects accumulate business rules "because the
handler would be one line" — the slice is where invariants are enforced.

## MediatR and CQRS

All externally triggered business use cases should run through MediatR —
including MCP tools and AI endpoints, so that FluentValidation rules and
pipeline behaviors apply identically on every entry path.

Rules:

- commands change state
- queries do not change state
- validators run before handlers
- transactions wrap commands, not queries

Do not add a generic repository or ceremonial `UnitOfWork` layer on top of
EF Core. Business rules that today live in Infrastructure services should
move to Domain/Application over time (known debt: the notebook-item tree
rules in `NotebookItemMutationService`).

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

- tools call the same application behavior as HTTP (same MediatR use cases)
- write tools must enforce the same validation and authorization rules
- uploads must stay bounded by actor, byte caps, and expiration
- MCP results should include structured content whenever useful

## AI Adapter Rules

AI is a first-class adapter over the notebook application layer.

- assistant reads and writes must call shared application services/use cases
- write tools must enforce the same validation and authorization rules as REST and MCP
- generated page content must be validated as TipTap JSON before persistence
- preview flows may diff extracted plain text, but must save TipTap JSON
- direct save requires explicit user intent
- do not add persistent AI chat history or AI edit history without a separate product requirement

## Configuration

Committed defaults live in `appsettings.json`.

Local secrets belong in:

```text
server/src/CodeCafe.Host/appsettings.Development.json
```

Do not commit real credentials, connection strings, private hosts, or
per-machine settings.

Prefer strongly typed options and validate critical configuration at startup
(`ValidateOnStart`).

## Testing

Current test layers under `server/tests`:

- `CodeCafe.Api.Tests` — Notes HTTP endpoint contract tests
- `CodeCafe.Application.Tests` — handlers, validators
- `CodeCafe.Architecture.Tests` — dependency direction
- `CodeCafe.Domain.Tests`
- `CodeCafe.Infrastructure.Tests` — EF Core read/write paths (SQLite)
- `CodeCafe.Mcp.Tests`
- `CodeCafe.Server.Tests` — host/auth/security behavior

Focus:

- domain and application rules below HTTP
- adapter contract tests at API and MCP boundaries using the combined host
- architecture tests for dependency direction (update them when projects or
  namespaces change — no stale project names in assertions)

## Deployment Standard

`CodeCafe.Server` is the backend host used for publish, deploy, and migration flows.

Migration command:

```bash
dotnet CodeCafe.Server.dll migrate
```

## Review Checklist

- Business logic in endpoint code: move it into the module's Application layer.
- Infrastructure dependency in Domain or Application: fix the dependency direction.
- Adapter (MCP/AI) calling Infrastructure services directly instead of MediatR:
  route it through the same use case as HTTP.
- New generic repository abstraction: challenge it.
- Transport-specific branching in shared use-case code: push it back to the adapter.
- New backend feature added to legacy-style shared helpers: stop and reshape it as a slice.
- New table added to the shared `ApplicationDbContext` in `CodeCafe.Shared.Infrastructure`
  from another module: check whether it belongs to that module's own persistence instead.
