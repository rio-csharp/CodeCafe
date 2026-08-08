#!/usr/bin/env bash
set -euo pipefail

require_env() {
  local name="$1"
  if [ -z "${!name:-}" ]; then
    echo "Missing required environment variable: $name" >&2
    exit 1
  fi
}

for name in NAMESPACE RELEASE FRONTEND_HOST API_HOST; do
  require_env "$name"
done

export PATH=/usr/local/bin:/usr/bin:/bin:/usr/local/sbin:/usr/sbin:/sbin

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
. "$script_dir/lib-port-forward.sh"

KUBECTL_BIN="${KUBECTL_BIN:-kubectl}"
SMOKE_TEST_ATTEMPTS="${SMOKE_TEST_ATTEMPTS:-20}"
SMOKE_TEST_DELAY_SECONDS="${SMOKE_TEST_DELAY_SECONDS:-3}"

if ! [[ "$SMOKE_TEST_ATTEMPTS" =~ ^[0-9]+$ ]] || [ "$SMOKE_TEST_ATTEMPTS" -lt 1 ]; then
  echo "SMOKE_TEST_ATTEMPTS must be a positive integer." >&2
  exit 1
fi

if ! [[ "$SMOKE_TEST_DELAY_SECONDS" =~ ^[0-9]+$ ]] || [ "$SMOKE_TEST_DELAY_SECONDS" -lt 1 ]; then
  echo "SMOKE_TEST_DELAY_SECONDS must be a positive integer." >&2
  exit 1
fi

frontend_pid=""
api_pid=""
init_port_forward_logs "$RELEASE"

cleanup() {
  port_forward_cleanup
  remove_port_forward_logs
}

on_signal() {
  local signal_name="$1"
  echo "Smoke test interrupted by $signal_name; cleaning up port-forward processes." >&2
  exit 130
}

trap cleanup EXIT
trap 'on_signal INT' INT
trap 'on_signal TERM' TERM
trap 'on_signal HUP' HUP

frontend_port="$(pick_port)"
api_port="$(pick_port)"

$KUBECTL_BIN port-forward --address 127.0.0.1 "service/${RELEASE}-frontend" "${frontend_port}:80" --namespace "$NAMESPACE" >"$frontend_log" 2>&1 &
frontend_pid=$!
$KUBECTL_BIN port-forward --address 127.0.0.1 "service/${RELEASE}-api" "${api_port}:80" --namespace "$NAMESPACE" >"$api_log" 2>&1 &
api_pid=$!

for _ in $(seq 1 "$SMOKE_TEST_ATTEMPTS"); do
  if ! kill -0 "$frontend_pid" 2>/dev/null || ! kill -0 "$api_pid" 2>/dev/null; then
    dump_port_forward_logs
    exit 1
  fi

  if curl --silent --fail --header "Host: $FRONTEND_HOST" "http://127.0.0.1:${frontend_port}/" >/dev/null \
    && curl --silent --fail --header "Host: $API_HOST" "http://127.0.0.1:${api_port}/health/ready" >/dev/null; then
    echo "Smoke test passed."
    exit 0
  fi

  sleep "$SMOKE_TEST_DELAY_SECONDS"
done

dump_port_forward_logs
curl --fail --show-error --header "Host: $FRONTEND_HOST" "http://127.0.0.1:${frontend_port}/" || true
curl --fail --show-error --header "Host: $API_HOST" "http://127.0.0.1:${api_port}/health/ready" || true
exit 1
