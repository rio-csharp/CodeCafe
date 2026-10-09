# clients/web

The CodeCafe web client — React and TypeScript, bundled by Vite. It talks to the
ASP.NET Core API in `server/` over HTTP.

## Commands

All commands run from `clients/web/`.

- `pnpm install --frozen-lockfile` — install the exact locked dependencies
- `pnpm dev` — dev server with HMR
- `pnpm build` — type-check (`tsc -b`) then bundle to `dist/`
- `pnpm preview` — serve the built bundle locally
- `pnpm lint` — Oxlint
- `pnpm typecheck` — TypeScript project checks without emitting files
- `pnpm test` — run the Vitest component and unit test suite once

## Layout

- `src/app/main.tsx` — entry point; mounts the application providers into `#root`
- `src/app/router.tsx` — browser routes and route-level boundaries
- `src/app/styles/index.css` — Tailwind import, semantic theme tokens, and global styles
- `src/pages/`, `src/widgets/`, `src/features/`, `src/entities/`, `src/shared/` —
  Feature-Sliced Design layers; see `../../docs/frontend-architecture.md`

## Notes

- TypeScript runs in strict mode (`tsconfig.app.json`).
- `.editorconfig` here overrides the repository root: 2-space indent for frontend files.
- The development server proxies `/api` to `http://localhost:5258`; start the API separately as
  described in the repository root README.
