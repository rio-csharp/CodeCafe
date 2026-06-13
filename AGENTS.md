# AGENTS.md

Create new working branches from the intended remote base branch (`origin/release/*`), never from the current local branch; run `git fetch origin` first, then branch from the target remote release branch when a separate feature branch is still desired.
Open PRs as `feature/* -> release/*` for normal release work; `release/* -> main` remains allowed.
Direct hotfix work on `release/*` is allowed when the team explicitly chooses that path, including local commits and direct pushes to the target `release/*` branch.
If asked to "merge into release/*", either push the `feature/*` branch and prepare or create the PR, or push directly to `release/*` when the user explicitly requests the relaxed hotfix flow.
Treat `main` as pull-request-only: never push directly to `main`; merge changes through a PR after required checks pass.
Prefer the GitHub CLI (`gh`) for creating and managing pull requests when it is available.

Before pushing or reporting that CI should be green, run the relevant local checks and ensure they pass:
- **Frontend**: `npm run lint && npm run test && npm run build && npm run e2e` from `frontend`
- **Backend**: `dotnet build` and `dotnet test` from solution root
Always report the result.
