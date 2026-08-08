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
  DEPLOY_SSH_PORT DEPLOY_SSH_USER DEPLOY_SSH_HOST \
  NAMESPACE RELEASE TLS_SECRET IMAGE_TAG FRONTEND_HOST API_HOST \
  REGISTRY IMAGE_NAMESPACE DB_SECRET_NAMESPACE; do
  require_env "$name"
done

helm_artifact_dir="${HELM_ARTIFACT_DIR:-.artifacts/helm}"
cd_scripts_dir="${CD_SCRIPTS_DIR:-.artifacts/scripts}"
oauth_env_file="${OAUTH_ENV_FILE:-.artifacts/deployment/oauth.env}"
ai_env_file="${AI_ENV_FILE:-.artifacts/deployment/ai.env}"
deploy_helm_script="${cd_scripts_dir}/deploy-helm.sh"
port_forward_lib="${cd_scripts_dir}/lib-port-forward.sh"

for path in "$helm_artifact_dir" "$deploy_helm_script" "$port_forward_lib" "$oauth_env_file"; do
  if [ ! -e "$path" ]; then
    echo "Required deploy input is missing: $path" >&2
    exit 1
  fi
done

chart_archive="${CHART_ARCHIVE:-codecafe-chart-${GITHUB_RUN_ID}-${GITHUB_RUN_ATTEMPT}.tgz}"
remote_dir="${REMOTE_DIR:-/tmp/codecafe-${GITHUB_RUN_ID}-${GITHUB_RUN_ATTEMPT}}"
remote_dir_created=false
remote_deploy_started=false
ai_env_file_written=false

ssh_target="${DEPLOY_SSH_USER}@${DEPLOY_SSH_HOST}"
ssh_options=(
  -p "$DEPLOY_SSH_PORT"
  -o "UserKnownHostsFile=$SSH_KNOWN_HOSTS_FILE"
  -o StrictHostKeyChecking=yes
)

cleanup() {
  rm -f "$chart_archive"

  if [ "$ai_env_file_written" = true ]; then
    rm -f "$ai_env_file"
  fi

  if [ "$remote_dir_created" = true ] && [ "$remote_deploy_started" != true ]; then
    ssh "${ssh_options[@]}" "$ssh_target" "rm -rf $(shell_quote "$remote_dir")" >/dev/null 2>&1 || true
  fi
}

on_signal() {
  local signal_name="$1"
  echo "Deployment interrupted by $signal_name; cleaning up local deploy inputs." >&2
  exit 130
}

trap cleanup EXIT
trap 'on_signal INT' INT
trap 'on_signal TERM' TERM
trap 'on_signal HUP' HUP

echo "Packaging Helm chart."
tar -czf "$chart_archive" -C "$helm_artifact_dir" .

echo "Copying deploy inputs to $remote_dir."
ssh "${ssh_options[@]}" "$ssh_target" "mkdir -p $(shell_quote "$remote_dir/codecafe")"
remote_dir_created=true
scp -P "$DEPLOY_SSH_PORT" \
  -o "UserKnownHostsFile=$SSH_KNOWN_HOSTS_FILE" \
  -o StrictHostKeyChecking=yes \
  "$chart_archive" "$ssh_target:$remote_dir/codecafe-chart.tgz"
scp -P "$DEPLOY_SSH_PORT" \
  -o "UserKnownHostsFile=$SSH_KNOWN_HOSTS_FILE" \
  -o StrictHostKeyChecking=yes \
  "$deploy_helm_script" "$ssh_target:$remote_dir/deploy-helm.sh"
scp -P "$DEPLOY_SSH_PORT" \
  -o "UserKnownHostsFile=$SSH_KNOWN_HOSTS_FILE" \
  -o StrictHostKeyChecking=yes \
  "$port_forward_lib" "$ssh_target:$remote_dir/lib-port-forward.sh"
scp -P "$DEPLOY_SSH_PORT" \
  -o "UserKnownHostsFile=$SSH_KNOWN_HOSTS_FILE" \
  -o StrictHostKeyChecking=yes \
  "$oauth_env_file" "$ssh_target:$remote_dir/oauth.env"

# Ship AI_API_KEY as a file (like oauth.env) instead of on the remote command
# line, where it would be visible to ps and sshd/sudo audit logs.
if [ -n "${AI_API_KEY:-}" ]; then
  (umask 077; printf 'AI_API_KEY=%q\n' "$AI_API_KEY" > "$ai_env_file")
  ai_env_file_written=true
  scp -P "$DEPLOY_SSH_PORT" \
    -o "UserKnownHostsFile=$SSH_KNOWN_HOSTS_FILE" \
    -o StrictHostKeyChecking=yes \
    "$ai_env_file" "$ssh_target:$remote_dir/ai.env"
fi

remote_command="REMOTE_DIR=$(shell_quote "$remote_dir")"
remote_command+=" NAMESPACE=$(shell_quote "$NAMESPACE")"
remote_command+=" RELEASE=$(shell_quote "$RELEASE")"
remote_command+=" TLS_SECRET=$(shell_quote "$TLS_SECRET")"
remote_command+=" IMAGE_TAG=$(shell_quote "$IMAGE_TAG")"
remote_command+=" FRONTEND_HOST=$(shell_quote "$FRONTEND_HOST")"
remote_command+=" API_HOST=$(shell_quote "$API_HOST")"
remote_command+=" REGISTRY=$(shell_quote "$REGISTRY")"
remote_command+=" IMAGE_NAMESPACE=$(shell_quote "$IMAGE_NAMESPACE")"
remote_command+=" DB_SECRET_NAMESPACE=$(shell_quote "$DB_SECRET_NAMESPACE")"
remote_command+=" OAUTH_ENV_FILE=$(shell_quote "$remote_dir/oauth.env")"

for optional_name in \
  VALUES_FILE KUBECTL_BIN HELM_BIN \
  HELM_TIMEOUT FRONTEND_REPLICA_COUNT API_REPLICA_COUNT API_MIGRATION_ENABLED \
  TLS_SECRET_NAMESPACE \
  AI_ENABLED AI_MODEL AI_BASE_URL \
  AI_ENDPOINT_PATH AI_STATUS_ENDPOINT_PATH AI_DRAFT_ENDPOINT_PATH AI_AGENT_NAME \
  AI_MAX_TOOL_RESULTS AI_MAX_TOOL_CONTENT_CHARS \
  AI_MAX_DRAFT_PROMPT_CHARS AI_MAX_DRAFT_CONTEXT_CHARS AI_MAX_DRAFT_OUTPUT_TOKENS; do
  if [ -n "${!optional_name:-}" ]; then
    remote_command+=" $optional_name=$(shell_quote "${!optional_name}")"
  fi
done

if [ "$ai_env_file_written" = true ]; then
  remote_command+=" AI_ENV_FILE=$(shell_quote "$remote_dir/ai.env")"
fi

remote_command+=" bash $(shell_quote "$remote_dir/deploy-helm.sh")"

echo "Deploying $NAMESPACE."
remote_deploy_started=true
ssh "${ssh_options[@]}" "$ssh_target" "$remote_command"
