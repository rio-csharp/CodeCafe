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
  GITHUB_RUN_ID GITHUB_RUN_ATTEMPT SSH_KNOWN_HOSTS_FILE \
  ROLLBACK_SSH_PORT ROLLBACK_SSH_USER ROLLBACK_SSH_HOST \
  NAMESPACE RELEASE FRONTEND_HOST API_HOST; do
  require_env "$name"
done

cd_scripts_dir="${CD_SCRIPTS_DIR:-scripts/cd}"
rollback_script="${ROLLBACK_HELM_SCRIPT:-$cd_scripts_dir/rollback-helm-release.sh}"
smoke_script="${SMOKE_TEST_SCRIPT:-$cd_scripts_dir/smoke-test-release.sh}"
port_forward_lib="${PORT_FORWARD_LIB:-$cd_scripts_dir/lib-port-forward.sh}"

for path in "$rollback_script" "$smoke_script" "$port_forward_lib"; do
  if [ ! -f "$path" ]; then
    echo "Required rollback input is missing: $path" >&2
    exit 1
  fi
done

remote_dir="${REMOTE_DIR:-/tmp/codecafe-rollback-${GITHUB_RUN_ID}-${GITHUB_RUN_ATTEMPT}}"
remote_dir_created=false

ssh_target="${ROLLBACK_SSH_USER}@${ROLLBACK_SSH_HOST}"
ssh_options=(
  -p "$ROLLBACK_SSH_PORT"
  -o "UserKnownHostsFile=$SSH_KNOWN_HOSTS_FILE"
  -o StrictHostKeyChecking=yes
)

cleanup() {
  if [ "$remote_dir_created" = true ]; then
    ssh "${ssh_options[@]}" "$ssh_target" "rm -rf $(shell_quote "$remote_dir")" >/dev/null 2>&1 || true
  fi
}

on_signal() {
  local signal_name="$1"
  echo "Rollback interrupted by $signal_name; cleaning up remote rollback inputs." >&2
  exit 130
}

trap cleanup EXIT
trap 'on_signal INT' INT
trap 'on_signal TERM' TERM
trap 'on_signal HUP' HUP

echo "Copying rollback scripts to $remote_dir."
ssh "${ssh_options[@]}" "$ssh_target" "mkdir -p $(shell_quote "$remote_dir")"
remote_dir_created=true
scp -P "$ROLLBACK_SSH_PORT" \
  -o "UserKnownHostsFile=$SSH_KNOWN_HOSTS_FILE" \
  -o StrictHostKeyChecking=yes \
  "$rollback_script" "$smoke_script" "$port_forward_lib" "$ssh_target:$remote_dir/"

remote_common="NAMESPACE=$(shell_quote "$NAMESPACE")"
remote_common+=" RELEASE=$(shell_quote "$RELEASE")"

for optional_name in KUBECTL_BIN HELM_BIN HELM_TIMEOUT; do
  if [ -n "${!optional_name:-}" ]; then
    remote_common+=" $optional_name=$(shell_quote "${!optional_name}")"
  fi
done

rollback_command="$remote_common"
if [ -n "${REVISION:-}" ]; then
  rollback_command+=" REVISION=$(shell_quote "$REVISION")"
fi
rollback_command+=" bash $(shell_quote "$remote_dir/rollback-helm-release.sh")"

smoke_command="$remote_common"
smoke_command+=" FRONTEND_HOST=$(shell_quote "$FRONTEND_HOST")"
smoke_command+=" API_HOST=$(shell_quote "$API_HOST")"
smoke_command+=" bash $(shell_quote "$remote_dir/smoke-test-release.sh")"

echo "Rolling back $NAMESPACE/$RELEASE."
ssh "${ssh_options[@]}" "$ssh_target" "$rollback_command"

echo "Running rollback smoke test for $NAMESPACE/$RELEASE."
ssh "${ssh_options[@]}" "$ssh_target" "$smoke_command"
