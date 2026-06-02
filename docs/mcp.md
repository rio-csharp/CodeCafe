# MCP

## Overview

CodeCafe exposes notebooks through an MCP adapter hosted inside `CodeCafe.Server`.

Default paths:

- MCP endpoint: `/mcp`
- Protected-resource metadata: `/.well-known/oauth-protected-resource/mcp`

The endpoint is enabled by default and protected by OAuth/OIDC bearer tokens unless `Mcp:RequireAuthorization` is turned off.

## Auth And Scopes

Default MCP configuration:

- Audience: `codecafe-mcp`
- Read scopes: `notes.read`
- Write scopes: `notes.write`

When authorization is enabled:

- callers authenticate through the built-in OpenIddict server
- the MCP endpoint adds `WWW-Authenticate` metadata hints on `401` responses
- optional origin allowlists are enforced for MCP requests

## Tools

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
- `notes_update_page_content_json`
- `notes_append_blocks_to_page`
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

## Prompts

Notebook prompts currently exposed through MCP:

- `notes.summarize_page`
- `notes.organize_notebook`
- `notes.expand_outline`
- `notes.review_for_staleness`

These prompts are helpers layered on top of notebook reads and writes; they do not create a separate persistence path.

## Upload Workflow

For larger content, the recommended flow is:

1. Call `notes_get_limits`.
2. Start a session with `notes_create_upload`.
3. Send one or more UTF-8 chunks with `notes_append_upload_chunk`.
4. Apply the uploaded content with page-creation or page-update tools.
5. Optionally discard abandoned sessions with `notes_discard_upload`.

Supported uploaded formats:

- `markdown`
- `tiptap_json`
- `tiptap_blocks_json`

Markdown uploads are converted server-side into TipTap JSON before validation and persistence.

## Default Limits

From `src/CodeCafe.Server/appsettings.json`:

- Max inline content: `131072` bytes
- Max upload chunk: `262144` bytes
- Max upload size: `4194304` bytes
- Max page content: `1048576` bytes
- Max paged list size: `500`
- Upload idle timeout: `900` seconds

Clients should treat these values as runtime configuration, not hard-coded constants.

## Operational Notes

- MCP diagnostics are exposed at `/mcp/health/live`.
- Notebook reads and writes still flow through the same notebook rules used by the REST API.
- Public-only MCP tools are narrower in scope than the authenticated notebook tools, but they still live on the same MCP host surface.
