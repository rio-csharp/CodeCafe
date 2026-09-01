# AGENTS.md

## Git & PRs

- Run `git fetch origin` first, then create working branches from the intended remote base branch (`origin/release/*`), never from the current local branch.
- PRs: `feature/*` → `release/*`; `release/*` → `main` is allowed. Direct hotfix commits on `release/*` are allowed only when the user explicitly requests that flow.
- `main` is pull-request-only: never push directly to it.
- Only commit when the user explicitly asks.

## Local checks (run before claiming anything is green)

- Backend: `dotnet build server/CodeCafe.slnx` and `dotnet test server/CodeCafe.slnx` from the repository root.
- Frontend: `npm run lint && npm run test && npm run build && npm run e2e` from `clients/web`.
- Always report the results.

## Code conventions

### Comments

- Only "why" comments: magic-number rationale, non-obvious background a reader would need. Never "what" comments — the code must explain what it does by itself.

### Domain layer (tactical DDD)

- Entities: private setters, a private parameterless constructor (EF materialization), static `Create` factories. No public constructors carrying business state.
- `Notebook` is the aggregate root; every `NotebookItem` mutation goes through a root method. Child mutation methods stay `internal` (tests access them via `InternalsVisibleTo`).
- Expected business-rule failures return violation enums (e.g. `NotebookItemAddViolation`); reserve exceptions for malformed input (e.g. `ArgumentException` in value-object factories).
- Domain events are raised inside aggregates (`RaiseDomainEvent`) and dispatched by the Application layer after a successful save. The Domain project has zero external dependencies.
- Value objects: `sealed record` + private constructor + static `Create` with validation/normalization (make illegal states unrepresentable).
- Cross-aggregate references by Id only — no navigation properties across aggregates.

### Interfaces

- Only at architectural boundaries: implementation in another layer, multiple implementations, or a required test seam. Pure logic stays as plain (static) classes without interfaces. No speculative abstractions (YAGNI).

### Folder organization

- Within a bounded-context folder: entities at the context root; `Enums/`, `Events/`, `ValueObjects/`, `Services/` subfolders. Namespaces follow folders.

### Tests

- xUnit `Fact`/`Theory`; test through the public API of the aggregate.
- A change lands only when its tests are green.
