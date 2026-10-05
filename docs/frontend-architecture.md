# Frontend Architecture

> **Status: proposal, not yet agreed.** Open questions are collected in §9.
> Nothing here is enforced until the decisions land, at which point this file should
> be edited down to the rules the code actually follows.

The client is `clients/web`. It is a React SPA that speaks REST to `server/`.

---

## 1. What changed since 3.x

The 3.x client (`D:\Development\CodeCafe\clients\web`) is the closest precedent, and
its layering is worth keeping. Its **transport and session layers are not** — the 4.0
backend replaced them wholesale. Copying `shared/api/client.ts` verbatim would break.

| Concern | 3.x | 4.0 | Consequence for the client |
|---|---|---|---|
| Session credential | Session cookie + CSRF token | Bearer JWT + refresh token, both in the **response body** | `credentials: 'include'`, `X-CSRF-TOKEN`, `fetchCsrfToken()`, `clearCsrfToken()`, and the `invalid_csrf_token` retry all disappear |
| CORS | Cookie-capable | `WithOrigins(...).AllowAnyHeader().AllowAnyMethod()` — **no `AllowCredentials`** | Cookies are not an option without a backend change |
| Success body | Bare resource | `Result<T>` envelope: `{ value, error, isSuccess }` | Every call needs unwrapping; the 3.x client returned the bare body |
| Error body | ProblemDetails (`title`/`detail`) | `{ error: { code, message, kind } }` | `ApiError` reads a different shape; `kind` is a closed enum |
| Domain | Notebook + NotebookItem (folder \| page) | Notebook → Page (path-addressed) → Block → Revision | `entities/notebook-item` becomes `entities/page` + `entities/block` + `entities/revision` |
| Notebook tree | Assembled client-side from items | Served by `GET /notebooks/{id}/tree` | Tree building moves out of the client |
| Trash | Per-notebook archive flag | First-class `/api/trash` + `/api/notebooks/{id}/trash` | New slice, not a flag on a card |
| AI | `@ag-ui/client` | `POST /notebooks/{id}/ai/chat` → SSE `AiChatEvent` | AG-UI dropped; small SSE parser written in-house |
| Shared access | `visibility` (private/unlisted/public) | Plus `X-CodeCafe-Access-Code` header | Access code is a credential and travels in a header, never a query string |

`ResultStatusCodeFilter` maps `ErrorKind` → status: Validation 400, Unauthorized 401,
Forbidden 403, NotFound 404, Conflict 409, RateLimited 429, everything else 500.
`NotFound` doubles as "you may not see this" (see `docs/principles.md` §4), so a 404
never proves absence.

---

## 2. Layering

Keep **Feature-Sliced Design** from 3.x. It is already proven in this codebase, the
rules are documented, and it maps cleanly onto the 4.0 API surface.

```
app → pages → widgets → features → entities → shared
```

Import only from lower layers. Every slice exposes a public API through its `index.ts`;
outside code must not deep-import into a slice.

Additions the 3.x doc did not have:

- The layer rule is currently a convention held up by review. It should be a **lint
  rule** so violations fail CI rather than review.
- `shared/` is the only layer allowed to own a module-level mutable singleton
  (the access token, the in-flight refresh promise). See §5.

---

## 3. Slices mapped onto the 4.0 API

`entities/` owns types and pure logic, no UI.

| Entity | Owns | Backing endpoints |
|---|---|---|
| `user` | `AuthUserDto` | `GET/PATCH /auth/me` |
| `notebook` | Notebook details, summary, tree, visibility, sharing, tags | `/notebooks`, `/notebooks/{id}/tree`, `.../shares`, `.../tags` |
| `page` | Page details, path addressing, favorites | `/pages/{id}`, `/notebooks/{id}/pages/by-path`, `/pages/favorites` |
| `block` | Block content, ordering, batch ops | `/pages/{id}/blocks` |
| `revision` | Page and block revision history | `/pages/{id}/revisions`, `.../blocks/{blockId}/revisions` |
| `trash` | Trashed page/notebook entries | `/trash`, `/notebooks/{id}/trash` |

