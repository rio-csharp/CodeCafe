# Test Environment Setup

This runbook documents how to configure the CodeCafe test environment from a
clean CentOS Stream 9 or Rocky Linux 9 server to a GitHub Actions deployment
target that supports PR previews and the permanent test environment.

Do not commit real IP addresses, domain names, SSH private keys, certificate
private keys, or local secrets. Keep them in ignored local files such as
`deploy/secrets.env`, `deploy/cloudflare/*.private.*`, or GitHub Secrets.

## Target Architecture

The test server is responsible for:

- Running a single-node k3s Kubernetes cluster
- Using the default k3s Traefik Ingress controller for HTTP and HTTPS traffic
- Receiving public traffic through Cloudflare
- Using a Cloudflare Origin Certificate for Cloudflare-to-origin HTTPS
- Allowing GitHub Actions to connect over SSH and run `kubectl` and `helm`
- Deploying each pull request into an isolated namespace, for example `codecafe-pr-123`
- Deploying the permanent test environment into `codecafe-test` after changes reach `main`

Hostnames follow this shape:

```text
<TEST_FRONTEND_HOST>                  Permanent test frontend
<TEST_API_HOST>                       Permanent test API
pr-<number>.<PREVIEW_BASE_DOMAIN>     PR preview frontend
api-pr-<number>.<PREVIEW_BASE_DOMAIN> PR preview API
```

To avoid paid Total TLS, use first-level subdomains for the test API and PR
previews. Avoid multi-level hostnames such as `api.test.<domain>` or
`pr-123.test.<domain>`.

## 1. Prepare the Server

Update the system and reboot once:

```bash
sudo dnf update -y
sudo reboot
```

Check the operating system version:

```bash
cat /etc/os-release
uname -a
```

## 2. Configure SSH

Use SSH key authentication for remote access. Add your local public key to the
server:

```bash
mkdir -p ~/.ssh
chmod 700 ~/.ssh
vi ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
```

After key login works, disable remote password login:

```bash
sudo vi /etc/ssh/sshd_config
```

Recommended settings:

```text
PubkeyAuthentication yes
PasswordAuthentication no
PermitRootLogin prohibit-password
```

Restart SSH:

```bash
sudo systemctl restart sshd
```

This only affects remote SSH login. Local login through VNC or the cloud
provider console can still use the server account password.

## 3. Install k3s

Install k3s:

```bash
curl -sfL https://get.k3s.io | sh -
```

Verify the node:

```bash
sudo k3s kubectl get nodes -o wide
```

k3s normally creates these commands:

```bash
which k3s
which kubectl
which crictl
```

If the current user should run `kubectl` directly, copy the kubeconfig:

```bash
mkdir -p ~/.kube
sudo cp /etc/rancher/k3s/k3s.yaml ~/.kube/config
sudo chown "$USER:$USER" ~/.kube/config
chmod 600 ~/.kube/config
kubectl get nodes
```

## 4. Install Helm

The deployment workflows run `helm upgrade --install` on the target server, so
Helm must be installed there.

```bash
curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash
helm version --short
```

## 5. Create the Deploy User

Do not let GitHub Actions deploy as root. Create a dedicated deploy user:

```bash
sudo useradd -m -s /bin/bash deploy
sudo passwd deploy
```

Set a random strong password so the account is not locked. Remote deployment
still uses SSH key authentication.

Generate a dedicated deploy key locally:

```powershell
ssh-keygen -t ed25519 -f .local-keys\codecafe_test_deploy_user_ed25519 -C "codecafe-test-deploy"
```

Add the public key to the server:

```bash
sudo mkdir -p /home/deploy/.ssh
sudo vi /home/deploy/.ssh/authorized_keys
sudo chown -R deploy:deploy /home/deploy/.ssh
sudo chmod 700 /home/deploy/.ssh
sudo chmod 600 /home/deploy/.ssh/authorized_keys
```

## 6. Configure kubectl for the Deploy User

Copy the k3s kubeconfig to the deploy user:

```bash
sudo mkdir -p /home/deploy/.kube
sudo cp /etc/rancher/k3s/k3s.yaml /home/deploy/.kube/config
sudo chown -R deploy:deploy /home/deploy/.kube
sudo chmod 600 /home/deploy/.kube/config
```

Set the deploy user's environment:

