# AGENTS.md

Create new working branches from the intended remote base branch (`origin/release/*`), never from the current local branch; run `git fetch origin` first, then branch from the target remote release branch.
Open PRs as `feature/* -> release/*`; only `release/* -> main` is allowed.
