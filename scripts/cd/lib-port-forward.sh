#!/usr/bin/env bash
# Shared kubectl port-forward helpers for CD scripts. Source this file from
# the calling script (it is copied to the remote deploy host alongside the
# scripts that use it). Callers must declare `frontend_pid` and `api_pid`
# (empty strings) and call init_port_forward_logs once the release name is
# known. Log paths default to /tmp with a PID suffix so concurrent
# deployments on the same host do not overwrite each other's logs.

pick_port() {
  python3 - <<'PY'
import socket
with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
    sock.bind(("127.0.0.1", 0))
    print(sock.getsockname()[1])
PY
}

init_port_forward_logs() {
  local release="$1"
  frontend_log="${FRONTEND_PORT_FORWARD_LOG:-/tmp/codecafe-${release}-frontend-port-forward-$$.log}"
  api_log="${API_PORT_FORWARD_LOG:-/tmp/codecafe-${release}-api-port-forward-$$.log}"
}

port_forward_cleanup() {
  local pids=()

  if [ -n "${frontend_pid:-}" ]; then
    pids+=("$frontend_pid")
  fi

  if [ -n "${api_pid:-}" ]; then
    pids+=("$api_pid")
  fi

  if [ "${#pids[@]}" -gt 0 ]; then
    kill "${pids[@]}" 2>/dev/null || true
    wait "${pids[@]}" 2>/dev/null || true
  fi
}

remove_port_forward_logs() {
  rm -f "${frontend_log:-}" "${api_log:-}"
}

dump_port_forward_logs() {
  echo "Frontend port-forward log:" >&2
  cat "${frontend_log:-}" >&2 || true
  echo "API port-forward log:" >&2
  cat "${api_log:-}" >&2 || true
}
