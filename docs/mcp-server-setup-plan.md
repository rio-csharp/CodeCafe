# CodeCafe MCP Server Full Implementation Plan

## Status

- Status: Full implementation plan ready
- Last reviewed: 2026-05-21
- Scope: CodeCafe Notes MCP server
- Related design: [notes-mcp-design.md](notes-mcp-design.md)
- Protocol baseline: MCP specification `2025-11-25`
- .NET package baseline: official C# SDK, `ModelContextProtocol.AspNetCore`

---

## 1. Setup Decision

Build the first MCP server inside `CodeCafe.WebApi` and expose it over
Streamable HTTP at a single MCP endpoint, preferably `/mcp`.

Why this shape:

- The API host already owns dependency injection, logging, configuration,
  database access, auth, rate limiting, and deployment wiring.
- The official C# SDK has an ASP.NET Core package for HTTP MCP servers.
- The Notes services have already moved behind application/infrastructure
  interfaces, so MCP tools can reuse behavior without calling controllers.
- A separate stdio process would make auth, DI, deployment, and observability
  harder for the product server.

Use a separate stdio server only later if we want a local developer-only tool
that is not part of the deployed product API.

---

## 2. Transport

Use Streamable HTTP for the product server.

Required operational rules:

- The MCP endpoint is a single HTTP endpoint that handles JSON-RPC requests.
- The server must validate host/origin behavior for browser-reachable traffic.
- Local development should bind only to loopback if the endpoint is run outside
  the normal API host.
- Do not expose the endpoint publicly without authentication.
- Prefer stateless HTTP mode for the first production release because the Notes tools do not need
  server-to-client requests such as sampling or elicitation.

Recommended SDK shape:

```csharp
builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .WithToolsFromAssembly();
```

Then map the endpoint in the API pipeline after auth/rate limiting middleware.
Confirm the exact `MapMcp` overload against the installed SDK version before
implementation.

---

## 3. Authentication and Exposure

Do not use browser cookie auth as the long-term MCP authentication model.
MCP clients should authenticate through bearer tokens or an OAuth-compatible
resource-server path.

CodeCafe currently has cookie-based Identity auth for the React app. That is not
enough for a deployed MCP endpoint because most MCP clients are not browsers,
and cookie auth would blur CSRF and token handling concerns. Treat the MCP
server as a protected resource that receives bearer tokens issued for the MCP
resource.

Production auth requirements:

- Every HTTP request to `/mcp` must include `Authorization: Bearer <token>`.
- Access tokens must not be accepted from query strings.
- Tokens must be audience-bound to the CodeCafe MCP resource, such as the
  canonical `/mcp` URL or configured resource identifier.
- The API must reject tokens issued for another API or upstream service.
- The MCP server must not pass received access tokens through to downstream
  services.
- `WWW-Authenticate` challenges should include the required scope and protected
  resource metadata URL once OAuth metadata is available.
- Serve OAuth Protected Resource Metadata for MCP clients before public
  exposure.

Exposure sequence:

1. Add the endpoint disabled by default with `Mcp:Enabled = false`.
2. Allow local development only while the first protocol wiring is tested. Keep
   it loopback-only or behind developer-only network access.
3. Enable authenticated read tools only after a real user identity is available from
   claims.
4. Keep write tools disabled until write scopes, audit logging, TipTap
   validation, and optimistic concurrency are in place.

Deployment gate:

- `Mcp:Enabled = true` in production requires bearer-token validation,
  resource/audience validation, protected resource metadata, configured allowed
  origins/hosts, and rate limiting.
- If any of those are missing, fail startup instead of exposing a partially
  protected MCP endpoint.

Suggested scopes:

- `notes.read`
- `notes.write`

Tool scope matrix:

