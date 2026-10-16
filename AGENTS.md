## Commits

Conventional Commits (`feat:`, `fix:`, `refactor:`, `chore:`, `docs:`), scoped where useful.

## Comments

Code should be self-explanatory; write comments only when a "why" cannot be expressed in code. When a comment is necessary, write it in English.

## Build & Test

All commands run from `server/`.

- Build: `dotnet build CodeCafe.slnx`
- Unit tests: `dotnet test --project tests/CodeCafe.Application.Tests` (same for `CodeCafe.Domain.Tests` and `CodeCafe.Host.Tests`)
- `CodeCafe.Infrastructure.Tests` requires a real PostgreSQL (Docker/Testcontainers).
- This repo uses Microsoft.Testing.Platform: **never** pass `--nologo`, `--logger`, `--blame-*`, `-tl`, or `-m:1` to `dotnet test`. Unmatched tokens are forwarded to every test assembly, which exits 5 before the handshake and renders as `Zero tests ran`. Write `--no-banner` if you mean the banner, and add `--minimum-expected-tests 1` so a false green fails loudly with exit 9.
- Running the backend with output redirection: write to `server/logs/` (gitignored), never the `server/` root.
