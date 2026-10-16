#!/usr/bin/env bash
set -euo pipefail

require_env() {
  local name="$1"
  if [ -z "${!name:-}" ]; then
    echo "Missing required environment variable: $name" >&2
    exit 1
  fi
}

for name in \
  REMOTE_DIR NAMESPACE RELEASE TLS_SECRET IMAGE_TAG FRONTEND_HOST API_HOST \
  REGISTRY IMAGE_NAMESPACE DB_SECRET_NAMESPACE; do
  require_env "$name"
done

export PATH=/usr/local/bin:/usr/bin:/bin:/usr/local/sbin:/usr/sbin:/sbin

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
. "$script_dir/lib-port-forward.sh"

KUBECTL_BIN="${KUBECTL_BIN:-kubectl}"
HELM_BIN="${HELM_BIN:-helm}"
HELM_TIMEOUT="${HELM_TIMEOUT:-180s}"
VALUES_FILE="${VALUES_FILE:-}"
FRONTEND_REPLICA_COUNT="${FRONTEND_REPLICA_COUNT:-}"
API_REPLICA_COUNT="${API_REPLICA_COUNT:-}"
API_MIGRATION_ENABLED="${API_MIGRATION_ENABLED:-}"
API_MIGRATE_ON_STARTUP="${API_MIGRATE_ON_STARTUP:-}"
# No script-side default either: unset means no --set, so the chart's off-by-default
# stands and a deployment that forgets the variable cannot open registration.
REGISTRATION_ENABLED="${REGISTRATION_ENABLED:-}"
AI_ENABLED="${AI_ENABLED:-false}"
AI_MODEL="${AI_MODEL:-}"
AI_BASE_URL="${AI_BASE_URL:-}"
AI_WIRE_FORMAT="${AI_WIRE_FORMAT:-}"
AI_RELAY="${AI_RELAY:-}"
# Deliberately no script-side default for the caps: when unset, no --set is
# passed and the chart default applies, so script/chart defaults cannot drift
# apart.
AI_MAX_OUTPUT_TOKENS="${AI_MAX_OUTPUT_TOKENS:-}"
AI_MAX_HISTORY_CHARS="${AI_MAX_HISTORY_CHARS:-}"
AI_MAX_OUTLINE_CHARS="${AI_MAX_OUTLINE_CHARS:-}"
AI_MAX_TOOL_RESULT_CHARS="${AI_MAX_TOOL_RESULT_CHARS:-}"
AI_MAX_TOOL_ITERATIONS="${AI_MAX_TOOL_ITERATIONS:-}"
AI_MAX_PROVIDER_RETRIES="${AI_MAX_PROVIDER_RETRIES:-}"
AI_PROVIDER_RETRY_BASE_DELAY_MS="${AI_PROVIDER_RETRY_BASE_DELAY_MS:-}"
AI_API_KEY="${AI_API_KEY:-}"
TLS_SECRET_NAMESPACE="${TLS_SECRET_NAMESPACE:-codecafe-shared}"
AI_ENV_FILE="${AI_ENV_FILE:-}"
tls_cert_file=""
tls_key_file=""
frontend_pid=""
api_pid=""
init_port_forward_logs "$RELEASE"

cleanup() {
  port_forward_cleanup
  remove_port_forward_logs
  rm -f "${tls_cert_file:-}" "${tls_key_file:-}"
  rm -rf "$REMOTE_DIR"
}

on_signal() {
  local signal_name="$1"
  echo "Deployment interrupted by $signal_name; cleaning up remote deploy inputs." >&2
  exit 130
}

trap cleanup EXIT
trap 'on_signal INT' INT
trap 'on_signal TERM' TERM
trap 'on_signal HUP' HUP

if [ -n "$AI_ENV_FILE" ]; then
  # shellcheck disable=SC1090
  . "$AI_ENV_FILE"
fi

case "$AI_ENABLED" in
  true|false)
    ;;
  *)
    echo "AI_ENABLED must be either 'true' or 'false'." >&2
    exit 1
    ;;