`features/` are user-triggered use cases, one per slice, deletable in isolation.

Starting set (mirrors the 3.x naming style): `authenticate`, `create-notebook`,
`edit-notebook`, `delete-notebook`, `manage-notebook-pages`, `edit-page`,
`toggle-favorite`, `share-notebook`, `search-notebooks`, `manage-trash`,
`import-markdown`, `export-notebook`, `ai-assistant`.

There is no `notebook-item` entity. The 3.x tree/folder model does not exist in 4.0:
pages carry a `path` and the server hands back a ready-made tree.

---

## 4. The API layer

`shared/api/client.ts` is the only place allowed to call `fetch` for JSON.

```ts
// success
{ "value": T, "error": null, "isSuccess": true }
// failure (4xx/5xx)
{ "value": null, "error": { "code": "...", "message": "...", "kind": "Validation" }, "isSuccess": false }
```

`apiFetch<T>` unwraps the envelope and returns `T`. Feature code must never see
`isSuccess`. On failure it throws `ApiError`:

```ts
class ApiError extends Error {
  status: number
  code: string
  kind: ErrorKind  // 'Validation' | 'Unauthorized' | ... | 'Unexpected'
}
```

`kind` drives behaviour (`Unauthorized` → refresh then login; `Forbidden` → no
permission, do not retry), `code` drives copy, `message` is a fallback. Because the
body is JSON on every path, `code` no longer needs the 3.x `code ?? title` guess.

### Escape hatches

Two endpoint families do **not** use the envelope and need separate helpers:

- `GET /notebooks/{id}/export` and `GET /pages/{id}/export` return raw `text/markdown`
  with `Content-Disposition`. Needs a blob/text fetch that still handles the JSON error
  envelope on failure.
- `POST /notebooks/{id}/ai/chat` returns `text/event-stream`. See §6.

### Cross-cutting request behaviour

- `Authorization: Bearer <accessToken>` when a session exists.
- `X-CodeCafe-Access-Code: <code>` when the current notebook requires one.
- 30s timeout via `AbortSignal.timeout`, combined with the caller's signal
  (`AbortSignal.any`) — carried over from 3.x.
- Four response shapes, not five: `204`/empty body → `undefined`.

### Pagination

Lists return `PagedResult<T>` → `{ items, page, pageSize, totalCount, hasNextPage }`;
fixed-sort feeds return `CursorPage<T>` → `{ items, nextCursor }`. One shared helper per
shape so query keys, `placeholderData`, and "load more" behave the same everywhere.

---

## 5. Session and token handling

`login` / `register` / `refresh` all return the same `AuthSessionDto`:

```ts
{ user, accessToken, accessTokenExpiresAtUtc, refreshToken }
```

Access tokens live **15 minutes**; refresh tokens live **30 days** and **rotate on every
refresh** (`RefreshTokenCommandHandler` consumes the old row and issues a replacement in
one transaction). `logout` takes the refresh token in the body; it is `[AllowAnonymous]`
because the refresh token *is* the credential.

### Storage

- **Access token: memory only.** Never `localStorage`. It is short-lived and is the
  token that actually authorizes requests.
- **Refresh token: `localStorage`.** Forced by the current contract — the token arrives
  in the body and CORS forbids cookies, so there is nowhere else that survives a reload.

**This is a security regression from 3.x**, where the session credential was an httpOnly
cookie that script could not read. A `localStorage` refresh token is XSS-readable, and
one stolen token mints access tokens for 30 days. Since 4.0 is being built now, the
cheap fix is a backend change: return the refresh token as an httpOnly `Secure`
`SameSite=Strict` cookie on `/auth/login|register|refresh`, add `AllowCredentials`, and
narrow the origin list. Recommended before the client is built on top of the weaker
option — see §9.

### Rotation forces single-flight refresh

Because every refresh revokes the token it consumed, two concurrent 401s that each
trigger their own refresh will revoke each other's replacement and log the user out.
Both of these are mandatory, not nice-to-have:

