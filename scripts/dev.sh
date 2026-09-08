#!/usr/bin/env bash
# Runs the API and the Angular dev server together. Ctrl+C stops both.

# shellcheck disable=SC1091
. "$(dirname "${BASH_SOURCE[0]}")/lib/common.sh"
load_toolchain

trap stop_apps EXIT INT TERM
require_port_free 5000 && require_port_free 4200
start_api
start_web
wait_for_url "$API_URL/health"
wait_for_url "$WEB_URL/"
echo "API     $API_URL   (Swagger: $API_URL/swagger)"
echo "Client  $WEB_URL"
echo "Logs    $LOG_DIR/api.log, $LOG_DIR/web.log"
echo "Press Ctrl+C to stop."
wait
