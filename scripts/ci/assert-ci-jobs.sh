#!/usr/bin/env bash
# Turns the individual job results of ci.yml into the single aggregated status
# branch protection requires, so requiring one check name is enough.
#
# A required check that is *skipped* never becomes success, which is why the
# conditional jobs cannot be required directly: every pull request would block
# forever. That is the whole reason this gate exists, and the grouping below is
# the one home for "what must pass".
#
# Runnable locally to reproduce a decision — the workflow passes the same
# variables:
#
#   UPLOAD_ARTIFACTS=false ARTIFACT_POLICY=success BACKEND=success \
#     FRONTEND=success CD_SCRIPTS=success HELM_CHART=skipped \
#     DEPLOYMENT_METADATA=skipped bash scripts/ci/assert-ci-jobs.sh
set -euo pipefail

# Always run, so anything but success fails the gate.
required_jobs=(artifact-policy backend frontend cd-scripts)

# Only run when the artifact policy decided to publish, so a skip is legitimate —
# but on a publishing run they must have succeeded, or the deploy workflow would
# download an artifact that is not there.
conditional_jobs=(helm-chart deployment-metadata)

upload_artifacts="${UPLOAD_ARTIFACTS:-}"

# Job ids reach this script as environment variables named after them
# (artifact-policy -> ARTIFACT_POLICY), which is how the step passes
# needs.<job>.result without interpolating expressions into a shell script.
result_of() {
  local job="$1"
  local var="${job//-/_}"
  var="${var^^}"

  if [ -z "${!var:-}" ]; then
    echo "Missing $var: ci.yml must pass needs.$job.result into this step." >&2
    exit 1
  fi

  printf '%s' "${!var}"
}

failed=0

for job in "${required_jobs[@]}"; do
  result="$(result_of "$job")"
  printf '%-20s %s\n' "$job" "$result"

  if [ "$result" != "success" ]; then
    failed=1
  fi
done

for job in "${conditional_jobs[@]}"; do
  result="$(result_of "$job")"
  printf '%-20s %s\n' "$job" "$result"

  if [ "$result" != "success" ] && [ "$result" != "skipped" ]; then
    failed=1
  fi

  if [ "$upload_artifacts" = "true" ] && [ "$result" != "success" ]; then
    failed=1
  fi
done

if [ "$failed" -ne 0 ]; then
  echo "CI gate failed (upload_artifacts=${upload_artifacts:-unset})." >&2
  exit 1
fi

echo "All required jobs succeeded."
