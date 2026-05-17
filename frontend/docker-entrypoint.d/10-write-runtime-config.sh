#!/bin/sh
set -eu

cat > /usr/share/nginx/html/config.js <<EOF
window.__CODECAFE_CONFIG__ = {
  apiBaseUrl: "${API_BASE_URL:-http://localhost:8080}"
};
EOF

if ! grep -q '/config.js' /usr/share/nginx/html/index.html; then
  sed -i 's#<script type="module"#<script src="/config.js"></script><script type="module"#' /usr/share/nginx/html/index.html
fi
