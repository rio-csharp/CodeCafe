#!/usr/bin/env bash
set -euo pipefail

connection_string="${E2E_CONNECTION_STRING:-Host=localhost;Database=codecafe_e2e;Username=codecafe;Password=codecafe}"
backend_url="${E2E_BACKEND_URL:-http://localhost:5042}"
ready_url="${backend_url%/}/health/ready"
api_dll="${E2E_API_DLL:-.artifacts/api/CodeCafe.Server.dll}"
backend_log="${E2E_BACKEND_LOG:-.artifacts/e2e/backend.log}"
frontend_dir="${E2E_FRONTEND_DIR:-clients/web}"
max_attempts="${E2E_BACKEND_READY_ATTEMPTS:-30}"
sleep_seconds="${E2E_BACKEND_READY_SLEEP_SECONDS:-4}"
backend_pid=""

if [ ! -f "$api_dll" ]; then
  echo "API DLL not found: $api_dll" >&2
  exit 1
fi

if [ ! -d "$frontend_dir" ]; then
  echo "Frontend directory not found: $frontend_dir" >&2
  exit 1
fi

if ! [[ "$max_attempts" =~ ^[0-9]+$ ]] || [ "$max_attempts" -lt 1 ]; then
  echo "E2E_BACKEND_READY_ATTEMPTS must be a positive integer." >&2
  exit 1
fi

if ! [[ "$sleep_seconds" =~ ^[0-9]+$ ]] || [ "$sleep_seconds" -lt 1 ]; then
  echo "E2E_BACKEND_READY_SLEEP_SECONDS must be a positive integer." >&2
  exit 1
fi

mkdir -p "$(dirname "$backend_log")"

cleanup() {
  if [ -n "${backend_pid:-}" ]; then
    kill "$backend_pid" 2>/dev/null || true
    wait "$backend_pid" 2>/dev/null || true
  fi
}

on_signal() {
  local signal_name="$1"
  echo "Release E2E interrupted by $signal_name." >&2
  exit 130
}

trap cleanup EXIT
trap 'on_signal INT' INT
trap 'on_signal TERM' TERM
trap 'on_signal HUP' HUP

# Registration is disabled by default in appsettings.json. Locally it is turned
# back on in appsettings.Development.json, but that file is gitignored, so on CI
# the flag has to be supplied here or auth.setup.ts cannot create its account.
ConnectionStrings__DefaultConnection="$connection_string" \
ASPNETCORE_URLS="$backend_url" \
ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}" \
Auth__RegistrationEnabled=true \
dotnet "$api_dll" > "$backend_log" 2>&1 &
backend_pid=$!

for i in $(seq 1 "$max_attempts"); do
  if ! kill -0 "$backend_pid" 2>/dev/null; then
    echo "Backend exited before becoming ready."
    cat "$backend_log"
    exit 1
  fi

  if curl -sf "$ready_url" >/dev/null; then
    echo "Backend is ready."
    (cd "$frontend_dir" && npx playwright test)
    exit 0
  fi

  echo "Waiting for backend... ($i/$max_attempts)"
  sleep "$sleep_seconds"
done

echo "Backend did not become ready in time."
cat "$backend_log"
exit 1
