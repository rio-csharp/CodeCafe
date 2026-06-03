#!/usr/bin/env bash
set -euo pipefail

export PATH=/usr/local/bin:/usr/bin:/bin:/usr/local/sbin:/usr/sbin:/sbin
export KUBECONFIG="${KUBECONFIG:-/etc/rancher/k3s/k3s.yaml}"

remote_dump="${REMOTE_DUMP:?REMOTE_DUMP is required}"
namespace="${NAMESPACE:-codecafe-test}"
release="${RELEASE:-codecafe-test}"
backup_dir="${TEST_DB_BACKUP_DIR:-/opt/backup/postgres}"

export PGPASSFILE="${PGPASSFILE:-/root/.pgpass}"

timestamp="$(date -u +%Y%m%d_%H%M%S)"
backup_file="${backup_dir}/codecafe_test_before_prod_sync_${timestamp}.dump"

cleanup() {
  rm -f "$remote_dump"
}
trap cleanup EXIT

test -f "$remote_dump"
mkdir -p "$backup_dir"

pg_dump -h localhost -U codecafe -d codecafe -Fc -f "$backup_file"
psql -h localhost -U codecafe -d postgres -v ON_ERROR_STOP=1 \
  -c "select pg_terminate_backend(pid) from pg_stat_activity where datname = 'codecafe' and pid <> pg_backend_pid();"
dropdb -h localhost -U codecafe --if-exists codecafe
createdb -h localhost -U codecafe codecafe
pg_restore -h localhost -U codecafe -d codecafe --no-owner --no-privileges "$remote_dump"
rm -f "$remote_dump"
trap - EXIT

deployment="${release}-api"
if ! kubectl get deployment "$deployment" --namespace "$namespace" >/dev/null 2>&1; then
  echo "No test API deployment found; skipping post-sync migration."
  echo "Previous test backup: $backup_file"
  exit 0
fi

image="$(kubectl get deployment "$deployment" --namespace "$namespace" -o jsonpath='{.spec.template.spec.containers[?(@.name=="api")].image}')"
secret="${release}-api-config"
if ! kubectl get secret "$secret" --namespace "$namespace" >/dev/null 2>&1; then
  secret="codecafe-db-secret"
fi

job="${release}-post-sync-migrate-$(date -u +%Y%m%d%H%M%S)"
cat <<EOF | kubectl apply -f -
apiVersion: batch/v1
kind: Job
metadata:
  name: ${job}
  namespace: ${namespace}
spec:
  backoffLimit: 1
  template:
    spec:
      restartPolicy: Never
      containers:
        - name: api-migrate
          image: ${image}
          envFrom:
            - secretRef:
                name: ${secret}
          command:
            - dotnet
            - CodeCafe.Server.dll
            - migrate
EOF

if ! kubectl wait --for=condition=complete "job/$job" --namespace "$namespace" --timeout=180s; then
  kubectl logs "job/$job" --namespace "$namespace" || true
  kubectl delete job "$job" --namespace "$namespace" --ignore-not-found
  exit 1
fi

kubectl delete job "$job" --namespace "$namespace" --ignore-not-found
echo "Test database restored from production. Previous test backup: $backup_file"
