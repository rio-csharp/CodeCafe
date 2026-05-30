# CodeCafe Notes MCP Design

## Status

- Status: Accepted design; Notes REST refactor phase 1 completed; MCP setup plan ready
- Scope: CodeCafe Notes / Notebook backend
- Primary audience: backend engineers, AI integration engineers
- Related setup plan: [mcp-server-setup-plan.md](mcp-server-setup-plan.md)
- Related code: [NotesController.cs](../src/CodeCafe.WebApi/Notes/NotesController.cs)

---

## 1. Context

CodeCafe Notes is not a flat note list. A "Note" is a Notebook that contains
folders and pages.

Current storage model:

- `Notebook` stores notebook-level metadata
- `NotebookItem` stores folder/page tree nodes
- page content is stored as TipTap JSON in PostgreSQL
- `PlainTextContent` is stored alongside page JSON for search and future indexing

We want to make Notes accessible through MCP so AI agents can read, create,
update, and organize notebook content directly.

At the same time, the current HTTP implementation has become too large:

- `NotesController` was previously over 1100 lines and mixed nearly all Notes behavior
- HTTP handling, authorization, slug generation, tree updates, content handling,
  search, and response shaping all live in one file
- this makes MCP support harder because there is no clean service layer to reuse

This document defines:

1. the MCP design for Notes
2. the explicit decision to let AI write TipTap JSON directly
3. the backend refactor plan required before or while implementing MCP

---

## 2. Decision Summary

### Accepted decisions

1. CodeCafe will expose Notes through an MCP server.
2. AI will read and write page content as **TipTap JSON**, not Markdown.
3. PostgreSQL remains the source of truth for notebooks, items, and page
   content.
4. Existing Notes authorization rules remain the foundation for MCP access.
5. `NotesController` will be decomposed into smaller application/backend
   services before it becomes the execution path for MCP operations.
6. The product MCP server should start as an ASP.NET Core Streamable HTTP
   endpoint using the official C# SDK package `ModelContextProtocol.AspNetCore`.
7. MCP write tools are blocked until TipTap validation and optimistic
   concurrency are implemented.

### Rejected for now

1. Converting AI input from Markdown to TipTap JSON as the primary MCP write
   format.
2. Letting MCP tools bypass existing business rules and write directly through
   EF entities.
3. Keeping all Notes behavior in one controller while adding MCP support on top.
4. Exposing remote MCP write tools without real user authentication, scopes, and
   audit logging.

### Implementation baseline

The setup plan is intentionally separate from this design record:

- this file defines product and domain decisions
- [mcp-server-setup-plan.md](mcp-server-setup-plan.md) defines the next
  implementation checklist

Current baseline:

- MCP protocol baseline: `2025-11-25`
- transport: Streamable HTTP for the deployed product server
- SDK: official C# SDK, `ModelContextProtocol.AspNetCore`
- endpoint: `/mcp`, gated by configuration and authentication
- delivery mode: full implementation in phases; read tools first, then resources,
  write tools, prompts, operations, and conformance hardening

---

## 3. Why We Are Choosing TipTap JSON Directly

We explicitly choose direct TipTap JSON for AI write operations.

### Reasons

1. **Storage format already exists**
   The system already stores page content as TipTap JSON in PostgreSQL.

2. **No conversion loss**
   Markdown cannot fully represent all TipTap document structure without a
   custom conversion layer and lossy edge cases.

3. **Better structural control**
   AI can create headings, lists, code blocks, blockquotes, tables, images,
   YouTube embeds, task lists, and nested structures directly as a document tree.
   Inline formatting includes bold, italic, underline, strikethrough, links,
   colors, highlights, subscript, superscript, and font family.

4. **Less hidden behavior**
   The backend does not need a Markdown parser plus a Markdown-to-TipTap
   translator to support writing.

### Tradeoffs

1. AI output becomes more verbose.
2. Invalid JSON shape is a real risk.
3. Whole-document replacement becomes easier, so conflict protection matters
   more.

### Resulting rule

For MCP writes, **`contentJson` is the primary write payload**.

The backend must not trust it blindly. It must validate, normalize, and derive
secondary fields from it.

---

## 4. Goals and Non-Goals

## Goals

1. Allow AI to discover notebooks and pages.
2. Allow AI to read TipTap JSON page content.
3. Allow AI to create folders and pages.
4. Allow AI to update or append TipTap JSON content.
5. Preserve existing notebook visibility and ownership rules.
6. Make MCP implementation reuse backend business logic rather than duplicate it.

