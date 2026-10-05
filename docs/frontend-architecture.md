# Frontend Architecture

The client is `clients/web`, a React SPA that speaks REST to `server/`.
This file records the **agreed** decisions; change it when the code changes.

## Stack

| Concern | Decision |
|---|---|
| Framework | React 19 + Vite 8 + TypeScript (strict) |
| Package manager | pnpm (one lockfile, do not mix with npm) |
| Linter | oxlint |
| Styling | Tailwind v4 via `@tailwindcss/vite`; semantic design tokens over raw palette classes |
| Routing | React Router, `lazy()` per page + `Suspense` |
| Server state | TanStack Query — all API reads/writes |
| Client state | Zustand, only when 3+ distant components need it |
| Forms | React Hook Form + Zod |
| i18n | i18next, `en` + `zh` from day one — no hardcoded copy |
| Imports | `@/` → `src/` alias in both Vite and tsconfig |
| Tests | Vitest + Testing Library (jsdom); colocated `*.test.ts(x)`, setup in `src/test/setup.ts` |
| Dev transport | Vite proxy `/api` → `http://localhost:5258` (same-origin in dev, CORS never involved) |

## Layering

Feature-Sliced Design, import only from lower layers:

```
app → pages → widgets → features → entities → shared
```

Every slice exposes a public API through its `index.ts`; no deep imports into a slice.

## API contract

- Success: `{ value: T, error: null, isSuccess: true }`; failure: `{ value: null, error: { code, message, kind } }`.
  `shared/api` unwraps the envelope — feature code never sees `isSuccess`. Failures throw `ApiError { status, code, kind }`.
- Lists: `PagedResult<T>` = `{ items, page, pageSize, totalCount, hasNextPage }`.
- Enums travel as strings (`"Public"`, `"Validation"`).
- Escape hatches (not the envelope): `GET .../export` returns `text/markdown`; `POST .../ai/chat` returns SSE.

## Session (when auth lands — not M1)

- Bearer JWT (15 min) + refresh token (30 days, rotates every refresh), both in the response body.
- Access token: memory only. Refresh token: `localStorage` (accepted trade-off — XSS-readable, no cookie option under current CORS).
- Refresh must be single-flight with a queue-and-replay for concurrent 401s.

## Anonymous access

Auth is not a hard gate. Two list endpoints keep the semantics clean:
`GET /api/notebooks` (auth — own + shared) and `GET /api/notebooks/public`
(anonymous — the public catalog). Anonymous callers can also read notebook details,
tree, page-by-path, and export. Every read UI must work without a session.

## Gates

Everything must pass before a change is considered done:

```
pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Milestones

- **M1 — homepage shell**: scaffold (router, query, tailwind, i18n, alias, FSD skeleton),
  `shared/api` client (no auth), anonymous homepage reading the real public catalog.
- **M2 — auth**: login/register, token plumbing, logged-in homepage (own + shared notebooks).
- **M3+** — notebook reader, editor, trash, search, AI. Planned later.
