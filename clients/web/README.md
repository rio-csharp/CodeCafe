# clients/web

The CodeCafe web client — React and TypeScript, bundled by Vite. It talks to the
ASP.NET Core API in `server/` over HTTP.

## Commands

All commands run from `clients/web/`.

- `pnpm install` — install dependencies
- `pnpm dev` — dev server with HMR
- `pnpm build` — type-check (`tsc -b`) then bundle to `dist/`
- `pnpm preview` — serve the built bundle locally
- `pnpm lint` — Oxlint

## Layout

- `src/main.tsx` — entry point; mounts `App` into `#root`
- `src/App.tsx` — root component (currently a placeholder)
- `src/index.css` — global styles and the reset

## Notes

- TypeScript runs in strict mode (`tsconfig.app.json`).
- `.editorconfig` here overrides the repository root: 2-space indent for frontend files.
