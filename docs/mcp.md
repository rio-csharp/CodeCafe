# MCP

## Overview

CodeCafe exposes notebooks through an MCP adapter hosted inside `CodeCafe.Server`.

Default paths:

- MCP endpoint: `/mcp`
- Protected-resource metadata: `/.well-known/oauth-protected-resource/mcp`

The endpoint is enabled by default and protected by OAuth/OIDC unless `Mcp:RequireAuthorization` is turned off.

## Quick Start

Choose the base URL for the environment you want to target:

- Local development: `https://localhost:7239`
- Example production host: `https://api.codes.cafe`

The MCP endpoint is always `{baseUrl}/mcp`.

For browser users, CodeCafe uses the normal web sign-in flow.
For MCP clients, connect to `{baseUrl}/mcp` and complete the CodeCafe OAuth flow in the browser when the client prompts for authentication.

CodeCafe does not currently expose a separate personal access token or manual bearer-token generation screen for MCP users.
The intended user-scoped flow is browser-based OAuth, with access tokens issued by the built-in authorization server during that flow.

### Claude Code

Recommended user-level install:

```bash
claude mcp add --transport http --scope user codecafe https://api.codes.cafe/mcp
```

Project-level install:

```bash
claude mcp add --transport http --scope project codecafe https://api.codes.cafe/mcp
```

If you omit `--scope`, Claude Code defaults to a local install for the current project only.

After installation:

1. Open Claude Code.
2. Run `/mcp`.
3. Start authentication for the `codecafe` server if prompted.
4. Complete the browser sign-in flow with your CodeCafe account.

After sign-in completes, Claude Code uses the OAuth-issued token for that authenticated CodeCafe user automatically.

### User-Scoped Access

User-scoped MCP access requires OAuth scopes granted to the signed-in CodeCafe account:

- `notes.read` for authenticated notebook reads and search
- `notes.write` for notebook and page mutations

After OAuth login completes, the resulting token is tied to the authenticated CodeCafe user and those granted scopes.

## Auth And Scopes

Default MCP configuration:

- Audience: `codecafe-mcp`
- Read scopes: `notes.read`
- Write scopes: `notes.write`

When authorization is enabled:

- callers authenticate through the built-in OpenIddict server
- MCP clients use bearer access tokens obtained from the CodeCafe OAuth flow
- the MCP endpoint adds `WWW-Authenticate` metadata hints on `401` responses
- optional origin allowlists are enforced for MCP requests

### Dynamic Client Registration

CodeCafe includes dynamic client registration for native loopback clients at `/connect/register`.

Registration requirements:

- `application_type` must be `native`
- `token_endpoint_auth_method` must be `none`
- redirect URIs must be HTTP loopback addresses on `localhost`, `127.0.0.1`, or `::1`
- supported grants are `authorization_code` and `refresh_token`

Example registration request:

```bash
curl -X POST https://localhost:7239/connect/register \
  -H "Content-Type: application/json" \
  -d '{
    "application_type": "native",
    "client_name": "My MCP Client",
    "grant_types": ["authorization_code", "refresh_token"],
    "response_types": ["code"],
    "token_endpoint_auth_method": "none",
    "redirect_uris": ["http://127.0.0.1/callback"]
  }'
```

CodeCafe also seeds a built-in public client:

- Client ID: `codecafe-claude`
- Display name: `Claude Code`
- Redirect URIs: `http://localhost/callback`, `http://127.0.0.1/callback`

## Tools

### Diagnostics Tools

- `diagnostics_status`

### Public Read Tools

These tools only expose public notebook data:

- `notes_list_public_notebooks`
- `notes_get_public_notebook`

### Authenticated Notebook Tools

Notebook discovery and search:

- `notes_list_notebooks`
- `notes_get_notebook`
- `notes_search`
- `notes_get_limits`
- `notes_prepare_http_upload`

Notebook mutation:

- `notes_create_notebook`
- `notes_update_notebook`
- `notes_delete_notebook`

Item and content mutation:

- `notes_list_items`
- `notes_get_page`
- `notes_create_upload`
- `notes_append_upload_chunk`
- `notes_discard_upload`
- `notes_create_folder`
- `notes_create_page`
- `notes_update_page_content`
- `notes_append_blocks_to_page`
- `notes_replace_block_at_index`
- `notes_insert_blocks_at_index`
- `notes_delete_block_at_index`
- `notes_replace_text`
- `notes_rename_item`
- `notes_move_item`
- `notes_reorder_items`
- `notes_delete_item`
- `notes_archive_item`
- `notes_restore_item`

## Resources

Available notebook resources:

- `notes://guide`
- `notebooks://mine`
- `notebooks://public`
- `notebook://{slug}`
- `notebook://{slug}/items`
- `page://{slug}/{path}`

`notes://guide` describes the recommended notebook workflow for discovery, page reads, uploads, and imports.

Path parameters on item/page tools accept the path returned by MCP responses. Resource-style `page/<path>` and `folder/<path>` inputs are also accepted for clients that derive paths from item resource URIs.

## Prompts

Notebook prompts currently exposed through MCP:

