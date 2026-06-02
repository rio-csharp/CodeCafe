# CodeCafe Backend Architecture Rebuild Plan

## Status

- Status: Rebuild largely completed; cleanup and core hardening remain
- Last reviewed: 2026-06-01
- Scope: Backend architecture, adapter split, server composition, test architecture
- Target release base: `release/2.3`
- Working branch intent: `feature/* -> release/2.3`

---

## 1. Objective

Rebuild the CodeCafe backend into a structure that can absorb sustained feature
growth without recurring large-scale reorganizations.

This plan is intentionally not a light refactor. The goal is to move the
backend to a stable long-term architecture with:

- Modular monolith boundaries
- Clean Architecture dependency direction
- Vertical slice organization
- MediatR-based CQRS
- Thin HTTP and MCP adapters
- Richer domain and application behavior
- Enforced architectural rules through tests

The desired outcome is not "never refactor again." The desired outcome is that
future feature work stays local to a slice or module, with limited ripple
effects across the solution.

---

## 2. Problem Summary

The current backend already has the beginnings of Clean Architecture, but the
center of gravity is still misplaced.

Original pain points:

- `CodeCafe.WebApi` contained too much adapter-specific and feature-specific code.
- Controllers are thin in some places, but several endpoints still contain
  authorization or workflow branching that belongs to the use-case layer.
- `CodeCafe.Application` is still mostly contracts and models rather than the
  primary home of use cases.
- `CodeCafe.Infrastructure` currently contains substantial business workflow
  logic, especially in Notes services.
- Domain entities are mostly data holders and do not yet protect enough
  business behavior or invariants directly.
- MCP is implemented inside the Web API project, which couples two adapters
  that should be allowed to evolve separately.
- Test coverage is strongest at the API integration level, while architecture
  and application-level regression protection is still too weak.

This means the repo has a good shell, but the internals are not yet arranged to
support rapid growth cleanly.

---

## 3. Target Architecture

Adopt this backend model:

```text
HTTP Endpoint / MCP Tool
        |
     MediatR
        |
Application Handler
        |
  Domain + Policies
        |
 Infrastructure
```

Architecture style:

- Modular monolith
- Vertical slice by feature
- CQRS at the application boundary
- Shared persistence process, not distributed services
- Explicit adapter boundaries for HTTP and MCP

Why this shape:

- It keeps deployment and local development simple.
- It gives strong separation without premature microservices.
- It reduces controller and service bloat.
- It lets the Notes, Auth, MCP, and future features grow independently inside
  one solution.

---

## 4. Non-Negotiable Design Rules

These rules define the rebuilt architecture.

1. `CodeCafe.Domain` references nothing outside itself.
2. `CodeCafe.Application` references only `CodeCafe.Domain`.
3. `CodeCafe.Infrastructure` implements application abstractions and may depend
   on `Application` and `Domain`.
4. `CodeCafe.Api` and `CodeCafe.Mcp` are adapters only. They do not contain
   business rules.
5. Every externally visible use case is modeled as a command or query.
6. Validation, transactions, logging, and performance tracking run through
   MediatR pipeline behaviors, not repeated by handlers.
7. Generic repository abstractions are not allowed.
8. `DbContext` remains the primary persistence unit; we do not add a ceremonial
   `UnitOfWork` wrapper unless a concrete need appears later.
9. MCP and HTTP must call the same application use cases.
10. New features are added as slices, not by expanding shared "helper" or
    "manager" classes.

---

## 5. Target Solution Shape

```text
src/
├─ CodeCafe.Api/
│  ├─ Common/
│  ├─ DependencyInjection/
│  ├─ Endpoints/
│  │  ├─ Auth/
│  │  ├─ Health/
│  │  └─ Notes/
│  └─ Program.cs
├─ CodeCafe.Mcp/
│  ├─ Common/
│  ├─ DependencyInjection/
│  ├─ Prompts/
│  ├─ Resources/
│  └─ Tools/
├─ CodeCafe.Application/
│  ├─ Abstractions/
│  ├─ Behaviors/
│  ├─ Common/
│  ├─ Auth/
│  └─ Notes/
├─ CodeCafe.Domain/
│  ├─ Common/
│  ├─ Auth/
│  └─ Notes/
├─ CodeCafe.Infrastructure/
│  ├─ DependencyInjection/
│  ├─ Identity/
│  ├─ Notes/
│  ├─ Persistence/
│  └─ Services/
└─ CodeCafe.Migrations/   # optional; create only if migration concerns need isolation

tests/
├─ CodeCafe.Api.Tests/
├─ CodeCafe.Application.Tests/
├─ CodeCafe.Architecture.Tests/
├─ CodeCafe.Domain.Tests/
├─ CodeCafe.Infrastructure.Tests/
└─ CodeCafe.Mcp.Tests/
```

