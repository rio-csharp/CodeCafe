#!/usr/bin/env bash
set -euo pipefail

for var_name in PR_NUMBER TEST_SSH_PORT TEST_SSH_USER TEST_SSH_HOST SSH_KNOWN_HOSTS_FILE; do
  if [ -z "${!var_name:-}" ]; then
    echo "Missing required environment variable: $var_name" >&2
    exit 1
  fi
done

if [[ ! "$PR_NUMBER" =~ ^[0-9]+$ ]]; then
  echo "PR_NUMBER must be numeric." >&2
  exit 1
fi

namespace_prefix="${NAMESPACE_PREFIX:-codecafe-pr}"
namespace="${namespace_prefix}-${PR_NUMBER}"

echo "Deleting preview namespace $namespace."
ssh -p "$TEST_SSH_PORT" \
  -o UserKnownHostsFile="$SSH_KNOWN_HOSTS_FILE" \
  -o StrictHostKeyChecking=yes \
  "$TEST_SSH_USER@$TEST_SSH_HOST" \
  "if kubectl get namespace '$namespace' >/dev/null 2>&1; then kubectl delete namespace '$namespace'; else echo 'No preview namespace found.'; fi"