| Tool | Scope | Additional authorization |
|------|-------|--------------------------|
| `notes_search` | `notes.read` | Return only notebooks/items readable by actor |
| `notes_get_notebook` | `notes.read` | Existing visibility and owner rules |
| `notes_list_items` | `notes.read` | Existing visibility and owner rules |
| `notes_get_page` | `notes.read` | Existing visibility and owner rules; item must be a page |
| `notes_create_page` | `notes.write` | Actor must own notebook |
| `notes_update_page_content_json` | `notes.write` | Actor must own notebook; concurrency token must match |
| `notes_append_blocks_to_page` | `notes.write` | Actor must own notebook; concurrency token should match |
| `notes_move_item` | `notes.write` | Actor must own notebook |
| `notes_reorder_items` | `notes.write` | Actor must own notebook |
| `notes_delete_item` | `notes.write` | Actor must own notebook; require soft-delete/versioning first |

Authorization rules:

- Deployed MCP should require an authenticated actor for all tools, including
  reads. Public REST reads can remain anonymous.
- Public and unlisted notebook visibility still matters after authentication:
  authenticated actors may read what REST would allow them to read.
- Private reads require the notebook owner.
- Writes require the notebook owner and `notes.write`.
- Scopes never bypass ownership.
- Anonymous MCP writes are not allowed.
- Agent identities must resolve to a CodeCafe user id or a delegated actor tied
  to a CodeCafe user id before service methods are called.

HTTP auth failures should map cleanly:

| Case | Response |
|------|----------|
| Missing or invalid access token | `401 Unauthorized` |
| Valid token without required scope | `403 Forbidden` |
| Valid token without notebook permission | MCP tool execution error with `forbidden` code |

Implementation options to decide before production:

| Option | Fit | Notes |
|--------|-----|-------|
| External OIDC/OAuth provider | Best production path | API validates JWTs; provider issues `notes.read` / `notes.write` scopes and MCP audience |
| CodeCafe-issued personal access tokens | Possible interim bridge | Simpler client setup, but requires secure token storage, rotation, revocation, hashing, and audit UI |
| Cookie auth | Browser app only | Do not use as deployed MCP auth |

Recommended path: start with external OIDC/OAuth bearer validation if an IdP is
available. If not, build personal access tokens as an explicit interim product
feature rather than weakening cookie/CSRF rules.

---

## 4. Project Shape

Keep MCP adapter code at the Web API boundary:

```text
src/CodeCafe.WebApi/
  Mcp/
    McpOptions.cs
    NotesMcpTools.cs
    NotesMcpSchemas.cs
    NotesMcpResultMapper.cs
```

Keep business behavior in application/infrastructure services:

```text
src/CodeCafe.Application/
  Notes/
    INotebookQueryService.cs
    INotebookCommandService.cs
    INotebookFavoriteService.cs
    ITipTapContentValidator.cs        # add before write tools

src/CodeCafe.Infrastructure/
  Notes/
    TipTapContentValidator.cs         # add before write tools
```

Do not put EF Core queries, slug generation, authorization branching, or TipTap
normalization inside MCP tool methods.

---

## 5. Full Capability Roadmap

The goal is full MCP support, delivered in controlled phases. Each phase should
be complete enough to ship without weakening security.

| Phase | Capability | Exit criteria |
|-------|------------|---------------|
| 0 | Protocol/auth foundation | `/mcp` endpoint, bearer auth, audience validation, protected resource metadata, startup gates, rate limiting |
| 1 | Shared Notes safety | TipTap validator, content service, server-derived plain text, optimistic concurrency, audit logging |
| 2 | Read tools | Search, notebook metadata, item tree, page read, stable structured outputs |
| 3 | Read resources | `notebook://` and `page://` resource templates for passive context |
| 4 | Write tools | Create/update/append/move/reorder with ownership, scopes, validation, and conflict handling |
| 5 | Destructive/versioned operations | Archive/delete only after version history or recoverability exists |
| 6 | Prompts and agent workflows | Curated prompts for notebook maintenance, summarization, and organization |
| 7 | Production operations | Dashboards, audit review, alerts, client docs, deployment values, rollback guidance |
| 8 | Conformance and hardening | MCP inspector/client smoke tests, fuzzed tool inputs, abuse/rate tests, security review |

---

## 6. Tool Naming and Contracts

Use namespaced tool names to avoid collisions:

