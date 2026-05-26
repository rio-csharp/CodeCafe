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
OAUTH_SECRET_NAME="${OAUTH_SECRET_NAME:-codecafe-oauth-secret}"
OAUTH_SECRET_NAMESPACE="${OAUTH_SECRET_NAMESPACE:-$NAMESPACE}"
OAUTH_ENV_FILE="${OAUTH_ENV_FILE:-}"

cleanup() {
  rm -rf "$REMOTE_DIR"
}
trap cleanup EXIT

if [ -n "$OAUTH_ENV_FILE" ]; then
  # shellcheck disable=SC1090
  . "$OAUTH_ENV_FILE"
fi

append_secret_key() {
  local secret_name="$1"
  local secret_namespace="$2"
  local key="$3"
  local required="${4:-true}"
  local encoded
  encoded="$($KUBECTL_BIN get secret "$secret_name" --namespace "$secret_namespace" -o "jsonpath={.data.$key}")"

  if [ -z "$encoded" ]; then
    if [ "$required" = "true" ]; then
      echo "Missing required secret key '$key' in $secret_namespace/$secret_name" >&2
      exit 1
    fi
    return
  fi

  printf '%s=%s\n' "$key" "$(printf '%s' "$encoded" | base64 -d)" >> "$REMOTE_DIR/api.env"
}

seed_oauth_secret() {
  if [ -z "${OAUTH_CERT_BASE64:-}" ]; then
    if [ -n "${OAUTH_CERT_PASSWORD:-}" ]; then
      echo "OAuth secret injection is partial. OAUTH_CERT_PASSWORD is set but OAUTH_CERT_BASE64 is missing." >&2
      exit 1
    fi
    return
  fi

  $KUBECTL_BIN create secret generic "$OAUTH_SECRET_NAME" \
    --from-literal=AuthorizationServer__SigningCertificateBase64="$OAUTH_CERT_BASE64" \
    --from-literal=AuthorizationServer__SigningCertificatePassword="$OAUTH_CERT_PASSWORD" \
    --from-literal=AuthorizationServer__EncryptionCertificateBase64="$OAUTH_CERT_BASE64" \
    --from-literal=AuthorizationServer__EncryptionCertificatePassword="$OAUTH_CERT_PASSWORD" \
    --namespace "$OAUTH_SECRET_NAMESPACE" \
    --dry-run=client -o yaml | $KUBECTL_BIN apply -f -
}

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

seed_oauth_secret

umask 077
$KUBECTL_BIN get secret codecafe-db-secret \
  --namespace "$DB_SECRET_NAMESPACE" \
  -o jsonpath='{.data.ConnectionStrings__DefaultConnection}' \
  | base64 -d \
  | awk '{ printf "ConnectionStrings__DefaultConnection=%s\n", $0 }' > "$REMOTE_DIR/api.env"

cat <<EOF >> "$REMOTE_DIR/api.env"
AuthorizationServer__Issuer=https://$API_HOST/
AuthorizationServer__FrontendBaseUrl=https://$FRONTEND_HOST
AllowedHosts=$FRONTEND_HOST;$API_HOST
Mcp__Enabled=true
Mcp__AllowedOrigins__0=https://$FRONTEND_HOST
Mcp__RequireAuthorization=true
Mcp__RequiredAudience=codecafe-mcp
EOF

append_secret_key "$OAUTH_SECRET_NAME" "$OAUTH_SECRET_NAMESPACE" AuthorizationServer__SigningCertificateBase64
append_secret_key "$OAUTH_SECRET_NAME" "$OAUTH_SECRET_NAMESPACE" AuthorizationServer__SigningCertificatePassword false
append_secret_key "$OAUTH_SECRET_NAME" "$OAUTH_SECRET_NAMESPACE" AuthorizationServer__EncryptionCertificateBase64
append_secret_key "$OAUTH_SECRET_NAME" "$OAUTH_SECRET_NAMESPACE" AuthorizationServer__EncryptionCertificatePassword false

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

pick_port() {
  python3 - <<'PY'
import socket
with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
    sock.bind(("127.0.0.1", 0))
    print(sock.getsockname()[1])
PY
}

dump_port_forward_logs() {
  echo "Frontend port-forward log:" >&2
  cat /tmp/codecafe-frontend-port-forward.log >&2 || true
  echo "API port-forward log:" >&2
  cat /tmp/codecafe-api-port-forward.log >&2 || true
}

frontend_port="$(pick_port)"
api_port="$(pick_port)"
$KUBECTL_BIN port-forward --address 127.0.0.1 "service/${RELEASE}-frontend" "${frontend_port}:80" --namespace "$NAMESPACE" >/tmp/codecafe-frontend-port-forward.log 2>&1 &
frontend_pid=$!
$KUBECTL_BIN port-forward --address 127.0.0.1 "service/${RELEASE}-api" "${api_port}:80" --namespace "$NAMESPACE" >/tmp/codecafe-api-port-forward.log 2>&1 &
api_pid=$!

port_forward_cleanup() {
  kill "$frontend_pid" "$api_pid" 2>/dev/null || true
  wait "$frontend_pid" "$api_pid" 2>/dev/null || true
}
trap 'port_forward_cleanup; cleanup' EXIT

for _ in $(seq 1 20); do
  if ! kill -0 "$frontend_pid" 2>/dev/null || ! kill -0 "$api_pid" 2>/dev/null; then
    dump_port_forward_logs
    exit 1
  fi

  if curl --silent --fail --header "Host: $FRONTEND_HOST" "http://127.0.0.1:${frontend_port}/" >/dev/null \
    && curl --silent --fail --header "Host: $API_HOST" "http://127.0.0.1:${api_port}/health/ready" >/dev/null \
    && curl --silent --fail --header "Host: $API_HOST" "http://127.0.0.1:${api_port}/.well-known/oauth-protected-resource/mcp" >/dev/null; then
    exit 0
  fi
  sleep 3
done

dump_port_forward_logs
curl --fail --header "Host: $FRONTEND_HOST" "http://127.0.0.1:${frontend_port}/"
curl --fail --header "Host: $API_HOST" "http://127.0.0.1:${api_port}/health/ready"
curl --fail --header "Host: $API_HOST" "http://127.0.0.1:${api_port}/.well-known/oauth-protected-resource/mcp"