esac

if [ "$AI_ENABLED" = "true" ]; then
  if [ -z "$AI_MODEL" ]; then
    echo "AI_MODEL is required when AI_ENABLED=true." >&2
    exit 1
  fi

  if [ -z "$AI_API_KEY" ]; then
    echo "AI_API_KEY is required when AI_ENABLED=true." >&2
    exit 1
  fi
fi

chart_dir="$REMOTE_DIR/codecafe"
api_config_secret="${RELEASE}-api-config"

# The API validates Auth:Jwt:SigningKey at start-up (>= 32 bytes) and refuses to
# boot without it, so a deployment has to supply one. Reusing whatever the last
# deploy wrote into <release>-api-config keeps sessions valid across redeploys;
# only a first install (or a deleted secret) mints a new key.
read_existing_jwt_signing_key() {
  $KUBECTL_BIN get secret "$api_config_secret" --namespace "$NAMESPACE" \
    -o jsonpath='{.data.Auth__Jwt__SigningKey}' 2>/dev/null | base64 -d 2>/dev/null || true
}

generate_jwt_signing_key() {
  if command -v openssl >/dev/null 2>&1; then
    openssl rand -hex 32
  else
    od -An -tx1 -N32 /dev/urandom | tr -d ' \n'
  fi
}

tar -xzf "$REMOTE_DIR/codecafe-chart.tgz" -C "$chart_dir"

$KUBECTL_BIN create namespace "$NAMESPACE" --dry-run=client -o yaml | $KUBECTL_BIN apply -f -

tls_cert_file="$(mktemp)"
tls_key_file="$(mktemp)"
$KUBECTL_BIN get secret "$TLS_SECRET" --namespace "$TLS_SECRET_NAMESPACE" -o jsonpath='{.data.tls\.crt}' | base64 -d > "$tls_cert_file"
$KUBECTL_BIN get secret "$TLS_SECRET" --namespace "$TLS_SECRET_NAMESPACE" -o jsonpath='{.data.tls\.key}' | base64 -d > "$tls_key_file"
$KUBECTL_BIN create secret tls "$TLS_SECRET" \
  --cert="$tls_cert_file" \
  --key="$tls_key_file" \
  --namespace "$NAMESPACE" \
  --dry-run=client -o yaml | $KUBECTL_BIN apply -f -
rm -f "$tls_cert_file" "$tls_key_file"
tls_cert_file=""
tls_key_file=""

jwt_signing_key="$(read_existing_jwt_signing_key)"
if [ -z "$jwt_signing_key" ]; then
  echo "No JWT signing key found in $NAMESPACE/$api_config_secret; generating one."
  jwt_signing_key="$(generate_jwt_signing_key)"
fi

umask 077
$KUBECTL_BIN get secret codecafe-db-secret \
  --namespace "$DB_SECRET_NAMESPACE" \
  -o jsonpath='{.data.ConnectionStrings__DefaultConnection}' \
  | base64 -d \
  | awk '{ printf "ConnectionStrings__DefaultConnection=%s\n", $0 }' > "$REMOTE_DIR/api.env"

cat <<EOF >> "$REMOTE_DIR/api.env"
Auth__Jwt__SigningKey=$jwt_signing_key
AllowedHosts=$FRONTEND_HOST;$API_HOST
EOF

if [ "$AI_ENABLED" = "true" ]; then
  printf 'Ai__ApiKey=%s\n' "$AI_API_KEY" >> "$REMOTE_DIR/api.env"
fi

$KUBECTL_BIN create secret generic "$api_config_secret" \
  --from-env-file="$REMOTE_DIR/api.env" \
  --namespace "$NAMESPACE" \
  --dry-run=client -o yaml | $KUBECTL_BIN apply -f -