```bash
sudo tee -a /home/deploy/.bashrc >/dev/null <<'EOF'
export KUBECONFIG=/home/deploy/.kube/config
export PATH=/usr/local/bin:/usr/bin:/bin:/usr/local/sbin:/usr/sbin:/sbin:$PATH
EOF
sudo chown deploy:deploy /home/deploy/.bashrc
```

Verify the deploy user from your local machine:

```powershell
ssh -i .local-keys\codecafe_test_deploy_user_ed25519 -p <TEST_SSH_PORT> deploy@<TEST_SSH_HOST> `
  "PATH=/usr/local/bin:/usr/bin:/bin:/usr/local/sbin:/usr/sbin:/sbin; whoami; kubectl get nodes; helm version --short"
```

Verify cluster permissions:

```bash
kubectl auth can-i create namespaces
kubectl auth can-i get secrets --namespace codecafe-shared
kubectl auth can-i create deployments --all-namespaces
```

The current test setup uses the k3s admin kubeconfig for the deploy user, so it
has cluster-admin permissions. Later, this can be tightened with a dedicated
ServiceAccount and RBAC.

## 7. Configure Cloudflare DNS

Cloudflare DNS can be imported with a zone file. The committed template is:

```text
deploy/cloudflare/zone.example.bind
```

The real local import file can be stored at:

```text
deploy/cloudflare/zone.private.bind
```

That file is ignored by Git.

The test environment needs these records, and they should be proxied:

```text
<TEST_FRONTEND_HOST>      A      <test-server-ip>    Proxied
<TEST_API_HOST>           A      <test-server-ip>    Proxied
*.<PREVIEW_BASE_DOMAIN>   A      <test-server-ip>    Proxied
```

If production also runs on Kubernetes, point the production frontend and API
records to the production server.

## 8. Configure Cloudflare SSL/TLS

Recommended Cloudflare settings:

```text
SSL/TLS encryption mode: Full (strict)
Always Use HTTPS: On
TLS 1.3: On
Minimum TLS Version: TLS 1.2 or higher
```

Do not use `Flexible`, because it makes Cloudflare connect to the origin over
HTTP.

Universal SSL normally covers the root domain and first-level subdomains. To
avoid paid Total TLS, use hostnames like:

```text
test.<domain>
test-api.<domain>
pr-123.<domain>
api-pr-123.<domain>
```

Avoid hostnames like:

```text
api.test.<domain>
pr-123.test.<domain>
```

## 9. Create a Cloudflare Origin Certificate

In the Cloudflare dashboard:

```text
SSL/TLS -> Origin Server -> Create Certificate
```

Recommended options:

```text
Generate private key and CSR with Cloudflare
Private key type: ECC
Hostnames: *.<PREVIEW_BASE_DOMAIN>
```

Save the certificate and key in ignored local files:

```text
deploy/cloudflare/cloudflare-origin.private.pem
deploy/cloudflare/cloudflare-origin.private.key
```

Import the certificate into k3s:

```bash
kubectl create namespace codecafe-shared --dry-run=client -o yaml | kubectl apply -f -

kubectl create secret tls codecafe-test-wildcard-tls \
  --cert=cloudflare-origin.private.pem \
  --key=cloudflare-origin.private.key \
  --namespace codecafe-shared \
  --dry-run=client -o yaml | kubectl apply -f -
```

Verify the secret:

```bash
kubectl get secret codecafe-test-wildcard-tls -n codecafe-shared -o wide
```

GitHub Actions copies this shared secret into `codecafe-test` and each
`codecafe-pr-*` namespace during deployment.

## 10. Configure GitHub Variables and Secrets

Repository variable:

```text
IMAGE_NAMESPACE
```

Test environment repository secrets:

```text
TEST_FRONTEND_HOST
TEST_API_HOST
PREVIEW_BASE_DOMAIN
TEST_SSH_HOST
TEST_SSH_PORT
TEST_SSH_USER
TEST_SSH_PRIVATE_KEY
```

Production uses the same SSH deployment model. Production repository secrets:

```text
PRODUCTION_FRONTEND_HOST
PRODUCTION_API_HOST
PRODUCTION_SSH_HOST
PRODUCTION_SSH_PORT
PRODUCTION_SSH_USER
PRODUCTION_SSH_PRIVATE_KEY
```

Set them locally with `gh`:

```powershell
gh variable set IMAGE_NAMESPACE --body "<owner-or-org>/codecafe"