| Tool | Phase | Purpose |
|------|-----------|---------|
| `notes_search` | Read | Search visible notebooks/items |
| `notes_get_notebook` | Read | Read notebook metadata by slug |
| `notes_list_items` | Read | List folder/page tree items |
| `notes_get_page` | Read | Read one page by notebook slug and path |
| `notes_create_page` | Write | Create page under parent path/id |
| `notes_update_page_content_json` | Write | Replace page TipTap document |
| `notes_append_blocks_to_page` | Write | Append TipTap block nodes |
| `notes_move_item` | Write | Move page/folder |
| `notes_reorder_items` | Write | Batch reorder tree items |
| `notes_archive_item` | Versioned write | Hide/remove item without immediate hard delete |
| `notes_restore_item` | Versioned write | Restore archived item |
| `notes_delete_item` | Destructive write | Hard delete only after explicit product approval and recoverability |

Tool schema rules:

- Keep inputs explicit and narrow.
- Prefer `notebookSlug` plus `path` for agent-facing reads/writes.
- Include `expectedUpdatedAtUtc` on full-document write tools.
- Do not accept `plainTextContent` in MCP write inputs.
- Return structured data as the primary result.
- Include a concise text content block when useful for clients that still rely
  on textual tool output.

Tool execution errors should be machine-readable:

```json
{
  "isError": true,
  "structuredContent": {
    "code": "content_conflict",
    "message": "The page changed after the expected timestamp.",
    "retryable": true
  }
}
```

Use protocol errors for malformed MCP/JSON-RPC requests. Use tool execution
errors for validation failures, permission failures, conflicts, and business
rules that the model can potentially correct.

---

## 7. Resources

Use MCP resources after read tools are stable. Resources are for passive context
selection; tools remain the write path.

Resource templates:

| Resource | Purpose |
|----------|---------|
| `notebook://{slug}` | Notebook metadata and summary |
| `notebook://{slug}/items` | Folder/page tree |
| `page://{slug}/{path}` | Page TipTap JSON and derived plain text |

Resource rules:

- Resources require `notes.read`.
- Resources enforce the same read rules as tools and REST.
- Resource content should include `lastModified` annotations where supported.
- Resources should not expose unrelated notebook data just because the actor can
  access one page.
- Start without subscriptions. Add subscriptions only if clients need change
  notifications and the server can support them reliably.

---

## 8. Prompts

Prompts are optional for protocol correctness, but useful for a complete CodeCafe
experience. They should guide client behavior without bypassing tool security.

Suggested prompts:

| Prompt | Purpose |
|--------|---------|
| `notes.summarize_page` | Summarize the current page and suggest headings |
| `notes.organize_notebook` | Propose folder/page reorganization without applying changes |
| `notes.expand_outline` | Draft sections from an existing outline |
| `notes.review_for_staleness` | Identify pages that look outdated based on metadata and content |

Prompt rules:

- Prompts never include secrets or hidden data.
- Prompts should reference tools/resources by name instead of embedding large
  hidden instructions.
- Prompts that lead to writes should ask the client/user to review planned
  changes before invoking write tools.

---

## 9. Read Tools

Implement read tools before writes.

Read tool acceptance criteria:

- `notes_get_notebook` returns metadata for a readable notebook.
- `notes_list_items` returns tree items without leaking private notebooks.
- `notes_get_page` returns TipTap JSON only for page items.
- `notes_search` reuses the same case-insensitive search behavior as REST.
- Tool results include `canEdit`, `updatedAtUtc`, and stable item identifiers
  where available.
- Unauthorized private reads do not leak whether hidden content exists beyond
  the existing REST behavior.
- Large result sets are paginated or bounded.

---

## 10. Write Tool Prerequisites

Do not implement MCP write tools until these are done:

1. Add a TipTap JSON validator with tests.
2. Make backend page writes derive `PlainTextContent` from `contentJson`; do not
   trust client-supplied plain text.
3. Add optimistic concurrency to page writes with `expectedUpdatedAtUtc` or a
   dedicated revision field.
4. Add write audit logging for actor id, actor type, operation, notebook id,
   item id, result, and timestamp.