- `notes.summarize_page`
- `notes.organize_notebook`
- `notes.expand_outline`
- `notes.review_for_staleness`

These prompts are helpers layered on top of notebook reads and writes; they do not create a separate persistence path.

## Upload Workflow

For larger content, the recommended flow is:

1. Call `notes_prepare_http_upload` first.
2. Follow the returned HTTP request details for `POST /api/mcp/uploads/markdown` using the same bearer token used for MCP.
3. Pass the returned `uploadId` into `notes_create_page`, `notes_update_page_content`, or `notes_append_blocks_to_page`.
4. Optionally clean up abandoned uploads with `DELETE /api/mcp/uploads/{uploadId}` or `notes_discard_upload`.

Chunked MCP upload remains available for clients that cannot use HTTP:

1. Optionally call `notes_get_limits` to fetch the runtime chunk and size caps.
2. Start a session with `notes_create_upload`.
3. Send one or more UTF-8 chunks with `notes_append_upload_chunk`.
4. Apply the uploaded content with page-creation or page-update tools.
5. Optionally discard abandoned sessions with `notes_discard_upload`.

Supported uploaded formats:

- `markdown`
- `tiptap_json`
- `tiptap_blocks_json`

Markdown uploads are converted server-side into TipTap JSON before validation and persistence.
This Markdown support is an import convenience for MCP/API clients. It is not the target write format for the in-app AI notebook editor; AI editing should produce validated TipTap JSON or structured operations that become TipTap JSON.

The create/update/append tool descriptions expose the default page-content limits by field name: `maxInlineContentBytes`, `maxPageContentBytes`, `maxTipTapDepth`, `maxTipTapNodeCount`, and `maxTipTapTextLength`. Treat `notes_get_limits` as the runtime source of truth when configuration differs from defaults.

Upload sessions are consumed and deleted after a successful page create, content replace, or block append. `notes_discard_upload` is idempotent, so clients may still call it during cleanup; already-consumed or already-absent sessions return a successful `already_absent` result.

## Content Mutation Semantics

Use the content mutation tools this way:

| Tool | Use when | Content argument |
| --- | --- | --- |
| `notes_create_page` | Creating a new page, optionally with initial body content | `contentJson` or `contentUploadId` |
| `notes_update_page_content` | Replacing the entire stored TipTap document | `contentJson` or `contentUploadId` |
| `notes_append_blocks_to_page` | Appending block nodes to the end of an existing page | `blocks` or `blocksUploadId` |
| `notes_replace_block_at_index` | Replacing one block in an existing page by zero-based index | `index`, `block` |
| `notes_insert_blocks_at_index` | Inserting blocks into an existing page at a zero-based index | `index`, `blocks` |
| `notes_delete_block_at_index` | Deleting one block from an existing page by zero-based index | `index` |
| `notes_replace_text` | Searching and replacing plain text inside an existing page without changing block structure | `searchText`, `replacementText` |

Write tools default to lightweight responses: `contentJson` and `plainTextContent` are omitted, while `contentJsonBytes`, `plainTextLength`, `tipTapNodeCount`, and page identifiers are still returned. Pass `includeContent: true` only when the client explicitly needs the full updated document in the mutation response. Use `notes_get_page` for normal full-content reads.

## Query Semantics

- `notes_list_notebooks` supports `scope=all|mine|public`.
- `notes_search` supports `scope=all|notebooks|items` and optional `notebookSlug` narrowing.
- `notes_list_items` supports `search`, `parentPath`, `type=all|page|folder`, `includeArchived`, `offset`, and `limit`.
- `includeArchived=true` is owner-only.
- Resource-style `page/<path>` and `folder/<path>` values are accepted on path-based item/page tools for clients that derive paths from MCP resource URIs.

## Default Limits

From `server/Host/CodeCafe.Server/appsettings.json`:

- Max inline content: `131072` bytes
- Max upload chunk: `262144` bytes
- Max upload size: `4194304` bytes
- Max HTTP upload size: `4194304` bytes
- Max page content: `1048576` bytes
- Max paged list size: `500`
- Max TipTap depth per page: `64`
- Max TipTap nodes per page: `5000`
- Max TipTap text characters per page: `200000`
- Upload idle timeout: `900` seconds

Clients should treat these values as runtime configuration, not hard-coded constants.

## Operational Notes

- MCP diagnostics are exposed at `/mcp/health/live`.
- The MCP server also exposes `diagnostics_status` as a read-only tool for smoke testing and adapter diagnostics.
- Notebook reads and writes still flow through the same notebook rules used by the REST API.
- Public-only MCP tools are narrower in scope than the authenticated notebook tools, but they still live on the same MCP host surface.

## Local Development

Run the combined backend host locally:

```powershell
dotnet restore server/CodeCafe.slnx
dotnet run --project server/Host/CodeCafe.Server
```

The checked-in development profile serves:

- HTTP: `http://localhost:5042`
- HTTPS: `https://localhost:7239`

Local MCP URLs:

- Endpoint: `https://localhost:7239/mcp`
- Protected-resource metadata: `https://localhost:7239/.well-known/oauth-protected-resource/mcp`
