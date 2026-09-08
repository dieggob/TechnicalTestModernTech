#!/usr/bin/env bash
# Regenerates apps/web/src/app/api from the running API's OpenAPI document with ng-openapi-gen.
# Starts the API itself when nothing answers on $API_URL, and stops it again afterwards.
# Run after any API contract change and commit the diff in the same slice.

# shellcheck disable=SC1091
. "$(dirname "${BASH_SOURCE[0]}")/lib/common.sh"
load_toolchain

started=false
if ! curl -s -o /dev/null "$API_URL/health"; then
  echo "starting the API for generation"
  (cd "$API_DIR" && dotnet build --nologo -v q)
  start_api
  started=true
  trap stop_apps EXIT INT TERM
  wait_for_url "$API_URL/health"
fi

(cd "$WEB_DIR" && npx ng-openapi-gen --input "$API_URL/swagger/v1/swagger.json")

$started && stop_apps
echo "client regenerated in apps/web/src/app/api; review and commit the diff"
