#!/usr/bin/env bash
set -euo pipefail

event_name="${EVENT_NAME:?EVENT_NAME is required}"
ref_name="${REF_NAME:-}"
base_ref="${BASE_REF:-}"
repository="${REPOSITORY:?REPOSITORY is required}"
pr_number="${PR_NUMBER:-}"
is_fork="${IS_FORK:-false}"
github_output="${GITHUB_OUTPUT:?GITHUB_OUTPUT is required}"
short_sha="${GITHUB_SHA:?GITHUB_SHA is required}"
short_sha="${short_sha::7}"

upload_artifacts=false
image_tag="manual-$short_sha"

if [ "$event_name" = "push" ] && [ "$ref_name" = "main" ]; then
  upload_artifacts=true
  image_tag="production-$short_sha"
fi

if [ "$event_name" = "push" ] && [[ "$ref_name" == release/* ]]; then
  upload_artifacts=true
  image_tag="test-$short_sha"
fi

if [ "$event_name" = "push" ] && [[ "$ref_name" == feature/* ]]; then
  owner="${repository%%/*}"
  pr_number="$(gh api "repos/$repository/pulls?state=open&head=$owner:$ref_name" \
    --jq 'map(select(.base.ref | startswith("release/")))[0].number // ""')"

  if [ -n "$pr_number" ]; then
    upload_artifacts=true
    image_tag="pr-$pr_number-$short_sha"
  fi
fi

if [ "$event_name" = "pull_request" ] && [ -n "$pr_number" ] && [ "$is_fork" != "true" ]; then
  if [[ "$base_ref" == release/* ]]; then
    upload_artifacts=true
    image_tag="pr-$pr_number-$short_sha"
  fi
fi

if [ "$event_name" = "workflow_dispatch" ]; then
  upload_artifacts=true
fi

{
  echo "upload_artifacts=$upload_artifacts"
  echo "image_tag=$image_tag"
} >> "$github_output"
