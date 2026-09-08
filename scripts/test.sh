#!/usr/bin/env bash
# Runs every suite in sequence, failing fast: dotnet test, ng test, then Playwright
# against freshly started apps. Use --no-e2e to skip the browser suite.

# shellcheck disable=SC1091
. "$(dirname "${BASH_SOURCE[0]}")/lib/common.sh"
load_toolchain

run_e2e=true
[ "${1:-}" = "--no-e2e" ] && run_e2e=false

echo "==> dotnet test (apps/api-maintenance)"
(cd "$API_DIR" && dotnet test --nologo -v q)

echo "==> ng test (apps/web)"
(cd "$WEB_DIR" && npx ng test --watch=false)

if $run_e2e; then
  echo "==> playwright test (apps/e2e) against fresh API and client"
  trap stop_apps EXIT INT TERM
  start_api
  start_web
  wait_for_url "$API_URL/health"
  wait_for_url "$WEB_URL/"
  (cd "$E2E_DIR" && npx playwright test)
  stop_apps
fi

echo "all suites passed"
