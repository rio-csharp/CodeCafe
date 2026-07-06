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