5. Add rate limiting policy for MCP write operations.
6. Decide how `notes.write` bearer/OAuth scopes are issued and validated.
7. Add recoverability strategy for destructive operations: version history,
   archive/restore, or explicit hard-delete policy.

Write tool acceptance criteria:

- Tool input schemas reject unknown or malformed fields where possible.
- Writes are idempotent where practical or return enough information for safe
  retry after transport failure.
- Conflict errors include current `updatedAtUtc` or revision so the model can
  re-read and retry safely.
- Batch operations are atomic within a notebook.
- The audit trail distinguishes user-triggered writes from delegated agent
  writes.

---

## 11. Implementation Checklist

1. Add `ModelContextProtocol.AspNetCore` to `CodeCafe.WebApi`.
2. Add `McpOptions` with at least:
   - `Enabled`
   - `EndpointPath`
   - `AllowedOrigins`
   - `RequireAuthorization`
   - `RequiredAudience`
   - `AuthorizationServers`
3. Add bearer-token validation for the MCP endpoint.
4. Add protected resource metadata for the MCP resource.
5. Add startup validation that blocks production exposure without auth,
   audience validation, allowed origins/hosts, and rate limiting.
6. Register MCP services only when `Mcp:Enabled` is true.
7. Map the MCP endpoint after the existing security middleware.
8. Add a temporary internal `notes.ping` or `server.info` tool only if needed to
   verify protocol wiring, then remove it before production exposure.
9. Implement `NotesMcpTools` read methods that call `INotebookQueryService`.
10. Map `NotesResult` failures to MCP tool execution errors.
11. Add tests around tool schema/result mapping and authorization behavior.
12. Document local client configuration after the endpoint works.

---

## 12. Testing and Conformance

Minimum test layers once the SDK is installed:

- Unit tests for schema/result mappers.
- Unit tests for TipTap validation before write tools.
- Application/infrastructure tests for query/write service behavior.
- Web API integration tests for `/mcp` auth, audience validation, insufficient
  scope handling, origin behavior, and basic tool invocation.
- Startup validation tests proving production cannot expose MCP with auth or
  audience validation disabled.
- MCP client smoke tests that list tools/resources/prompts and call representative
  read/write operations against a test database.
- Fuzz tests for tool JSON schemas, TipTap validator, and path/slug inputs.
- Authorization regression tests for public, unlisted, private, owner, non-owner,
  missing scope, and wrong audience cases.

Use real SDK client integration tests where they add confidence, but keep most
business behavior tests below the transport layer.

---

## 13. Operations

Production MCP rollout needs operational controls, not only code.

Required:

- Helm values for `Mcp:Enabled`, endpoint path, allowed origins, resource
  identifier, and authorization server metadata.
- Separate rate-limit policy for read and write tools.
- Structured logs for every tool call with actor, scopes, tool name, target ids,
  duration, result code, and correlation id.
- Metrics for request count, error count, conflict count, validation failures,
  auth failures, rate-limit rejections, and write latency.
- Alert on repeated auth failures, write spikes, validator failures, and
  unexpected 5xx responses.
- Documentation for client setup, required scopes, local development, and
  production troubleshooting.
- Rollback plan that disables MCP independently from the rest of the API.

Nice later:

- Audit review UI.
- Per-user token management UI if CodeCafe-issued tokens are used.
- Admin kill switch by user, token, tool, or notebook.
- Usage quotas by actor/client.

---

## 14. References

- MCP SDK list: https://modelcontextprotocol.io/docs/sdk
- MCP Streamable HTTP transport: https://modelcontextprotocol.io/specification/2025-11-25/basic/transports
- MCP authorization: https://modelcontextprotocol.io/specification/2025-11-25/basic/authorization
- MCP tools: https://modelcontextprotocol.io/specification/2025-11-25/server/tools
- MCP resources: https://modelcontextprotocol.io/specification/2025-11-25/server/resources
- MCP prompts: https://modelcontextprotocol.io/specification/2025-11-25/server/prompts
- MCP C# SDK getting started: https://csharp.sdk.modelcontextprotocol.io/concepts/getting-started.html
