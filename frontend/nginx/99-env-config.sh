#!/bin/sh
set -eu

CONFIG_PATH="/usr/share/nginx/html/assets/config.json"

mkdir -p "$(dirname "$CONFIG_PATH")"

cat > "$CONFIG_PATH" <<EOF
{
  "apiBaseUrl": "${API_BASE_URL:-/api}",
  "keycloak": {
    "url": "${KEYCLOAK_URL:-http://localhost:8081}",
    "realm": "${KEYCLOAK_REALM:-medcarehub}",
    "clientId": "${KEYCLOAK_CLIENT_ID:-medcarehub-web}"
  }
}
EOF

echo "Generated runtime config at ${CONFIG_PATH}:"
cat "$CONFIG_PATH"