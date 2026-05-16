# Architecture

CodeCafe starts as a notes-focused product, but the architecture treats notes as
the first domain module inside a broader AI workbench platform.

## Backend layers

- `CodeCafe.Domain`: domain entities and core business concepts.
- `CodeCafe.Application`: use cases, interfaces, and application orchestration.
- `CodeCafe.Infrastructure`: persistence, provider integrations, and external
  services.
- `CodeCafe.Contracts`: request and response contracts shared at API boundaries.
- `CodeCafe.Api`: HTTP endpoints, middleware, authentication, and composition.

## Module direction

The first modules are expected to be:

- Identity and access control
- Notes
- Audit
- Workspaces
- AI and MAF orchestration

Notes should expose capabilities to the AI layer over time, such as reading,
searching, summarizing, and drafting notes. The notes module should not directly
depend on MAF implementation details.

See [Backend AI and MAF Architecture](backend-ai-maf-architecture.md) for the
AI runtime boundary and milestone design.
