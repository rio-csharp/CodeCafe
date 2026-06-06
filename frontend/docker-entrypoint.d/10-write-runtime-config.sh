#!/bin/sh
set -eu

json_string() {
  printf '%s' "$1" | awk '
    BEGIN { ORS = ""; print "\"" }
    {
      gsub(/\\/, "\\\\")
      gsub(/"/, "\\\"")
      gsub(/\r/, "\\r")
      gsub(/\t/, "\\t")
      if (NR > 1) print "\\n"
      printf "%s", $0
    }
    END { print "\"" }
  '
}

api_base_url_json=$(json_string "${API_BASE_URL:-http://localhost:8080}")

cat > /usr/share/nginx/html/config.js <<EOF
window.__CODECAFE_CONFIG__ = {
  apiBaseUrl: ${api_base_url_json}
};
EOF

if ! grep -q '/config.js' /usr/share/nginx/html/index.html; then
  sed -i 's#<script type="module"#<script src="/config.js"></script><script type="module"#' /usr/share/nginx/html/index.html
fi
