# CodeCafe Backend Best Practices

> This guide is for the CodeCafe backend development team to keep the API clean,
> testable, secure, and easy to evolve.
>
> Tech stack: ASP.NET Core Web API + Clean Architecture + .NET 10.

---

## Philosophy

We optimize for:

- **Clear boundaries**: each layer has a small, obvious job.
- **Explicit behavior**: configuration, validation, errors, and side effects should be visible.
- **Testability**: business rules should be testable without HTTP, databases, or external services.
- **Operational safety**: deployment, rollback, logging, and health checks are part of the product.
- **Simple code first**: abstractions are earned by repeated need, not created just in case.
- **SOLID design**: responsibilities, dependencies, and extension points should stay intentional.
- **Test-guided development**: tests should shape business behavior before implementation hardens.

We do not optimize for:

- **Framework magic**: implicit conventions are fine only when the team can predict them.
- **Generic repositories everywhere**: use abstractions when they protect business code from infrastructure details.
- **Large service classes**: split by use case or cohesive domain behavior before a class becomes a dumping ground.
- **Premature infrastructure**: do not add queues, caches, background jobs, or distributed locks before the workflow needs them.

---

## 1. SOLID Principles

SOLID is a good default for CodeCafe backend code. Use it as a practical review tool, not as an excuse to create unnecessary indirection.

### Single Responsibility Principle

A class should have one reason to change.

Good signs:

- A controller only handles HTTP concerns.
- An application service only coordinates one use case or one cohesive workflow.
- A domain entity protects its own invariants.
- Infrastructure adapters only handle one external concern.

Bad signs:

- A service validates input, performs authorization, sends email, writes files, and builds HTTP responses.
- A controller contains business rules.
- A repository also decides user permissions.

**Review rule of thumb**: if a class name needs `And`, `Manager`, or `Helper`, question its responsibility.

### Open/Closed Principle

Code should be open for extension and closed for risky modification.

Use this when behavior has clear variants:

- Different storage providers.
- Different notification channels.
- Different external API clients.
- Different policy implementations.

Do not create abstractions for one-off behavior that has no real second implementation.

**Review rule of thumb**: add an abstraction when a second implementation is real or the dependency is genuinely external.

### Liskov Substitution Principle

An implementation should be usable anywhere its abstraction is expected.

Avoid implementations that surprise callers:

- A repository method that sometimes throws for ordinary "not found" behavior while another returns `null`.
- A fake test implementation that behaves very differently from the production implementation.
- A derived type that weakens validation or violates domain invariants.

**Review rule of thumb**: callers should not need to know the concrete implementation to use an interface safely.

### Interface Segregation Principle

Prefer small, role-focused interfaces over broad interfaces.

Good:

```csharp
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
```

Bad smell:

```csharp
public interface ISystemService
{
    DateTimeOffset UtcNow { get; }
    Guid NewGuid();
    Task SendEmailAsync();
    Task WriteFileAsync();
}
```

**Review rule of thumb**: if an implementation throws `NotSupportedException` for part of an interface, the interface is too broad.

### Dependency Inversion Principle

Business code should depend on abstractions for infrastructure concerns.

Application code can define what it needs:

```csharp
public interface IWorkspaceRepository
{
    Task<Workspace?> GetAsync(Guid id, CancellationToken cancellationToken);
}
```

Infrastructure decides how to implement it.

Do not invert dependencies for pure domain behavior or simple in-process code. The goal is to keep policies independent from details, not to wrap every method call in an interface.

**Review rule of thumb**: dependencies that touch time, files, databases, network, queues, secrets, or external services should usually be inverted.

---

## 2. Architecture Boundaries

The backend follows Clean Architecture:

```text
Domain <- Application <- Infrastructure
                      <- WebApi
```

### Domain

Use `CodeCafe.Domain` for:

- Entities and value objects.
- Domain invariants.
- Domain-specific exceptions or result concepts.
- Pure business behavior that does not require infrastructure.

