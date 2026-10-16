#!/usr/bin/env bash
set -euo pipefail

for var_name in GH_TOKEN GITHUB_REPOSITORY TEST_SSH_PORT TEST_SSH_USER TEST_SSH_HOST SSH_KNOWN_HOSTS_FILE; do
  if [ -z "${!var_name:-}" ]; then
    echo "Missing required environment variable: $var_name" >&2
    exit 1
  fi
done

namespace_prefix="${NAMESPACE_PREFIX:-codecafe-pr}"

all_namespaces="$(ssh -p "$TEST_SSH_PORT" \
  -o UserKnownHostsFile="$SSH_KNOWN_HOSTS_FILE" \
  -o StrictHostKeyChecking=yes \
  "$TEST_SSH_USER@$TEST_SSH_HOST" \
  "kubectl get namespaces -o jsonpath='{range .items[*]}{.metadata.name}{\"\n\"}{end}'")"

found_preview_namespace=false
while IFS= read -r namespace; do
  [ -n "$namespace" ] || continue
  [[ "$namespace" =~ ^${namespace_prefix}-[0-9]+$ ]] || continue

  found_preview_namespace=true
  pr_number="${namespace#${namespace_prefix}-}"
  # Distinguish "query failed" (network, rate limit, token) from a real
  # non-OPEN state; never delete on a failed lookup.
  if ! state="$(gh pr view "$pr_number" --repo "$GITHUB_REPOSITORY" --json state --jq .state 2>/dev/null)"; then
    echo "Skipping namespace $namespace: failed to query state of PR #$pr_number." >&2
    continue
  fi

  if [ "$state" != "OPEN" ]; then
    echo "Deleting stale namespace $namespace for PR #$pr_number (state: $state)."
    ssh -n -p "$TEST_SSH_PORT" \
      -o UserKnownHostsFile="$SSH_KNOWN_HOSTS_FILE" \
      -o StrictHostKeyChecking=yes \
      "$TEST_SSH_USER@$TEST_SSH_HOST" \
      "kubectl delete namespace '$namespace' --ignore-not-found=true"
  else
    echo "Keeping namespace $namespace for open PR #$pr_number."
  fi
done <<< "$all_namespaces"

if [ "$found_preview_namespace" = false ]; then
  echo "No PR preview namespaces found."
fi
