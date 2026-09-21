# Principles

The rules this codebase lives by.

> Verified against the `server/` working tree on top of `0b166f1`. If this file and the code
> disagree, **the code is right** — fix this file in the same commit that changes the code.

## 1. Working agreements

- Code is the single source of truth. A document explains *why*; it never restates what the code
  already shows.
- Comments explain why, in English, and only where the code cannot (`AGENTS.md`).
- Conventional Commits, scoped where useful (`AGENTS.md`).
- Do not commit unless explicitly asked.
- Ship vertically: one feature area at a time (auth → notebooks → pages → blocks → revisions →
  search → trash → AI → frontend), each taken through Domain → Application → Infrastructure → Host
  → tests. Advancing layer-by-layer across the whole system was attempted and abandoned.
- Do not record progress in docs ("X was added in slice N", test counts). It rots within days and
  misleads; record only what the code cannot express.

## 2. Layering

Dependencies point inward.

| Project | May depend on |
|---|---|
| `Domain` | `MediatR.Contracts` only (for `IDomainEvent : INotification`) |
| `Application` | `Domain`, `MediatR`, `FluentValidation` |
| `Infrastructure` | `Application` |
| `Host` | `Application`, `Infrastructure` (composition root) |

- No service layer: handlers use repository abstractions directly and save through `IUnitOfWork`.
- An interface is warranted only where a database, the clock, or another external resource is
  involved; pure logic is a named static class (`NotebookSlug`).
- Application never touches `HttpContext`; it reads the caller through `ICurrentUserAccessor`.
- Authentication is a custom `AuthenticationHandler` validating the bearer token through
  `IAccessTokenService` — not the JWT bearer middleware.

## 3. Domain

- One aggregate root per consistency boundary. Private setters, static factories, mutations only
  through aggregate methods; collections exposed as `IReadOnlyCollection<T>` over a private field
  (`UsePropertyAccessMode(PropertyAccessMode.Field)`), plus an EF-only parameterless constructor.
- Aggregates enforce their own invariants even when callers check first.
- `Entity` provides `Guid Id`, identity equality on `(type, Id)`, `==`/`!=`, and a domain-event buffer.
- Ids come from `Guid.CreateVersion7()`.
- Domain raises events; it never publishes or persists them.
- Constants such as `Notebook.MaxTitleLength` are the single source for limits, reused by EF
  configurations and validators.

## 4. Application

- One folder per use case, one type per file; the HTTP `Request` DTO stays separate from the
  `Command`/`Query` record.
- `ICommand<T>`/`IQuery<T>` wrap MediatR, so business code never mentions `IRequest`.
- Expected failures return `Result`/`Result<T>` with `Error(Code, Message, ErrorKind)`; exceptions are
  for bugs. `ErrorKind` is closed: Validation, Unauthorized, Forbidden, NotFound, Conflict,
  RateLimited, Unexpected.
- **An expected outcome never becomes a 500.** Whatever the caller can provoke has an `ErrorKind`, and
  Host maps it to the matching status. Where the database is the authority — a unique index rejecting
  a race no pre-check could see — persistence throws `UniqueConstraintViolationException` and the
  handler answers with the error its pre-check would have returned.
- Error instances are `static readonly` singletons grouped per feature (`NotebookErrors`, `AuthErrors`).
- `NotFound` doubles as "you may not see this", so existence never leaks. Reachability and permission
  are separate questions.
- Validation is FluentValidation, assembly-scanned and run by the `ValidationBehavior<,>` open behavior.
- Handlers take their dependencies through a primary constructor.
- Lists paginate with `PagedResult<T>` (offset + total count) when callers choose the sort;
  `CursorPage<T>` (keyset) fits fixed-sort feeds, and an unparsable cursor is a Validation error,
  never a silent fallback.
- Read models are records, not entities.

## 5. Host

- Controllers are one line: map to a command/query, `await sender.Send(...)`, return the `Result`.
- `ResultStatusCodeFilter` maps `ErrorKind` to the HTTP status; `ApiErrorResponsesConvention`
  publishes the same statuses into OpenAPI, because filters are invisible to ApiExplorer.
- Deny by default (`FallbackPolicy` requires an authenticated user); public endpoints opt out
  explicitly with `[AllowAnonymous]`.
- MCP tools are static `[McpServerTool]` methods named `codecafe_<verb>_<noun>`, mapping 1:1 onto an
  existing command or query, with read-only / idempotent / destructive hints declared where they apply.
- Cross-cutting concerns live in `Host/Hosting/*Extensions.cs`, wired by `AddCodeCafe` /
  `UseCodeCafePipeline`: Serilog, CORS, rate limiting, forwarded headers, security headers, health
  checks, OpenAPI.
- Rate limits partition on the authenticated user id, falling back to IP only for anonymous traffic.
- `GlobalExceptionHandler` maps unhandled exceptions to ProblemDetails.
- `Program` is `public partial class Program` so `WebApplicationFactory<Program>` can boot the app.

## 6. Persistence

- `AppDbContext` *is* the unit of work; there is no separate UoW class.
- One `IEntityTypeConfiguration<T>` per entity, assembly-scanned; table names are plural snake_case.
- `SaveChangesAsync` drains the change tracker's domain events and publishes them through MediatR
  **before** committing, so an event's reactions stage into the same transaction. Provider-specific
  unique violations become `UniqueConstraintViolationException`.
- Soft-deleted notebooks are hidden by a global query filter (`DeletedAtUtc == null`); trash queries
  opt out with `IgnoreQueryFilters()`.
- Enums persist as strings with an explicit length; `string[]` maps to `text[]`.
- Case-insensitive uniqueness (email) is normalized in Application and enforced by a plain unique index.
- Optimistic concurrency uses PostgreSQL's `xmin` as a shadow property, configured per entity as needed.
- Migrations are committed; `AppDbContextDesignTimeFactory` keeps `dotnet ef` working.

## 7. Testing

- Four projects, one per layer: `Domain.Tests` (pure logic), `Application.Tests` (test doubles),
  `Infrastructure.Tests` (real PostgreSQL), `Host.Tests` (`WebApplicationFactory` over real HTTP).
- Prefer a real dependency over a mock when it is cheap: constraints and concurrency are only proven
  against the real database.
- A feature's tests land with it, not after it.
