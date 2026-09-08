#!/usr/bin/env bash
# Shared helpers for the repository scripts. Source this file; do not run it.
#
# Provides: REPO_ROOT, API_DIR, WEB_DIR, E2E_DIR, API_URL, WEB_URL,
#           load_toolchain, start_api, start_web, wait_for_url, stop_apps

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
API_DIR="$REPO_ROOT/apps/api-maintenance"
WEB_DIR="$REPO_ROOT/apps/web"
E2E_DIR="$REPO_ROOT/apps/e2e"
API_URL="${API_URL:-http://localhost:5000}"
WEB_URL="${WEB_URL:-http://localhost:4200}"
LOG_DIR="${TMPDIR:-/tmp}/vehicle-maintenance-tracker"
mkdir -p "$LOG_DIR"

# Puts dotnet and the pinned Node on PATH for this shell.
load_toolchain() {
  export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
  export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
  export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
  export NVM_DIR="${NVM_DIR:-$HOME/.nvm}"
  if [ -s "$NVM_DIR/nvm.sh" ]; then
    # shellcheck disable=SC1091
    . "$NVM_DIR/nvm.sh"
    (cd "$REPO_ROOT" && nvm use >/dev/null 2>&1) || true
    # nvm use in a subshell does not affect us; resolve the pinned version's bin explicitly.
    local node_bin
    node_bin="$(cd "$REPO_ROOT" && nvm which "$(cat .nvmrc)" 2>/dev/null || true)"
    [ -n "$node_bin" ] && export PATH="$(dirname "$node_bin"):$PATH"
  fi
  command -v dotnet >/dev/null || { echo "dotnet not found; install the .NET SDK to ~/.dotnet or set DOTNET_ROOT" >&2; return 1; }
  command -v node >/dev/null || { echo "node not found; run scripts/setup.sh to install it via nvm" >&2; return 1; }
}

API_PID=""
WEB_PID=""

# Apps start in their own process group (setsid) so stop_apps can kill the whole tree:
# `npx ng serve` and `dotnet run` both spawn grandchildren that would otherwise outlive us
# and keep their ports.
start_api() {
  setsid bash -c "cd '$API_DIR' && exec dotnet run --project src/Maintenance.Api --launch-profile http" >"$LOG_DIR/api.log" 2>&1 &
  API_PID=$!
}

# The PrimeUI license key for PrimeNG: the PRIMEUI_LICENSE variable, else apps/web/.env. Prints the
# ng serve / ng build argument that embeds it, or nothing when no key is configured.
web_license_args() {
  local key="${PRIMEUI_LICENSE:-}"
  if [ -z "$key" ] && [ -f "$WEB_DIR/.env" ]; then
    key="$(grep -E '^PRIMEUI_LICENSE=' "$WEB_DIR/.env" | head -1 | cut -d= -f2- | tr -d '[:space:]')"
  fi
  if [ -n "$key" ]; then
    printf -- "--define PRIMEUI_LICENSE='%s'" "$key"
  fi
}

# start_web [dev|static]: the dev server for humans, or a production build served statically
# with an /api proxy (scripts/lib/serve-web.mjs) for deterministic browser tests.
start_web() {
  local mode="${1:-dev}" license_args
  license_args="$(web_license_args)"
  if [ "$mode" = "static" ]; then
    (cd "$WEB_DIR" && eval npx ng build "$license_args" >"$LOG_DIR/web-build.log" 2>&1) || { tail -20 "$LOG_DIR/web-build.log" >&2; echo "ng build failed (see $LOG_DIR/web-build.log)" >&2; return 1; }
    setsid node "$REPO_ROOT/scripts/lib/serve-web.mjs" "$WEB_DIR/dist/web/browser" 4200 "$API_URL" >"$LOG_DIR/web.log" 2>&1 &
  else
    setsid bash -c "cd '$WEB_DIR' && exec npx ng serve --port 4200 $license_args" >"$LOG_DIR/web.log" 2>&1 &
  fi
  WEB_PID=$!
}

# wait_for_url <url> [seconds]
wait_for_url() {
  local url="$1" seconds="${2:-90}" i
  for ((i = 0; i < seconds; i++)); do
    if curl -s -o /dev/null "$url"; then return 0; fi
    sleep 1
  done
  echo "timed out waiting for $url (see $LOG_DIR)" >&2
  return 1
}

stop_apps() {
  for pid in "$WEB_PID" "$API_PID"; do
    if [ -n "$pid" ] && kill -0 "$pid" 2>/dev/null; then
      kill -TERM -- "-$pid" 2>/dev/null || kill -TERM "$pid" 2>/dev/null || true
    fi
  done
  sleep 1
  for pid in "$WEB_PID" "$API_PID"; do
    [ -n "$pid" ] && kill -KILL -- "-$pid" 2>/dev/null || true
  done
  API_PID=""; WEB_PID=""
}

# Fails fast when a port is already taken by a stray process instead of silently testing the wrong server.
require_port_free() {
  if ss -ltn 2>/dev/null | grep -q ":$1 "; then
    echo "port $1 is already in use; stop the process holding it (ss -ltnp | grep :$1)" >&2
    return 1
  fi
}