Notes:

- `CodeCafe.Api` and `CodeCafe.Mcp` are adapter class libraries composed by
  `CodeCafe.Server`, which is the only backend host path.
- `CodeCafe.Migrations` is optional. Only create it if we want to decouple
  startup hosting from migration ownership.

---

## 6. Project Responsibilities

### `CodeCafe.Api`

Owns:

- ASP.NET Core host bootstrapping
- endpoint registration
- auth/cookie wiring for browser API traffic
- ProblemDetails and HTTP exception mapping
- CORS, health checks, OpenAPI, rate limiting, middleware

Must not own:

- business rules
- EF queries
- transaction logic
- notebook rules
- MCP-specific protocol code

### `CodeCafe.Mcp`

Owns:

- MCP transport wiring
- tool/resource/prompt definitions
- MCP-specific auth/resource configuration
- MCP result mapping

Must not own:

- notebook workflow logic
- persistence logic
- business validation beyond transport/schema validation

### `CodeCafe.Application`

Owns:

- commands, queries, handlers
- feature DTOs and result contracts
- validators
- application interfaces
- orchestration of domain and infrastructure calls
- MediatR pipeline behaviors

Must not own:

- EF Core
- ASP.NET Core
- MCP SDK concerns
- direct configuration lookup from environment

### `CodeCafe.Domain`

Owns:

- aggregates, entities, value objects
- invariant protection
- domain policies
- domain events where useful

Must not own:

- serialization contracts
- persistence annotations that leak infrastructure concerns
- HTTP or MCP concepts

### `CodeCafe.Infrastructure`

Owns:

- EF Core persistence
- repository implementations
- identity integrations
- external services
- content transformation implementations
- system clock, audit storage, and other technical adapters

Must not own:

- primary use-case orchestration
- transport-specific response mapping

---

## 7. Feature Slice Template

Each major feature should follow a vertical slice structure.

Example for Notes:

```text
CodeCafe.Application/
  Notes/
    Commands/
      CreateNotebook/
        CreateNotebookCommand.cs
        CreateNotebookValidator.cs
        CreateNotebookHandler.cs
        CreateNotebookResult.cs
      UpdateNotebook/
      CreateNotebookItem/
      UpdateNotebookItem/
      ReorderNotebookItems/
      ArchiveNotebookItem/
      RestoreNotebookItem/
    Queries/
      GetNotebookById/
      GetNotebookBySlug/
      GetNotebookItems/
      GetPublicNotebooks/
      SearchVisibleNotebookItems/
    DTOs/
    Mapping/
```

Endpoint structure:

```text
CodeCafe.Api/
  Endpoints/
    Notes/
      CreateNotebookEndpoint.cs
      UpdateNotebookEndpoint.cs
      GetNotebookEndpoint.cs
      GetNotebookItemsEndpoint.cs
```

MCP structure:

```text
CodeCafe.Mcp/
  Tools/
    Notes/
      CreateNotebookTool.cs
      UpdateNotebookItemTool.cs
      SearchNotesTool.cs
```

---

## 8. CQRS, MediatR, Repository, and Unit of Work Rules

### MediatR

Required.

Every externally triggered business use case should execute through MediatR.
This gives a predictable pipeline for cross-cutting concerns and keeps adapters
thin.

### CQRS

Required, but logical rather than physical.

Rules:

- Commands change state.
- Queries do not change state.
- Read models may differ from write models.
- Do not split into separate databases during this rebuild.

### Repository

Allowed only at aggregate or module boundary.

Good:

- `INotebookRepository`
- `INotebookReadRepository`
- `IUserIdentityRepository`

Not allowed:

- generic `IRepository<TEntity>`
- generic `IUnitOfWorkRepository<T>`

### Unit of Work

Handled through EF Core `DbContext` and MediatR transaction behavior.

This rebuild should not introduce an extra abstract `UnitOfWork` layer unless a
future module needs coordinated persistence across more than one write store.

---

