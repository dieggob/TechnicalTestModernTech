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

start_api() {
  (cd "$API_DIR" && exec dotnet run --project src/Maintenance.Api --launch-profile http) >"$LOG_DIR/api.log" 2>&1 &
  API_PID=$!
}

start_web() {
  (cd "$WEB_DIR" && exec npx ng serve --port 4200) >"$LOG_DIR/web.log" 2>&1 &
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
      pkill -P "$pid" 2>/dev/null || true
      kill "$pid" 2>/dev/null || true
    fi
  done
  API_PID=""; WEB_PID=""
}
