# Scripts

Repository automation scripts are grouped by what owns the workflow:

- `ci/`: CI-only policy and artifact metadata helpers.
- `cd/`: deployment runtime scripts. `deploy-environment.sh` runs on the GitHub runner and
  `ssh`/`scp`s its inputs to the target host; `deploy-helm.sh`, `rollback-helm-release.sh` and
  `smoke-test-release.sh` run **on the host**, so they are also packaged as a CI artifact.
- `github/`: GitHub Actions orchestration helpers (PR preview resolution, CI run validation).
- `maintenance/`: operational cleanup for PR preview namespaces.

`scripts/cd` is deliberately dependency-free apart from `kubectl`, `helm`, `ssh` and `curl`: it is
executed from a checkout of a protected branch rather than from a PR-built artifact, so it must stay
readable enough to review in a pull request.