helm_args=(
  upgrade --install "$RELEASE" "$chart_dir"
  --namespace "$NAMESPACE"
  --atomic
  --cleanup-on-fail
  --wait
  --timeout "$HELM_TIMEOUT"
  --set "frontend.image.repository=$REGISTRY/$IMAGE_NAMESPACE/frontend"
  --set "frontend.image.tag=$IMAGE_TAG"
  # The SPA calls the API host directly, so the origin has to be baked into the
  # deployed runtime config rather than inferred from the address bar.
  --set-string "frontend.config.apiBaseUrl=https://$API_HOST"
  --set "frontend.ingress.host=$FRONTEND_HOST"
  --set "frontend.ingress.tls.secretName=$TLS_SECRET"
  --set "api.image.repository=$REGISTRY/$IMAGE_NAMESPACE/api"
  --set "api.image.tag=$IMAGE_TAG"
  --set "api.envFromSecrets[0]=$api_config_secret"
  --set-string "api.allowedHosts=$FRONTEND_HOST;$API_HOST"
  --set "api.ai.enabled=$AI_ENABLED"
  --set "api.cors.allowedOrigins[0]=https://$FRONTEND_HOST"
  --set "api.ingress.host=$API_HOST"
  --set "api.ingress.tls.secretName=$TLS_SECRET"
)

if [ -n "$VALUES_FILE" ]; then
  helm_args+=(--values "$chart_dir/$VALUES_FILE")
fi

if [ -n "$FRONTEND_REPLICA_COUNT" ]; then
  helm_args+=(--set "frontend.replicaCount=$FRONTEND_REPLICA_COUNT")
fi

if [ -n "$API_REPLICA_COUNT" ]; then
  helm_args+=(--set "api.replicaCount=$API_REPLICA_COUNT")
fi

if [ -n "$API_MIGRATION_ENABLED" ]; then
  helm_args+=(--set "api.migration.enabled=$API_MIGRATION_ENABLED")
fi

if [ -n "$API_MIGRATE_ON_STARTUP" ]; then
  helm_args+=(--set "api.migrateOnStartup=$API_MIGRATE_ON_STARTUP")
fi

if [ -n "$REGISTRATION_ENABLED" ]; then
  helm_args+=(--set "api.auth.registrationEnabled=$REGISTRATION_ENABLED")
fi

if [ -n "$AI_MODEL" ]; then
  helm_args+=(--set-string "api.ai.model=$AI_MODEL")
fi

if [ -n "$AI_BASE_URL" ]; then
  helm_args+=(--set-string "api.ai.baseUrl=$AI_BASE_URL")
fi

if [ -n "$AI_WIRE_FORMAT" ]; then
  helm_args+=(--set-string "api.ai.wireFormat=$AI_WIRE_FORMAT")
fi

if [ -n "$AI_RELAY" ]; then
  helm_args+=(--set-string "api.ai.relay=$AI_RELAY")
fi

for pair in \
  "AI_MAX_OUTPUT_TOKENS:api.ai.maxOutputTokens" \
  "AI_MAX_HISTORY_CHARS:api.ai.maxHistoryChars" \
  "AI_MAX_OUTLINE_CHARS:api.ai.maxOutlineChars" \
  "AI_MAX_TOOL_RESULT_CHARS:api.ai.maxToolResultChars" \
  "AI_MAX_TOOL_ITERATIONS:api.ai.maxToolIterations" \
  "AI_MAX_PROVIDER_RETRIES:api.ai.maxProviderRetries" \
  "AI_PROVIDER_RETRY_BASE_DELAY_MS:api.ai.providerRetryBaseDelayMs"; do
  env_name="${pair%%:*}"
  helm_key="${pair#*:}"

  if [ -n "${!env_name:-}" ]; then
    helm_args+=(--set "$helm_key=${!env_name}")
  fi
done

