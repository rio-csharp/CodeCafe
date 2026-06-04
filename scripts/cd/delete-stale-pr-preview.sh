#!/usr/bin/env bash
set -euo pipefail

require_env() {
  local name="$1"
  if [ -z "${!name:-}" ]; then
    echo "Missing required environment variable: $name" >&2
    exit 1
  fi
}

shell_quote() {
  local value="$1"
  printf "'%s'" "${value//\'/\'\\\'\'}"
}

for name in \
  NAMESPACE RELEASE IMAGE_TAG \
  TEST_SSH_PORT TEST_SSH_USER TEST_SSH_HOST SSH_KNOWN_HOSTS_FILE; do
  require_env "$name"
done

ssh_target="${TEST_SSH_USER}@${TEST_SSH_HOST}"
ssh_options=(
  -p "$TEST_SSH_PORT"
  -o "UserKnownHostsFile=$SSH_KNOWN_HOSTS_FILE"
  -o StrictHostKeyChecking=yes
)

api_deployment="${RELEASE}-api"
frontend_deployment="${RELEASE}-frontend"
remote_get_images="kubectl get deployment"
remote_get_images+=" $(shell_quote "$api_deployment")"
remote_get_images+=" $(shell_quote "$frontend_deployment")"
remote_get_images+=" --namespace $(shell_quote "$NAMESPACE")"
remote_get_images+=" -o jsonpath='{range .items[*]}{range .spec.template.spec.containers[*]}{.image}{\"\n\"}{end}{end}'"
remote_get_images+=" 2>/dev/null || true"

current_images="$(ssh "${ssh_options[@]}" "$ssh_target" "$remote_get_images")"

if ! grep -qF ":$IMAGE_TAG" <<< "$current_images"; then
  echo "Preview namespace no longer uses image tag $IMAGE_TAG; skipping delete."
  exit 0
fi

echo "Deleting stale preview namespace $NAMESPACE."
ssh "${ssh_options[@]}" "$ssh_target" \
  "kubectl delete namespace $(shell_quote "$NAMESPACE") --ignore-not-found=true"