Do not use `Domain` for:

- EF Core attributes or persistence details.
- ASP.NET Core types.
- Logging, HTTP, files, clocks, queues, or external services.
- DTOs shaped for API requests or responses.

### Application

Use `CodeCafe.Application` for:

- Use cases and application services.
- Interfaces needed by use cases, such as clocks, repositories, file storage, email, or external APIs.
- Validation rules that belong to an operation.
- DTOs or result objects used inside the application layer.

Do not use `Application` for:

- ASP.NET Core controllers or middleware.
- EF Core `DbContext` implementations.
- Concrete external clients.
- Reading environment variables directly.

### Infrastructure

Use `CodeCafe.Infrastructure` for:

- Database access and repository implementations.
- File storage, external HTTP clients, email, queues, cache, and other adapters.
- Implementations of application interfaces.

Infrastructure can depend on `Application`, but application code should not know which infrastructure implementation is used.

### WebApi

Use `CodeCafe.WebApi` for:

- HTTP endpoints, controllers, request/response models, filters, and middleware.
- Authentication/authorization wiring.
- Dependency injection composition.
- OpenAPI, health checks, CORS, and host-level concerns.

Controllers should be thin. They translate HTTP into application calls and translate application results into HTTP responses.

**Review rule of thumb**: if a controller contains business branching that would matter outside HTTP, move it into `Application`.

---

## 3. Dependency Direction

Allowed project references:

```text
CodeCafe.Application -> CodeCafe.Domain
CodeCafe.Infrastructure -> CodeCafe.Application
CodeCafe.WebApi -> CodeCafe.Application
CodeCafe.WebApi -> CodeCafe.Infrastructure
Tests -> the layer being tested
```

Avoid references in the opposite direction.

Bad signs:

- `Domain` references `Application`, `Infrastructure`, or `WebApi`.
- `Application` references `Infrastructure` or `WebApi`.
- Business rules require `HttpContext`.
- Tests need a real database or web server for simple domain behavior.

---

## 4. Configuration

`appsettings.json` is committed and acts as the template/reference file.

Local secret values belong in:

```text
src/CodeCafe.WebApi/appsettings.Development.json
```

That file is ignored by Git, excluded from Docker build context, and excluded from publish output.

Deployment values should be injected through:

- Environment variables.
- GitHub secrets.
- Kubernetes secrets.
- Helm values.

Do not commit real credentials, tokens, connection strings, private hosts, or per-machine settings.

Prefer strongly typed options:

```csharp
builder.Services.Configure<MyFeatureOptions>(
    builder.Configuration.GetSection("MyFeature"));
```

Validate important options at startup when invalid values would cause production failure.

**Review rule of thumb**: if a value changes by environment, it should not be hardcoded in application code.

---

## 5. API Design

Use predictable REST-ish routes:

```http
GET    /api/workspaces
GET    /api/workspaces/{id}
POST   /api/workspaces
PUT    /api/workspaces/{id}
DELETE /api/workspaces/{id}
```

Prefer nouns over verbs in route names.

Use HTTP status codes deliberately:

| Case | Status |
|------|--------|
| Success with body | `200 OK` |
| Created resource | `201 Created` |
| Success without body | `204 No Content` |
| Validation failure | `400 Bad Request` |
| Not authenticated | `401 Unauthorized` |
| Not allowed | `403 Forbidden` |
| Missing resource | `404 Not Found` |
| Conflict with current state | `409 Conflict` |

Use stable request/response contracts. Do not return domain entities directly from controllers.

**Review rule of thumb**: API models belong to the HTTP boundary; domain entities belong to the domain.

---

## 6. Validation

Validate at the boundary first:

- Required fields.
- String lengths.
- Numeric ranges.
- Enum values.
- URL/email/date formats.

Then enforce business invariants in the domain or application layer.

Do not rely only on frontend validation. The backend owns data integrity.

