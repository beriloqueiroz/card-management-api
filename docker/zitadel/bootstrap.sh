#!/bin/sh
# One-shot ZITADEL provisioning for the challenge, driven by the service
# account PAT created by FirstInstance. Idempotent: safe to re-run.
#
# Creates:
#   - project "cards-api"
#   - public OIDC app "swagger-ui" (authorization code + PKCE + refresh token)
#   - the three seed users (password documented in the README)
# Outputs (consumed by the API container):
#   - $OUT_DIR/project_id        -> JWT audience
#   - $OUT_DIR/swagger_client_id -> Swagger UI OAuth client
set -eu

BASE="${ZITADEL_BASE_URL:-http://auth.localhost:8080}"
# curl resolves *.localhost to loopback internally, bypassing the compose DNS
# alias; --connect-to keeps the Host header (ZITADEL routes by it) while the
# TCP connection goes to the zitadel service.
CONNECT_TO="${ZITADEL_CONNECT_TO:-auth.localhost:8080:zitadel:8080}"
PAT_FILE="${ZITADEL_PAT_FILE:-/zitadel/pat/pat.txt}"
OUT_DIR="${OUT_DIR:-/zitadel/out}"
PROJECT_NAME="${PROJECT_NAME:-cards-api}"
APP_NAME="${APP_NAME:-swagger-ui}"
SWAGGER_REDIRECT_URI="${SWAGGER_REDIRECT_URI:-http://localhost:8000/swagger/oauth2-redirect.html}"
USER_PASSWORD="${SEED_USER_PASSWORD:-Cards@2026!}"

echo "waiting for zitadel at $BASE ..."
# First init runs all ZITADEL migrations and can take a few minutes.
i=0
until curl -fsS -o /dev/null --connect-to "$CONNECT_TO" "$BASE/debug/ready"; do
  i=$((i + 1))
  if [ "$i" -gt 240 ]; then
    echo "zitadel did not become ready"
    exit 1
  fi
  sleep 2
done

until [ -s "$PAT_FILE" ]; do
  echo "waiting for PAT file $PAT_FILE ..."
  sleep 2
done
PAT="$(cat "$PAT_FILE")"

api() {
  method="$1"
  path="$2"
  body="${3:-}"
  if [ -n "$body" ]; then
    curl -fsS -X "$method" --connect-to "$CONNECT_TO" "$BASE$path" \
      -H "Authorization: Bearer $PAT" \
      -H "Content-Type: application/json" \
      -d "$body"
  else
    curl -fsS -X "$method" --connect-to "$CONNECT_TO" "$BASE$path" -H "Authorization: Bearer $PAT"
  fi
}

# --- project -----------------------------------------------------------------
PROJECT_ID="$(api POST /management/v1/projects/_search \
  '{"queries":[{"nameQuery":{"name":"'"$PROJECT_NAME"'","method":"TEXT_QUERY_METHOD_EQUALS"}}]}' \
  | jq -r '.result[0].id // empty')"

if [ -z "$PROJECT_ID" ]; then
  PROJECT_ID="$(api POST /management/v1/projects '{"name":"'"$PROJECT_NAME"'"}' | jq -r '.id')"
  echo "created project $PROJECT_NAME ($PROJECT_ID)"
else
  echo "project $PROJECT_NAME already exists ($PROJECT_ID)"
fi

# --- OIDC app (public client: authorization code + PKCE + refresh token) -----
CLIENT_ID="$(api POST "/management/v1/projects/$PROJECT_ID/apps/_search" \
  '{"queries":[{"nameQuery":{"name":"'"$APP_NAME"'","method":"TEXT_QUERY_METHOD_EQUALS"}}]}' \
  | jq -r '.result[0].oidcConfig.clientId // empty')"

if [ -z "$CLIENT_ID" ]; then
  CLIENT_ID="$(api POST "/management/v1/projects/$PROJECT_ID/apps/oidc" '{
    "name": "'"$APP_NAME"'",
    "redirectUris": ["'"$SWAGGER_REDIRECT_URI"'"],
    "responseTypes": ["OIDC_RESPONSE_TYPE_CODE"],
    "grantTypes": ["OIDC_GRANT_TYPE_AUTHORIZATION_CODE", "OIDC_GRANT_TYPE_REFRESH_TOKEN"],
    "appType": "OIDC_APP_TYPE_USER_AGENT",
    "authMethodType": "OIDC_AUTH_METHOD_TYPE_NONE",
    "accessTokenType": "OIDC_TOKEN_TYPE_JWT",
    "idTokenUserinfoAssertion": true,
    "devMode": true
  }' | jq -r '.clientId')"
  echo "created app $APP_NAME (clientId $CLIENT_ID)"
else
  echo "app $APP_NAME already exists (clientId $CLIENT_ID)"
fi

# --- seed users ----------------------------------------------------------------
create_user() {
  email="$1"
  first="$2"
  last="$3"

  existing="$(api POST /management/v1/users/_search \
    '{"queries":[{"emailQuery":{"emailAddress":"'"$email"'","method":"TEXT_QUERY_METHOD_EQUALS"}}]}' \
    | jq -r '.result[0].id // empty')"

  if [ -n "$existing" ]; then
    echo "user $email already exists ($existing)"
    return
  fi

  api POST /management/v1/users/human/_import '{
    "userName": "'"$email"'",
    "profile": {"firstName": "'"$first"'", "lastName": "'"$last"'"},
    "email": {"email": "'"$email"'", "isEmailVerified": true},
    "password": "'"$USER_PASSWORD"'",
    "passwordChangeRequired": false
  }' > /dev/null
  echo "created user $email"
}

create_user "mariana.alves@cardcorp.test" "Mariana" "Alves"
create_user "rafael.souza@cardcorp.test" "Rafael" "Souza"
create_user "camila.rocha@cardcorp.test" "Camila" "Rocha"

# --- outputs -------------------------------------------------------------------
mkdir -p "$OUT_DIR"
printf '%s' "$PROJECT_ID" > "$OUT_DIR/project_id"
printf '%s' "$CLIENT_ID" > "$OUT_DIR/swagger_client_id"
echo "bootstrap done: project_id=$PROJECT_ID swagger_client_id=$CLIENT_ID"
