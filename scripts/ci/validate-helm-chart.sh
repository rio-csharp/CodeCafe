#!/usr/bin/env bash
# Validates the Helm chart without touching a cluster. Runnable from anywhere:
# the repository root is resolved from this script's own location.
set -euo pipefail

chart_dir="${CHART_DIR:-deploy/helm/codecafe}"
helm_bin="${HELM_BIN:-helm}"
# The chart intentionally requires explicit image tags, and validation does not
# deploy, so a throwaway tag is enough.
image_tag="${VALIDATION_IMAGE_TAG:-ci}"

if ! command -v "$helm_bin" >/dev/null 2>&1; then
  echo "helm is required but was not found on PATH." >&2
  exit 1
fi

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

if [ ! -d "$chart_dir" ]; then
  echo "Chart directory not found: $chart_dir" >&2
  exit 1
fi

render() {
  local release="$1"
  shift
  echo "helm template $release $*"
  "$helm_bin" template "$release" "$chart_dir" "$@" \
    --set "frontend.image.tag=$image_tag" \
    --set "api.image.tag=$image_tag" >/dev/null
}

echo "helm lint $chart_dir"
"$helm_bin" lint "$chart_dir" \
  --set "frontend.image.tag=$image_tag" \
  --set "api.image.tag=$image_tag"

# One render per environment, because a values file that only breaks when
# combined with the chart is exactly the failure this step exists to catch —
# rendering the defaults alone would miss it. Add a line when a values file
# appears.
render codecafe
render codecafe-test --values "$chart_dir/values-test.yaml"
render codecafe-prod --values "$chart_dir/values-prod.yaml"
# The runtime-config path sits behind a value the environment renders leave empty,
# so it needs a render of its own: otherwise a broken frontend-configmap template
# or a bad checksum reference would only surface midway through a real deploy.
render codecafe --set "frontend.config.apiBaseUrl=https://api.example.test"

echo "Helm chart validated."
