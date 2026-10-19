# clients/web

The CodeCafe web client — React and TypeScript, bundled by Vite. It talks to the
ASP.NET Core API in `server/` over HTTP.

## Commands

All commands run from `clients/web/`.

- `pnpm install` — install dependencies
- `pnpm dev` — dev server with HMR (proxies `/api` to `http://localhost:5258`)
- `pnpm lint` — Oxlint
- `pnpm typecheck` — `tsc -b --noEmit`
- `pnpm test` — Vitest (jsdom, Testing Library)
- `pnpm build` — type-check then bundle to `dist/`
- `pnpm preview` — serve the built bundle locally

## Layout

The client follows Feature-Sliced Design; imports point only at lower layers:

```
app → pages → widgets → features → entities → shared
```

- `src/app` — bootstrap, router, providers, route boundaries
- `src/pages` — route-level screens (home, reader, login, search, trash, account)
- `src/widgets` — composed surfaces (reader layout, page editor, site chrome)
- `src/features` — user actions (auth forms, dialogs, sharing, theme/language)
- `src/entities` — domain slices (notebook, page, block, session) and their APIs
- `src/shared` — API client, i18n, theme, small UI primitives

Every slice exposes its public API through `index.ts`; no deep imports into a slice.

## Notes

- TypeScript runs in strict mode (`tsconfig.app.json`).
- `.editorconfig` here overrides the repository root: 2-space indent for frontend files.
- `public/theme-init.js` runs before first paint so the stored theme never flashes; keep it a
  same-origin external file so the deployed CSP can stay `script-src 'self'`.
- `public/config.js` is the runtime API-origin configuration; deployments mount a ConfigMap over
  it.
