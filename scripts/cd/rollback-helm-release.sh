#!/usr/bin/env bash
set -euo pipefail

require_env() {
  local name="$1"
  if [ -z "${!name:-}" ]; then
    echo "Missing required environment variable: $name" >&2
    exit 1
  fi
}

for name in NAMESPACE RELEASE; do
  require_env "$name"
done

export PATH=/usr/local/bin:/usr/bin:/bin:/usr/local/sbin:/usr/sbin:/sbin

HELM_BIN="${HELM_BIN:-helm}"
KUBECTL_BIN="${KUBECTL_BIN:-kubectl}"
HELM_TIMEOUT="${HELM_TIMEOUT:-180s}"
REVISION="${REVISION:-}"

if [ -n "$REVISION" ] && ! [[ "$REVISION" =~ ^[0-9]+$ ]]; then
  echo "REVISION must be numeric when provided." >&2
  exit 1
fi

$HELM_BIN history "$RELEASE" --namespace "$NAMESPACE"

if [ -n "$REVISION" ]; then
  $HELM_BIN rollback "$RELEASE" "$REVISION" --namespace "$NAMESPACE" --wait --timeout "$HELM_TIMEOUT"
else
  $HELM_BIN rollback "$RELEASE" --namespace "$NAMESPACE" --wait --timeout "$HELM_TIMEOUT"
fi

$KUBECTL_BIN rollout status deployment \
  --selector "app.kubernetes.io/instance=$RELEASE,app.kubernetes.io/component=frontend" \
  --namespace "$NAMESPACE" \
  --timeout="$HELM_TIMEOUT"
$KUBECTL_BIN rollout status deployment \
  --selector "app.kubernetes.io/instance=$RELEASE,app.kubernetes.io/component=api" \
  --namespace "$NAMESPACE" \
  --timeout="$HELM_TIMEOUT"

$HELM_BIN history "$RELEASE" --namespace "$NAMESPACE"
