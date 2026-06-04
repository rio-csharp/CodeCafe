#!/usr/bin/env bash
set -euo pipefail

image_tag="${IMAGE_TAG:?IMAGE_TAG is required}"
git_sha="${GIT_SHA:?GIT_SHA is required}"
git_ref="${GIT_REF:?GIT_REF is required}"
output_dir="${DEPLOYMENT_METADATA_DIR:-.artifacts/deployment}"

mkdir -p "$output_dir"

{
  echo "IMAGE_TAG=$image_tag"
  echo "GIT_SHA=$git_sha"
  echo "GIT_REF=$git_ref"
} > "$output_dir/deployment.env"