Bad smell:

```csharp
public async Task CreateWorkspace(string name)
{
    // accepts null, empty, or arbitrary length and hopes the caller behaved
}
```

Good shape:

```csharp
public sealed record CreateWorkspaceRequest(string Name);
```

Then validate before creating the domain object or running the use case.

---

## 7. Error Handling

Prefer one central error handling path in `WebApi`, such as exception handling middleware or `IExceptionHandler`.

Application and domain code should throw meaningful exceptions or return explicit results. Controllers should not duplicate exception mapping everywhere.

Return consistent error responses. Prefer `ProblemDetails` for HTTP errors.

Do not leak:

- Stack traces.
- Connection strings.
- Secret values.
- Internal file paths.
- Raw external service responses containing sensitive data.

**Review rule of thumb**: every expected failure mode should map to a predictable HTTP response.

---

## 8. Persistence

Keep persistence details in `Infrastructure`.

When EF Core is introduced:

- Put `DbContext` in `Infrastructure`.
- Keep migrations in the same persistence boundary.
- Use explicit configurations for non-trivial entities.
- Avoid lazy loading by default.
- Use cancellation tokens in async database calls.
- Avoid returning `IQueryable` across layer boundaries.

Repository abstractions are useful when they protect use cases from persistence details. Do not create generic repositories unless they make the application code clearer.

**Review rule of thumb**: `Application` asks for what it needs; `Infrastructure` decides how to retrieve it.

---

## 9. Time, Randomness, and External Effects

Do not call these directly from business code:

- `DateTime.UtcNow`
- `DateTimeOffset.UtcNow`
- `Guid.NewGuid()` when deterministic tests matter
- `Random.Shared`
- File system APIs
- HTTP clients

Wrap them behind application interfaces when the value affects business behavior.

The existing `IDateTimeProvider` pattern is the right direction.

---

## 10. Async and Cancellation

Use async APIs for I/O.

Pass `CancellationToken` through:

- Controllers.
- Application services.
- Repository calls.
- External HTTP calls.

Avoid `.Result`, `.Wait()`, and sync-over-async.

**Review rule of thumb**: public async application methods should usually accept a `CancellationToken`.

---

## 11. Security

Security defaults:

- Treat all request input as untrusted.
- Use authorization policies for permission checks.
- Do not put secrets in logs.
- Do not log request bodies by default.
- Keep CORS narrow in deployed environments.
- Use secure cookie settings when cookies are introduced.
- Use rate limiting for login or expensive endpoints when those endpoints exist.

Authentication answers “who are you?” Authorization answers “may you do this?” Keep them separate.

CORS should be explicit. Local development may allow the Vite dev server, but
test, preview, and production should allow only the matching frontend host for
that environment.

---

## 12. Logging and Observability

Use structured logging:

```csharp
logger.LogInformation("Workspace {WorkspaceId} created", workspaceId);
```

Do not use string interpolation in log messages:

```csharp
logger.LogInformation($"Workspace {workspaceId} created");
```

Log useful events:

- Startup configuration validation failure.
- Authentication and authorization failures, without secrets.
- Important state transitions.
- External dependency failures.
- Background job failures.

Avoid noisy logs in normal request paths.

Keep health endpoints boring and reliable:

- `/health/live` tells the platform the process is alive.
- `/health/ready` tells the platform the app is ready to receive traffic.

During graceful shutdown, readiness should turn false before the process exits so Kubernetes can stop routing new requests while in-flight work drains.

Use a short Kubernetes `preStop` delay with enough `terminationGracePeriodSeconds`
for endpoint updates and in-flight request draining. If the API later owns Kafka
consumers or background workers, stop accepting new work first, mark readiness
false, and then drain in-flight work before process exit.

---

## 13. Testing

Use TDD where it gives leverage. CodeCafe does not require strict test-first development for every change, but we should prefer it for business behavior.

Best fit for TDD:

