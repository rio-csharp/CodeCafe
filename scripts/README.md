# Scripts

Repository automation scripts are grouped by what owns the workflow:

- `ci/`: CI-only policy and artifact metadata helpers.
- `cd/`: deployment runtime scripts, including deploy, rollback, and smoke-test helpers.
- `github/`: GitHub Actions orchestration helpers.
- `maintenance/`: operational cleanup and environment maintenance scripts.

The previous single-file database sync script has been replaced by the maintained
console project at `server/tools/CodeCafe.DbSync`.

Use:

```bash
dotnet run --project server/tools/CodeCafe.DbSync -- check
dotnet run --project server/tools/CodeCafe.DbSync -- prod-to-local
dotnet run --project server/tools/CodeCafe.DbSync -- local-to-test
```

## Frontend debug helpers

`clients/web/scripts/check-mine.mjs` (lives under `clients/web/scripts/`, not the
root `scripts/` tree): hardcoded-localhost debug helper that reuses the Playwright
auth state from `clients/web/e2e/.auth/user.json` and prints the status and first
bytes of `GET /api/notes/mine?limit=50` and `GET /api/notes/public?limit=50`
against `http://localhost:5042`. Run it from the frontend directory (the auth
state path is relative), with the local backend and an e2e login already done:

```bash
cd clients/web && node scripts/check-mine.mjs
```