## 9. Required Cross-Cutting Behaviors

Add these MediatR behaviors in `CodeCafe.Application`.

- Validation behavior
- Transaction behavior
- Structured logging behavior
- Performance timing behavior
- Unhandled exception enrichment behavior
- Idempotency behavior for selected commands if later required

Rules:

- Validation runs before handlers.
- Transactions wrap command handlers, not queries.
- Logging includes correlation identifiers and use-case names.
- Behaviors must be deterministic and easy to test.

---

## 10. Endpoint Strategy

Replace large controllers with endpoint classes or endpoint modules.

Preferred shape:

- one endpoint class per use case
- explicit request model
- explicit response model
- minimal adapter logic

The API request path should become:

1. bind request
2. authorize at transport boundary where appropriate
3. send command/query to MediatR
4. map result to HTTP response

Endpoint rules:

- no domain logic
- no direct `DbContext` usage
- no feature workflow branching
- no repeated manual validation

This keeps the API adapter small even as the feature set grows.

---

## 11. Domain Rebuild Strategy

The current domain is too anemic for the target architecture. The rebuild should
gradually move meaningful rules inward.

For Notes, move these categories toward the domain/application core:

- notebook visibility and publication rules
- notebook ownership and mutation policies
- page and folder tree movement rules
- archive and restore invariants
- path and slug semantics
- optimistic concurrency expectations

Guideline:

- if the rule is business meaning, move it to domain or application
- if the rule is storage technique, keep it in infrastructure

Not every helper becomes a domain method, but the current `NotesSupport` style
should be reduced aggressively.

---

## 12. MCP Rebuild Strategy

The long-term adapter shape should be:

- `CodeCafe.Api` for browser and REST traffic
- `CodeCafe.Mcp` for protocol-driven automation traffic

MCP split objectives:

- isolate protocol concerns from HTTP controller/endpoints
- avoid continued growth of the API host project
- keep shared use cases in `Application`
- keep transport-specific schemas and result mapping in the MCP adapter

Rules:

- MCP tools call MediatR commands and queries
- MCP does not call controllers
- MCP does not bypass authorization or validation
- MCP write paths use the same concurrency and audit rules as REST

---

## 13. Testing Strategy

The rebuilt architecture must be protected by tests that match the new
boundaries.

### `CodeCafe.Architecture.Tests`

Add architectural tests for:

- project dependency direction
- forbidden namespace references
- endpoint assemblies not referencing persistence types directly
- handlers not depending on adapter assemblies

### `CodeCafe.Domain.Tests`

Focus on:

- invariants
- state transitions
- edge-case rules
- tree and archive semantics

### `CodeCafe.Application.Tests`

Focus on:

- command/query handler behavior
- validator coverage
- pipeline behavior coverage
- permission and conflict orchestration

### `CodeCafe.Infrastructure.Tests`

Focus on:

- EF mappings
- repository behavior
- persistence queries
- identity and external service adapters

### `CodeCafe.Api.Tests`

Focus on:

- routing
- auth behavior
- request/response contracts
- ProblemDetails mapping

### `CodeCafe.Mcp.Tests`

Focus on:

- tool registration
- auth and scope behavior
- schema validation
- result mapping

---

## 14. Migration Phases

This rebuild should happen in controlled phases, even if the target is a deep
restructure.

### Phase 0: Foundation

- create target projects
- wire solution references
- add MediatR and FluentValidation
- add application pipeline behaviors
- add architecture test project
- keep existing runtime behavior unchanged

Exit criteria:

- solution builds
- architecture tests run
- new projects are wired

### Phase 1: API Adapter Reshape

- create endpoint pattern in `CodeCafe.Api`
- move common HTTP concerns from controller implementations into shared API
  infrastructure
- preserve existing routes and contracts

Exit criteria:

- at least one feature is served from endpoint classes
- existing frontend contracts still pass

### Phase 2: Notes Slice Migration

- migrate Notes commands and queries into `CodeCafe.Application`
- replace `NotebookCommandService` orchestration with handlers
- replace `NotebookQueryService` query orchestration with handlers and read
  repositories
- move Notes-specific policies out of helper-heavy infrastructure code

Exit criteria:

- Notes REST uses MediatR end-to-end
- Notes MCP uses the same handlers
- old Notes application flow services are removed or reduced to infrastructure
  adapters only

### Phase 3: Auth Slice Migration