gh secret set TEST_FRONTEND_HOST --body "<test-frontend-host>"
gh secret set TEST_API_HOST --body "<test-api-host>"
gh secret set PREVIEW_BASE_DOMAIN --body "<preview-base-domain>"
gh secret set TEST_SSH_HOST --body "<test-server-ip>"
gh secret set TEST_SSH_PORT --body "<ssh-port>"
gh secret set TEST_SSH_USER --body "deploy"
Get-Content .local-keys\codecafe_test_deploy_user_ed25519 | gh secret set TEST_SSH_PRIVATE_KEY

gh secret set PRODUCTION_FRONTEND_HOST --body "<production-frontend-host>"
gh secret set PRODUCTION_API_HOST --body "<production-api-host>"
gh secret set PRODUCTION_SSH_HOST --body "<production-server-ip>"
gh secret set PRODUCTION_SSH_PORT --body "<ssh-port>"
gh secret set PRODUCTION_SSH_USER --body "deploy"
Get-Content .local-keys\codecafe_production_deploy_user_ed25519 | gh secret set PRODUCTION_SSH_PRIVATE_KEY
```

Check the configured names:

```powershell
gh variable list
gh secret list
```

## 11. Firewall Recommendations

Minimum access:

```text
SSH port: allow only your own IP address or trusted sources
80/tcp: allow only Cloudflare IP ranges
443/tcp: allow only Cloudflare IP ranges
```

With firewalld, use rich rules to restrict ports 80 and 443 to Cloudflare IP
ranges. Cloudflare IP ranges can change, so a production-grade setup should sync
them from Cloudflare's official list.

Do not expose the Kubernetes API on `6443` to the public internet. The current
deployment model connects over SSH and runs `kubectl` on the target server, so
GitHub Actions does not need direct Kubernetes API access.

## 12. Verify the Deployment Path

Verify the server basics:

```powershell
ssh -i .local-keys\codecafe_test_deploy_user_ed25519 -p <TEST_SSH_PORT> deploy@<TEST_SSH_HOST> `
  "PATH=/usr/local/bin:/usr/bin:/bin:/usr/local/sbin:/usr/sbin:/sbin; kubectl get nodes; helm list --all-namespaces"
```

Verify the shared TLS secret:

```bash
kubectl get namespace codecafe-shared
kubectl get secret codecafe-test-wildcard-tls -n codecafe-shared
```

Trigger a PR preview by opening or updating a pull request. The CI workflow runs
backend and frontend checks on every push, so each commit gets a GitHub check
status. If the pushed branch has an open PR, CI uploads API and frontend build
artifacts after those checks pass. The PR image jobs download those artifacts
and package them into thin runtime images in parallel. After image publishing
finishes, CI triggers a separate PR preview deployment workflow run. The
deployment workflow updates a PR comment with preview URL formats only; it does
not expose the real preview base domain.

Expected result:

```text
namespace: codecafe-pr-<number>
frontend:  https://pr-<number>.<PREVIEW_BASE_DOMAIN>
api:       https://api-pr-<number>.<PREVIEW_BASE_DOMAIN>
```

Trigger the permanent test environment by merging to `main`, or by manually
running `Deploy Test` with a branch, tag, or commit SHA.

Expected result:

```text
namespace: codecafe-test
frontend:  https://<TEST_FRONTEND_HOST>
api:       https://<TEST_API_HOST>
```

## 13. Troubleshooting

If GitHub Actions can SSH into the server but cannot find commands, check PATH:

```bash
echo $PATH
command -v kubectl
command -v helm
```

The workflow remote scripts explicitly set a standard PATH:

```bash
export PATH=/usr/local/bin:/usr/bin:/bin:/usr/local/sbin:/usr/sbin:/sbin
```

If HTTPS returns a Cloudflare 526 error, the origin certificate is usually
missing, expired, or not trusted by Cloudflare. Check:

```bash
kubectl get secret codecafe-test-wildcard-tls -n codecafe-shared
```

If a PR hostname has a certificate mismatch, confirm the hostname does not use a
multi-level subdomain and that the Cloudflare DNS record is proxied.

If the workflow cannot find the TLS secret, recreate it:

```bash
kubectl create namespace codecafe-shared --dry-run=client -o yaml | kubectl apply -f -
kubectl create secret tls codecafe-test-wildcard-tls \
  --cert=cloudflare-origin.private.pem \
  --key=cloudflare-origin.private.key \
  --namespace codecafe-shared \
  --dry-run=client -o yaml | kubectl apply -f -
```
