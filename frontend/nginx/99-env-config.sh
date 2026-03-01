#!/bin/sh
set -eu

CFG="/usr/share/nginx/html/assets/config.json"

mkdir -p "$(dirname "$CFG")"

API_BASE_URL="${API_BASE_URL:-/api}"
KEYCLOAK_URL="${KEYCLOAK_URL:-http://localhost:8081}"
KEYCLOAK_REALM="${KEYCLOAK_REALM:-medcarehub}"
KEYCLOAK_CLIENT_ID="${KEYCLOAK_CLIENT_ID:-medcarehub-web}"

cat > "$CFG" <<EOF
{
  "apiBaseUrl": "${API_BASE_URL}",
  "keycloak": {
    "url": "${KEYCLOAK_URL}",
    "realm": "${KEYCLOAK_REALM}",
    "clientId": "${KEYCLOAK_CLIENT_ID}"
  }
}
EOF

echo "[medcarehub-web] runtime config written: $CFG"