## Non-Goals for Initial Production Release

1. Real-time collaborative editing
2. Version history UI
3. Multi-user merging
4. AI-specific rendered HTML
5. Full-text semantic retrieval pipeline
6. Markdown-first editing support

---

## 5. Existing Authorization Model

The current Notes authorization model should remain canonical.

### Read rules

- `public`: anyone can read
- `unlisted`: anyone with the link or slug can read
- `private`: only the owner can read

### Write rules

- only the notebook owner can create, update, move, reorder, or delete items
- only the notebook owner can edit notebook metadata

### MCP implication

MCP must never introduce a weaker permission path than REST.

If a user or agent could not do something through the normal product rules, MCP
 must not allow it either.

---

## 6. MCP Surface Design

We should expose Notes through **MCP tools** first. MCP resources are useful for
read-heavy scenarios, but tools are the core write path.

## 6.1 Core MCP tools

Implementation should use the namespaced tool names in
[mcp-server-setup-plan.md](mcp-server-setup-plan.md), such as `notes_search`
and `notes_get_page`. The tool sections below describe the capability and
payload shape.

### `search_notes`

Search notebooks and optionally notebook items.

Example input:

```json
{
  "query": "cookie jwt",
  "notebookSlug": "net-platform",
  "scope": "items"
}
```

Example output:

```json
{
  "results": [
    {
      "notebookSlug": "net-platform",
      "itemId": "guid",
      "path": "auth/cookie-vs-jwt",
      "title": "Cookie vs JWT",
      "type": "page",
      "plainTextSnippet": "..."
    }
  ]
}
```

### `get_notebook`

Read notebook metadata and top-level info.

Example input:

```json
{
  "slug": "net-platform"
}
```

Example output:

```json
{
  "id": "guid",
  "slug": "net-platform",
  "title": ".NET Platform",
  "visibility": "public",
  "canEdit": true,
  "itemCount": 12,
  "folderCount": 4,
  "pageCount": 8
}
```

### `list_items`

List folder/page items for a notebook.

Example input:

```json
{
  "notebookSlug": "net-platform"
}
```

### `get_page`

Read a specific page by notebook slug and path.

Example input:

```json
{
  "notebookSlug": "net-platform",
  "path": "auth/cookie-vs-jwt"
}
```

Example output:

```json
{
  "pageId": "guid",
  "notebookId": "guid",
  "notebookSlug": "net-platform",
  "path": "auth/cookie-vs-jwt",
  "title": "Cookie vs JWT",
  "contentFormat": "tiptap_json",
  "contentJson": {
    "type": "doc",
    "content": []
  },
  "plainTextContent": "..."
}
```

### `create_notebook`

Create a notebook with title, description, and visibility.

### `create_page`

Create a page or folder under a parent path or parent item id.

We should prefer notebook slug + parent path in MCP-facing schemas because they
are easier for agents to use than opaque item ids.

### `update_page_content_json`

Replace the whole page document with a new TipTap JSON document.

Example input:

```json
{
  "notebookSlug": "net-platform",
  "path": "auth/cookie-vs-jwt",
  "expectedUpdatedAtUtc": "2026-05-19T08:00:00Z",
  "contentJson": {
    "type": "doc",
    "content": [
      {
        "type": "heading",
        "attrs": { "level": 2 },
        "content": [
          { "type": "text", "text": "Cookie Flow" }
        ]
      }
    ]
  }
}
```

### `append_blocks_to_page`

Append a list of TipTap block nodes to the end of an existing page.

Example input:

```json
{
  "notebookSlug": "net-platform",
  "path": "auth/cookie-vs-jwt",
  "blocks": [
    {
      "type": "paragraph",
      "content": [
        { "type": "text", "text": "New note." }
      ]
    }
  ]
}
```

### `move_item`

Move a folder or page to a different parent.

### `reorder_items`

Batch reorder/move items.

---

## 6.2 Optional MCP resources

We may later expose read-only MCP resources such as:

- `notebook://{slug}`
- `notebook://{slug}/items`
- `page://{slug}/{path}`

These are useful when an agent wants passive context rather than imperative tool
 calls.

For the first production release, tools are enough. Full implementation should
add resource templates once read tools are stable.

---

## 7. Content Contract for AI