- migrate auth use cases to vertical slices where beneficial
- keep host-level authentication wiring in API/infrastructure
- isolate authorization server support from transport glue

Exit criteria:

- auth endpoints use the same endpoint and MediatR conventions
- auth contracts remain stable

### Phase 4: MCP Adapter Extraction

- move MCP code from `CodeCafe.WebApi` into `CodeCafe.Mcp`
- keep shared use cases untouched
- add MCP integration tests against the new adapter project

Exit criteria:

- MCP no longer lives in API project
- transport split is complete

### Phase 5: Cleanup

- delete obsolete controllers/services/helpers
- rename projects where needed
- update solution docs
- tighten architecture tests

Exit criteria:

- old structure no longer drives new feature work
- docs match the rebuilt solution

---

## 15. Notes Module First-Class Migration Map

Notes is the first migration target because it currently has the heaviest mix of
transport, workflow, and persistence logic.

Move from:

- large Notes controller
- infrastructure-heavy Notes orchestration services
- shared Notes helper logic
- adapter-specific branching scattered across REST and MCP

Move to:

- Notes command/query handlers
- aggregate-aware repositories
- smaller endpoint classes
- transport-neutral use cases

First command/query inventory:

- `CreateNotebookCommand`
- `UpdateNotebookCommand`
- `DeleteNotebookCommand`
- `CreateNotebookItemCommand`
- `UpdateNotebookItemCommand`
- `ReorderNotebookItemsCommand`
- `ArchiveNotebookItemCommand`
- `RestoreNotebookItemCommand`
- `DeleteNotebookItemCommand`
- `GetNotebookByIdQuery`
- `GetNotebookBySlugQuery`
- `GetNotebookItemsQuery`
- `GetPublicNotebooksQuery`
- `GetMyNotebooksQuery`
- `GetPublicNotebookItemQuery`
- `SearchVisibleNotebookItemsQuery`
- `GetNotebookFavoriteStatusQuery`

---

## 16. Delivery Rules During Rebuild

To avoid a half-old, half-new architecture lingering indefinitely, use these
rules during the migration:

1. New backend features must be added only in the new slice pattern once the
   foundation lands.
2. Existing controller actions may be touched only when migrating them to
   endpoints or fixing production bugs.
3. New business rules must not be added to infrastructure helper classes.
4. New shared abstractions require a concrete second use case, not speculative
   design.
5. Frontend-facing API contracts should stay stable unless a release-specific
   breaking change is explicitly planned.

---

## 17. Risks and Controls

### Risk: Excessive framework ceremony

Control:

- use MediatR and validators deliberately
- avoid generic repository layers
- avoid mapping explosion unless there is a real contract difference

### Risk: Half-migrated architecture

Control:

- migrate by complete feature slices
- delete old code after slice completion
- add architecture tests early

### Risk: Regression during deep Notes migration

Control:

- keep API and MCP integration coverage green
- add application-level tests before removing old logic
- migrate use cases one command/query at a time

### Risk: Over-modeling the domain

Control:

- move real invariants inward
- do not invent fake entities or domain services for simple CRUD without rules

---

## 18. Definition of Done

The rebuild is complete when:

- `CodeCafe.Api` and `CodeCafe.Mcp` are distinct adapter projects
- all externally invoked business use cases run through MediatR
- controllers are replaced or reduced to thin legacy shells pending deletion
- Notes no longer relies on infrastructure orchestration services as the primary
  use-case implementation
- architecture tests enforce dependency direction
- application tests cover the main use cases directly
- new backend features can be added as isolated slices without modifying large
  shared controller/service/helper files

---

## 19. Immediate Next Steps

1. Continue moving Notes business orchestration out of infrastructure-heavy services.
2. Expand direct `Application` and `Domain` regression coverage.
3. Keep deployment, CI, and operational docs aligned to `CodeCafe.Server`.
4. Tighten architecture tests as new backend slices land.

---

## 20. Decision Summary

This rebuild adopts:

- modular monolith over microservices
- vertical slice organization over layered feature dumping
- MediatR + CQRS over controller/service-centric orchestration
- thin endpoints over growing controllers
- explicit repositories over generic repositories
- `DbContext` transaction boundary over ceremonial `UnitOfWork` wrappers
- separate API and MCP adapters over one overloaded Web API project

This is the target architecture that should be treated as the new backend
standard for CodeCafe starting from the `release/2.3` line.