- Domain rules.
- Application use cases.
- Bug fixes.
- Authorization and permission behavior.
- Edge cases with dates, state transitions, or validation.

Less useful for strict TDD:

- Simple dependency injection wiring.
- Helm or workflow edits.
- Thin controller pass-through code.
- Exploratory spikes.

Recommended loop:

```text
Red -> Green -> Refactor
```

1. Write a failing test that describes the behavior.
2. Implement the smallest code that makes it pass.
3. Refactor while keeping tests green.

For bug fixes, start with a regression test whenever practical. The test should fail before the fix and pass after the fix.

Testing priority:

1. Domain rules.
2. Application use cases.
3. Infrastructure integration behavior.
4. API contract behavior.

Unit tests should not require:

- Real network calls.
- Real production credentials.
- Real external services.
- Real clock time.

Use integration tests when verifying:

- Routing and model binding.
- Authentication and authorization behavior.
- Database queries and migrations.
- Serialization contracts.

**Review rule of thumb**: a bug in a business rule should usually be caught below the HTTP layer.

**TDD rule of thumb**: if the behavior is important enough to explain in a PR, it is probably important enough to pin down with a test.

---

## 14. Dependency Injection

Each layer owns its service registration extension:

```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
```

Keep registrations close to the layer that owns the implementation.

Default lifetimes:

| Service type | Lifetime |
|--------------|----------|
| Stateless application service | Scoped or transient |
| EF Core `DbContext` | Scoped |
| External HTTP client | `HttpClientFactory` |
| Clock provider | Singleton |
| In-memory state | Avoid unless intentionally process-local |

Do not use service locator patterns in application code.

---

## 15. File and Folder Organization

Preferred backend shape:

```text
src/
├─ CodeCafe.Domain/
│  ├─ Common/
│  └─ <Feature>/
├─ CodeCafe.Application/
│  ├─ Common/
│  │  └─ Interfaces/
│  └─ <Feature>/
├─ CodeCafe.Infrastructure/
│  ├─ Persistence/
│  ├─ Services/
│  └─ <Feature>/
└─ CodeCafe.WebApi/
   ├─ Controllers/
   ├─ Middleware/
   ├─ Models/
   └─ Configuration/
```

Organize by feature when it improves cohesion. Avoid broad folders that become junk drawers, such as `Helpers`, `Managers`, or `Utils`.

---

## 16. Pull Request Review Checklist

| When you see... | Action |
|-----------------|--------|
| Controller with business logic | Move behavior to `Application` |
| Class with multiple reasons to change | Split by responsibility |
| Broad interface with unused members | Apply interface segregation |
| Infrastructure dependency in business code | Apply dependency inversion |
| Domain entity returned directly from API | Create response model |
| `Application` referencing `Infrastructure` | Fix dependency direction |
| Hardcoded environment value | Move to configuration |
| Secret-like value in code or config | Remove and inject at runtime |
| Repeated validation logic | Centralize at the boundary/use case |
| `.Result` or `.Wait()` | Use async all the way |
| Missing `CancellationToken` in I/O path | Thread token through |
| `DateTimeOffset.UtcNow` in business code | Use `IDateTimeProvider` |
| String-interpolated logs | Use structured logging template |
| Expected error handled ad hoc in controller | Use central mapping |
| No tests for new business rule | Add domain/application tests |
| Infrastructure detail in domain/application | Move behind interface |
| New package for a tiny problem | Question whether built-in code is enough |

---

## Backend Focus

This guide focuses on backend work:

- `src/CodeCafe.Domain`
- `src/CodeCafe.Application`
- `src/CodeCafe.Infrastructure`
- `src/CodeCafe.WebApi`
- backend tests under `tests/`
- backend-facing deployment concerns such as API image, health checks, configuration, and secrets

The frontend is developed in the same repository by another AI collaborator. Backend changes should preserve API contracts and document breaking changes, but this guide does not define frontend implementation rules.