release_status="$($HELM_BIN status "$RELEASE" --namespace "$NAMESPACE" -o template --template '{{.info.status}}' 2>/dev/null || true)"
case "$release_status" in
  pending-install)
    echo "Found pending Helm install for $RELEASE; uninstalling before retry."
    $HELM_BIN uninstall "$RELEASE" --namespace "$NAMESPACE" --wait --timeout "$HELM_TIMEOUT" || true
    ;;
  pending-upgrade|pending-rollback)
    echo "Found pending Helm state '$release_status' for $RELEASE; attempting rollback before retry."
    $HELM_BIN rollback "$RELEASE" 0 --namespace "$NAMESPACE" --wait --timeout "$HELM_TIMEOUT" || true
    ;;
esac

if ! $HELM_BIN "${helm_args[@]}"; then
  echo "Helm deployment failed; collecting migration job diagnostics." >&2
  $KUBECTL_BIN get pods --namespace "$NAMESPACE" \
    -l "app.kubernetes.io/instance=$RELEASE" -o wide >&2 || true
  $KUBECTL_BIN logs --namespace "$NAMESPACE" \
    -l "app.kubernetes.io/component=api-migration" \
    --all-containers --tail=-1 >&2 || true
  $KUBECTL_BIN describe job "${RELEASE}-api-migrate" \
    --namespace "$NAMESPACE" >&2 || true
  exit 1
fi

$KUBECTL_BIN rollout status deployment \
  --selector "app.kubernetes.io/instance=$RELEASE,app.kubernetes.io/component=frontend" \
  --namespace "$NAMESPACE" \
  --timeout=180s
$KUBECTL_BIN rollout status deployment \
  --selector "app.kubernetes.io/instance=$RELEASE,app.kubernetes.io/component=api" \
  --namespace "$NAMESPACE" \
  --timeout=180s

frontend_port="$(pick_port)"
api_port="$(pick_port)"
$KUBECTL_BIN port-forward --address 127.0.0.1 "service/${RELEASE}-frontend" "${frontend_port}:80" --namespace "$NAMESPACE" >"$frontend_log" 2>&1 &
frontend_pid=$!
$KUBECTL_BIN port-forward --address 127.0.0.1 "service/${RELEASE}-api" "${api_port}:80" --namespace "$NAMESPACE" >"$api_log" 2>&1 &
api_pid=$!

for _ in $(seq 1 20); do
  if ! kill -0 "$frontend_pid" 2>/dev/null || ! kill -0 "$api_pid" 2>/dev/null; then
    dump_port_forward_logs
    exit 1
  fi

  # Five checks: the SPA document, the API's readiness, the API path the browser
  # calls first, the runtime config that points the SPA at that API host, and the
  # frontend's same-origin /api fallback (unused in a deployed environment, but it
  # is what a bare image falls back to).
  if curl --silent --fail --header "Host: $FRONTEND_HOST" "http://127.0.0.1:${frontend_port}/" >/dev/null \
    && curl --silent --fail --header "Host: $API_HOST" "http://127.0.0.1:${api_port}/health/ready" >/dev/null \
    && curl --silent --fail --header "Host: $API_HOST" "http://127.0.0.1:${api_port}/api/notebooks/public" >/dev/null \
    && curl --silent --fail --header "Host: $FRONTEND_HOST" "http://127.0.0.1:${frontend_port}/config.js" | grep -q "https://$API_HOST" \
    && curl --silent --fail --header "Host: $FRONTEND_HOST" "http://127.0.0.1:${frontend_port}/api/notebooks/public" >/dev/null; then
    exit 0
  fi
  sleep 3
done

dump_port_forward_logs
curl --fail --show-error --header "Host: $FRONTEND_HOST" "http://127.0.0.1:${frontend_port}/" || true
curl --fail --show-error --header "Host: $API_HOST" "http://127.0.0.1:${api_port}/health/ready" || true
curl --fail --show-error --header "Host: $API_HOST" "http://127.0.0.1:${api_port}/api/notebooks/public" || true
curl --fail --show-error --header "Host: $FRONTEND_HOST" "http://127.0.0.1:${frontend_port}/config.js" || true
curl --fail --show-error --header "Host: $FRONTEND_HOST" "http://127.0.0.1:${frontend_port}/api/notebooks/public" || true
exit 1
