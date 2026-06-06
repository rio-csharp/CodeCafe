# AGENTS.md

Create new working branches from the intended remote base branch (`origin/release/*`), never from the current local branch; run `git fetch origin` first, then branch from the target remote release branch.
Open PRs as `feature/* -> release/*`; only `release/* -> main` is allowed.

Before pushing or reporting that CI should be green, run the relevant local checks and ensure they pass:
- **Frontend**: `npm run lint && npm run test && npm run build` from `frontend`
- **Backend**: `dotnet build` and `dotnet test` from solution root
Always report the result.
