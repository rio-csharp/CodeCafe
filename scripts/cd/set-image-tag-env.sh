#!/usr/bin/env bash
set -euo pipefail

if [ -z "${GITHUB_ENV:-}" ]; then
  echo "Missing required environment variable: GITHUB_ENV" >&2
  exit 1
fi

deployment_env_file="${DEPLOYMENT_ENV_FILE:-.artifacts/deployment/deployment.env}"

if [ ! -f "$deployment_env_file" ]; then
  echo "Deployment metadata file not found: $deployment_env_file" >&2
  exit 1
fi

# shellcheck disable=SC1090
. "$deployment_env_file"

if [ -z "${IMAGE_TAG:-}" ]; then
  echo "IMAGE_TAG is missing from $deployment_env_file" >&2
  exit 1
fi

echo "IMAGE_TAG=$IMAGE_TAG" >> "$GITHUB_ENV"