- **Single-flight**: `refreshPromise` is a module-level singleton in `shared/api`. The
  second caller awaits the first caller's promise.
- **Queue and replay**: requests that 401 while a refresh is in flight wait for it, then
  retry once with the new access token. A retry that 401s again is a real
  logout — surface it, do not loop.

Refresh proactively at `accessTokenExpiresAtUtc - 60s` so the common path never 401s;
keep the reactive path as a safety net for sleep/wake and clock drift.

### Anonymous reads

Auth is **not** a hard gate. `[AllowAnonymous]` covers notebook details, tree, export,
page-by-path, and search. Every read must therefore work with no session, and
`ProtectedRoute` applies only to mutations, `dashboard`, and `trash`.

Consequently the client must distinguish:

- `401` → token missing/expired → refresh, else redirect to login preserving the target.
- `403` → authenticated but not permitted → app-level "no access" state.
- `404` on an anonymous read → could be private rather than absent; do not render
  "does not exist".

---

## 6. AI streaming

`EventSource` cannot be used: the endpoint is POST, it needs an `Authorization` header,
and it needs a JSON request body. Implement a small SSE reader over
`fetch` + `response.body.getReader()`:

- reuse the same auth/refresh path as `apiFetch` (§5),
- parse `data:` frames into the `AiChatEvent` discriminated union,
- support `AbortController` so the user can stop generation.

This replaces 3.x's `@ag-ui/client` dependency. The 4.0 contract is a custom union of
five event types, so the AG-UI client buys nothing.

---

## 7. Cross-cutting

| Concern | Decision |
|---|---|
| Routing | React Router, `lazy()` per page + `Suspense`, route-level `ErrorBoundary` |
| Server state | TanStack Query — all API reads/writes |
| Client state | Zustand, promoted only when 3+ distant components need it |
| Forms | React Hook Form + Zod, schema shared with the API types where practical |
| Styling | Tailwind v4 via `@tailwindcss/vite`, semantic design tokens over raw palette classes |
| i18n | i18next; `en` + `zh` from the start |
| Imports | `@/` → `src/` alias in both Vite and tsconfig |
| Dev transport | Vite proxy `/api` → `http://localhost:5258`, so dev is same-origin and CORS never enters the picture (the backend already whitelists `5173` as a fallback) |
| Prod transport | Same-origin; nginx serves the bundle and proxies `/api`, `VITE_API_BASE_URL` stays empty |

### Testing and gates

Same philosophy as 3.x — "test alongside development", regression test with every bug
fix. Gates that must pass before a push:

```
pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

Playwright e2e for critical paths only (login → create → edit → delete). Not wired up
yet; add it when there is a path worth locking down.

---

## 8. Tooling deltas from 3.x

- **Package manager**: `pnpm` (3.x used npm). One lockfile per repo; do not mix.
- **Linter**: `oxlint` ships with the current Vite template and is an order of magnitude
  faster than ESLint. It does not yet cover everything `typescript-eslint` does —
  enable type-aware rules before relying on it for correctness, or switch to ESLint.
- **TypeScript**: `strict` is on. The template does not enable it by default; this repo
  does.

---

## 9. Open decisions

1. **Refresh token storage** — `localStorage` (works today) vs httpOnly cookie (safer,
   needs a backend change). Recommendation: change the backend now; it is cheaper than
   migrating a shipped client.
2. **Where the access code lives** — URL segment, `sessionStorage` per notebook, or a
   prompt. It must never be a query string (history, `Referer`, logs).
3. **Linter** — stay on `oxlint`, or move to ESLint + `typescript-eslint` for rule
   parity with 3.x.
4. **i18n from day one** — 3.x shipped `en`/`zh`. Confirm both locales are wanted before
   `t()` calls are threaded through every component.
5. **Auth surface ordering** — build `authenticate` first (everything depends on the
   token plumbing), or scaffold public read-only notebook browsing first to validate the
   API layer against anonymous endpoints.