Since AI writes TipTap JSON directly, we need a strict contract.

## Required invariants

1. Root node must be:

```json
{ "type": "doc", "content": [...] }
```

2. `content` must be an array when present.
3. Only page items may carry `contentJson`.
4. Folder items must not persist page content fields.

## Backend responsibilities

The backend must:

1. validate JSON structure
2. normalize missing optional arrays/fields when needed
3. reject invalid documents with `400 Bad Request`
4. rebuild `PlainTextContent` from `contentJson`
5. keep `ContentFormat = "tiptap_json"` for pages

## Explicit rule

`PlainTextContent` must be **derived by the backend**, not trusted from MCP
input.

This protects search quality and avoids drift between stored JSON and stored
plain text.

Current implementation note:

- REST page writes still accept `plainTextContent` as a fallback when the
  extractor returns no text.
- Before MCP write tools are enabled, page writes should move to a single
  content service where `contentJson` is validated and `PlainTextContent` is
  always derived server-side.

---

## 8. Concurrency and Conflict Handling

AI-driven updates are more likely to replace full documents, so conflict
protection matters.

### Recommendation

Every page write tool should support one of:

- `expectedUpdatedAtUtc`
- `revision`

If the provided expected value does not match current persisted state, the
backend should reject the write with `409 Conflict`.

### Why

Without this, an agent can overwrite:

- a recent manual user edit
- another AI write
- a second tab's pending save

The first implementation can start with `expectedUpdatedAtUtc`, since that
matches the current model more naturally. A dedicated integer `Revision` should
be considered before high-volume or collaborative agent writes.

---

## 9. Search Behavior

Notes search should remain user-friendly and case-insensitive.

Current backend behavior:

- PostgreSQL: use `ILIKE`
- non-PostgreSQL test providers: use a lowercased `LIKE` fallback

For MCP, search tools should reuse the same behavior as product APIs.

---

## 10. Refactor Requirement Before MCP Expansion

The current Notes implementation is too concentrated in one controller.

### Problem

`NotesController` currently owns:

- authorization checks
- notebook CRUD
- item CRUD
- slug generation
- path generation
- tree movement logic
- reorder logic
- favorite logic
- search logic
- metadata aggregation
- response shaping

This violates our own backend best practice of keeping controllers thin and
splitting large service classes by cohesive behavior.

### Decision

Before MCP grows beyond a thin spike, we should refactor Notes into dedicated
backend services.

### Current state

This refactor has now started and the first phase is in place:

- `NotesController` has been reduced to HTTP orchestration
- notebook reads moved into `INotebookQueryService`
- notebook and item writes moved into `INotebookCommandService`
- favorites moved into `INotebookFavoriteService`
- TipTap plain-text derivation introduced via `ITipTapPlainTextExtractor`

---

## 11. Proposed Refactor Target Shape

We do not need CQRS or MediatR for this. We do need separation by
responsibility.

## Suggested structure

```text
src/CodeCafe.Application/
  Notes/
    INotebookQueryService.cs
    INotebookCommandService.cs
    INotebookFavoriteService.cs
    ITipTapPlainTextExtractor.cs
    NotesModels.cs
    NotesResults.cs

src/CodeCafe.Infrastructure/
  Notes/
    NotebookQueryService.cs
    NotebookCommandService.cs
    NotebookFavoriteService.cs
    TipTapPlainTextExtractor.cs
    NotesSupport.cs

src/CodeCafe.WebApi/
  Notes/
    NotesController.cs
    NoteDtos.cs
```

### Minimum service split

#### `INotebookQueryService`

Responsibilities:

- get notebook by id
- get notebook by slug
- list public notebooks
- list my notebooks
- shape metadata for summary/detail views

#### `INotebookCommandService`

Responsibilities:

- create notebook
- update notebook metadata
- delete notebook
- create/update/delete/reorder notebook items
- regenerate slug
- save with unique-slug retry behavior

#### `ITipTapPlainTextExtractor`

Responsibilities:

- derive `PlainTextContent`

### Remaining extraction opportunities

The current shape is already a large improvement over controller-owned logic,
but it is not the final target. Over time we may still split out:

- item-specific command service
- TipTap validation/content service
- dedicated search service if search behavior grows

#### `INotebookFavoriteService`

Responsibilities:

- get favorite status
- add favorite
- remove favorite
- favorite count aggregation

#### `INotesSearchService`

