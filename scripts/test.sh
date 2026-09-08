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
  echo "==> playwright test (apps/e2e) against a fresh API and a production build of the client"
  trap stop_apps EXIT INT TERM
  require_port_free 5000 && require_port_free 4200
  # Every journey logs in from one address; the production limit (10/min) would fail the suite itself.
  export RateLimits__AuthPermitLimit="${E2E_AUTH_PERMIT_LIMIT:-1000}"
  start_api
  start_web static
  wait_for_url "$API_URL/health"
  wait_for_url "$WEB_URL/"
  (cd "$E2E_DIR" && npx playwright test)
  stop_apps
fi

echo "all suites passed"
