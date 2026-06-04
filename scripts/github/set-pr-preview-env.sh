#!/usr/bin/env bash
set -euo pipefail

for var_name in GITHUB_ENV PR_NUMBER PREVIEW_BASE_DOMAIN; do
  if [ -z "${!var_name:-}" ]; then
    echo "Missing required environment variable: $var_name" >&2
    exit 1
  fi
done

if [[ ! "$PR_NUMBER" =~ ^[0-9]+$ ]]; then
  echo "PR_NUMBER must be numeric." >&2
  exit 1
fi

deployment_env_file="${DEPLOYMENT_ENV_FILE:-.artifacts/deployment/deployment.env}"
namespace_prefix="${NAMESPACE_PREFIX:-codecafe-pr}"

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

{
  echo "IMAGE_TAG=$IMAGE_TAG"
  echo "NAMESPACE=${namespace_prefix}-${PR_NUMBER}"
  echo "RELEASE=codecafe-pr-${PR_NUMBER}"
  echo "FRONTEND_HOST=pr-${PR_NUMBER}.${PREVIEW_BASE_DOMAIN}"
  echo "API_HOST=api-pr-${PR_NUMBER}.${PREVIEW_BASE_DOMAIN}"
} >> "$GITHUB_ENV"