Responsibilities:

- notebook search
- item search
- provider-aware case-insensitive query building

---

## 12. Refactor Boundaries

### Controller should keep

- route definitions
- request/response DTO binding
- HTTP status mapping
- auth attributes

### Controller should lose

- slug generation internals
- path generation internals
- EF-heavy aggregation code
- duplicate permission branching
- TipTap validation/normalization
- save retry logic

The controller should become orchestration-only.

---

## 13. MCP Adapter Architecture

We should not implement MCP by calling controllers.

### Correct layering

```text
MCP server
    -> Notes application/backend services
        -> Infrastructure / EF Core
```

### Avoid

```text
MCP server
    -> HTTP call to our own controller
```

Internal self-HTTP would add:

- duplicated serialization
- weaker typing
- harder testing
- awkward auth propagation

MCP and REST should share the same service layer, not call each other.

---

## 14. Authentication and Authorization for MCP

MCP should authenticate as a real CodeCafe user or as an explicitly delegated
agent identity tied to a CodeCafe user.

For the product HTTP endpoint, prefer bearer-token or OAuth-compatible
authentication. Browser cookie auth plus CSRF is the right model for the React
app, but it should not become the long-term MCP client authentication model.

Current CodeCafe gap:

- the API currently has cookie-based Identity auth for the React app
- deployed MCP needs a bearer-token/OAuth path before it is enabled outside
  local development
- the MCP token must identify both the actor and the token audience/resource

MCP should be treated as an OAuth protected resource:

- clients send `Authorization: Bearer <token>` on every `/mcp` request
- the server validates issuer, expiry, signature or introspection result,
  audience/resource, and scopes
- tokens issued for another API are rejected
- tokens are not accepted in query strings
- received MCP tokens are not passed through to downstream services
- OAuth protected resource metadata should advertise the authorization server
  and supported scopes once the endpoint is public

### Suggested scopes

- `notes.read`
- `notes.write`

### Scope and permission matrix

| Operation | Scope | Permission rule |
|-----------|-------|-----------------|
| Search notebooks/items | `notes.read` | actor can only see readable notebooks/items |
| Read notebook metadata | `notes.read` | existing public, unlisted, private owner rules |
| List notebook items | `notes.read` | existing public, unlisted, private owner rules |
| Read page content | `notes.read` | existing read rules and item must be a page |
| Create page/folder | `notes.write` | notebook owner only |
| Update page content | `notes.write` | notebook owner only plus concurrency check |
| Append page blocks | `notes.write` | notebook owner only plus conflict protection |
| Move/reorder/delete items | `notes.write` | notebook owner only |

### Rules

- deployed MCP should require an authenticated actor for all tools, including
  reads; anonymous public reads remain available through REST
- read tools enforce current read rules
- write tools enforce owner-only write rules
- scopes do not bypass notebook ownership
- missing or invalid HTTP credentials fail before tool execution
- insufficient scopes fail as authorization errors, not as successful tool
  results
- agent identities must resolve to a CodeCafe user id or delegated actor tied to
  a CodeCafe user id before Notes services are called

### Exposure rule

Do not enable `Mcp:Enabled` in production until bearer-token validation,
audience/resource validation, protected resource metadata, host/origin controls,
rate limiting, and audit logging are configured.

### Audit recommendation

Every MCP write should record:

- actor user id
- actor type: `user` or `agent`
- operation
- notebook id
- item id if applicable
- timestamp

This can begin as structured logging, but the full implementation should add a
queryable audit trail before broad write-tool exposure.

---

## 15. Validation Rules for MCP Writes

For page writes:

1. notebook must exist
2. page must exist for update operations
3. actor must own the notebook
4. `contentJson` must be valid TipTap doc JSON
5. `PlainTextContent` must be re-derived
6. `UpdatedAtUtc` must advance
7. optimistic concurrency token must match when provided

For item create/move:

1. parent must exist in same notebook
2. parent must be a folder
3. move must not create a cycle
4. `path` must remain unique in notebook

---

## 16. API and MCP Contract Relationship

REST and MCP should not diverge in semantics.

### Same rules

- slug generation
- item path generation
- visibility handling
- favorite permissions
- search behavior

### Possible differences

- REST can continue exposing DTOs optimized for frontend
- MCP can expose agent-optimized schemas using `notebookSlug + path`

That is acceptable as long as the underlying business behavior is shared.

