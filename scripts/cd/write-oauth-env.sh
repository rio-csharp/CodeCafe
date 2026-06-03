#!/usr/bin/env bash
set -euo pipefail

for var_name in OAUTH_CERT_BASE64 OAUTH_CERT_PASSWORD; do
  if [ -z "${!var_name:-}" ]; then
    echo "Missing required environment variable: $var_name" >&2
    exit 1
  fi
done

oauth_env_file="${OAUTH_ENV_FILE:-.artifacts/deployment/oauth.env}"
mkdir -p "$(dirname "$oauth_env_file")"

umask 077
{
  printf 'OAUTH_CERT_BASE64=%q\n' "$OAUTH_CERT_BASE64"
  printf 'OAUTH_CERT_PASSWORD=%q\n' "$OAUTH_CERT_PASSWORD"
} > "$oauth_env_file"
