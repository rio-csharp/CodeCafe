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

KUBECTL_BIN="${KUBECTL_BIN:-kubectl}"
HELM_BIN="${HELM_BIN:-helm}"
VALUES_FILE="${VALUES_FILE:-}"
FRONTEND_REPLICA_COUNT="${FRONTEND_REPLICA_COUNT:-}"
API_REPLICA_COUNT="${API_REPLICA_COUNT:-}"
API_MIGRATION_ENABLED="${API_MIGRATION_ENABLED:-}"

cleanup() {
  rm -rf "$REMOTE_DIR"
}
trap cleanup EXIT

chart_dir="$REMOTE_DIR/codecafe"
api_config_secret="${RELEASE}-api-config"

tar -xzf "$REMOTE_DIR/codecafe-chart.tgz" -C "$chart_dir"

$KUBECTL_BIN create namespace "$NAMESPACE" --dry-run=client -o yaml | $KUBECTL_BIN apply -f -

tls_cert_file="$(mktemp)"
tls_key_file="$(mktemp)"
$KUBECTL_BIN get secret "$TLS_SECRET" --namespace codecafe-shared -o jsonpath='{.data.tls\.crt}' | base64 -d > "$tls_cert_file"
$KUBECTL_BIN get secret "$TLS_SECRET" --namespace codecafe-shared -o jsonpath='{.data.tls\.key}' | base64 -d > "$tls_key_file"
$KUBECTL_BIN create secret tls "$TLS_SECRET" \
  --cert="$tls_cert_file" \
  --key="$tls_key_file" \
  --namespace "$NAMESPACE" \
  --dry-run=client -o yaml | $KUBECTL_BIN apply -f -
rm -f "$tls_cert_file" "$tls_key_file"

umask 077
$KUBECTL_BIN get secret codecafe-db-secret \
  --namespace "$DB_SECRET_NAMESPACE" \
  -o jsonpath='{.data.ConnectionStrings__DefaultConnection}' \
  | base64 -d \
  | awk '{ printf "ConnectionStrings__DefaultConnection=%s\n", $0 }' > "$REMOTE_DIR/api.env"

$KUBECTL_BIN create secret generic "$api_config_secret" \
  --from-env-file="$REMOTE_DIR/api.env" \
  --namespace "$NAMESPACE" \
  --dry-run=client -o yaml | $KUBECTL_BIN apply -f -

helm_args=(
  upgrade --install "$RELEASE" "$chart_dir"
  --namespace "$NAMESPACE"
  --set "frontend.image.repository=$REGISTRY/$IMAGE_NAMESPACE/frontend"
  --set "frontend.image.tag=$IMAGE_TAG"
  --set "frontend.env.apiBaseUrl=https://$API_HOST"
  --set "frontend.ingress.host=$FRONTEND_HOST"
  --set "frontend.ingress.tls.secretName=$TLS_SECRET"
  --set "api.image.repository=$REGISTRY/$IMAGE_NAMESPACE/api"
  --set "api.image.tag=$IMAGE_TAG"
  --set "api.envFromSecrets[0]=$api_config_secret"
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

$HELM_BIN "${helm_args[@]}"

$KUBECTL_BIN rollout status deployment \
  --selector "app.kubernetes.io/instance=$RELEASE,app.kubernetes.io/component=frontend" \
  --namespace "$NAMESPACE" \
  --timeout=180s
$KUBECTL_BIN rollout status deployment \
  --selector "app.kubernetes.io/instance=$RELEASE,app.kubernetes.io/component=api" \
  --namespace "$NAMESPACE" \
  --timeout=180s

frontend_port=18080
api_port=18081
$KUBECTL_BIN port-forward "service/${RELEASE}-frontend" "${frontend_port}:80" --namespace "$NAMESPACE" >/tmp/codecafe-frontend-port-forward.log 2>&1 &
frontend_pid=$!
$KUBECTL_BIN port-forward "service/${RELEASE}-api" "${api_port}:80" --namespace "$NAMESPACE" >/tmp/codecafe-api-port-forward.log 2>&1 &
api_pid=$!

port_forward_cleanup() {
  kill "$frontend_pid" "$api_pid" 2>/dev/null || true
  wait "$frontend_pid" "$api_pid" 2>/dev/null || true
}
trap 'port_forward_cleanup; cleanup' EXIT

for _ in $(seq 1 20); do
  if curl --silent --fail "http://127.0.0.1:${frontend_port}/" >/dev/null \
    && curl --silent --fail "http://127.0.0.1:${api_port}/health/ready" >/dev/null; then
    exit 0
  fi
  sleep 3
done

curl --fail "http://127.0.0.1:${frontend_port}/"
curl --fail "http://127.0.0.1:${api_port}/health/ready"