---

## 17. Suggested Implementation Phases

## Phase 1: Notes backend refactor

1. Extract notebook query service
2. Extract notebook command service
3. Extract notebook favorite service
4. Extract TipTap plain-text extraction service
5. Slim down `NotesController`
6. Keep behavior unchanged

Success condition:

- controller becomes thin
- tests remain green
- no MCP server yet

Status: done

## Phase 2: TipTap-safe content service

1. implement TipTap JSON validator
2. implement plain text extractor
3. route REST page writes through a single content service
4. stop trusting client-supplied `plainTextContent` as a persistence fallback
5. add conflict handling hooks

Success condition:

- all page content writes use one canonical path
- invalid TipTap documents are rejected before persistence
- page write responses have reliable `UpdatedAtUtc` values for future
  concurrency checks

## Phase 3: MCP server skeleton and read tools

1. add `ModelContextProtocol.AspNetCore`
2. add `McpOptions` and keep the endpoint disabled by default
3. map Streamable HTTP at `/mcp`
4. apply host/origin, auth, and rate-limit boundaries
5. add read tools:
   - `notes_get_notebook`
   - `notes_list_items`
   - `notes_get_page`
   - `notes_search`

Success condition:

- agents can inspect notebooks safely
- MCP endpoint is not publicly writable
- read tools reuse Notes query services

## Phase 4: MCP write tools

1. `notes_create_page`
2. `notes_update_page_content_json`
3. `notes_append_blocks_to_page`
4. `notes_move_item`
5. `notes_reorder_items`
6. archive/restore/delete only after recoverability is available

Success condition:

- agents can draft and maintain notes
- write tools require `notes.write`, owner permissions, validated TipTap JSON,
  and optimistic concurrency

## Phase 5: MCP resources

1. add `notebook://{slug}`
2. add `notebook://{slug}/items`
3. add `page://{slug}/{path}`
4. include modification annotations where supported

Success condition:

- clients can attach notebook/page context without invoking write tools
- resources enforce the same `notes.read` and visibility rules

## Phase 6: MCP prompts and workflows

1. add prompts for summarizing pages
2. add prompts for organizing notebooks
3. add prompts for expanding outlines
4. add prompts for reviewing stale pages

Success condition:

- prompts guide agent workflows without embedding secrets or bypassing tools
- prompts lead to reviewable plans before write tools are invoked

## Phase 7: Operations and conformance

1. add dashboards/alerts for tool calls, failures, conflicts, and auth failures
2. add queryable audit trail for write tools
3. document client setup and troubleshooting
4. add SDK-client smoke tests and MCP Inspector/manual verification checklist
5. add fuzz and authorization regression tests

Success condition:

- MCP can be operated, audited, tested, and disabled independently from the rest
  of the API

---

## 18. Risks

### Risk: invalid TipTap documents

Mitigation:

- backend validator
- targeted tests for malformed structures

### Risk: user edits overwritten by agents

Mitigation:

- optimistic concurrency input
- conflict response

### Risk: Notes rules duplicated between REST and MCP

Mitigation:

- shared services
- no self-HTTP

### Risk: further controller growth before refactor

Mitigation:

- do not add new Notes logic directly into `NotesController`
- all new work goes into extracted services first

---

## 19. Immediate Next Steps

Use [mcp-server-setup-plan.md](mcp-server-setup-plan.md) as the implementation
checklist.

Completed:

1. notebook create/update/delete
2. notebook slug generation and unique-slug save retry
3. notebook summary/detail shaping
4. item tree operations
5. favorite operations

Next:

1. add the disabled-by-default MCP endpoint with `ModelContextProtocol.AspNetCore`
2. wire configuration, auth, host/origin, and rate-limit boundaries
3. implement read tools on `INotebookQueryService`
4. add TipTap JSON validation
5. make page writes derive `PlainTextContent` server-side
6. add optimistic concurrency before any MCP write tool
7. implement write tools only after the blockers above are complete

---

## 20. Final Recommendation

The right path for CodeCafe is:

1. keep TipTap JSON as the MCP write format
2. add strict backend validation and plain-text derivation
3. refactor Notes into cohesive services
4. expose MCP through the official C# SDK over Streamable HTTP
5. build MCP on top of those services, not on top of controllers

This keeps the current Notes model intact while making the backend clean enough
to support AI-native workflows without turning Notes into a maintenance trap.
