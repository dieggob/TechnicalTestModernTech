#!/usr/bin/env bash
# One-time machine setup: toolchain on PATH, pinned Node via nvm, dependencies restored,
# and the e2e .env created from its example.
#
# Prerequisites installed by hand: .NET SDK 8 at ~/.dotnet, nvm, Docker with the Compose plugin.

# shellcheck disable=SC1091
. "$(dirname "${BASH_SOURCE[0]}")/lib/common.sh"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
command -v dotnet >/dev/null || { echo "dotnet not found at $DOTNET_ROOT; install the .NET SDK 8 first" >&2; exit 1; }
echo "dotnet $(dotnet --version)"

export NVM_DIR="${NVM_DIR:-$HOME/.nvm}"
[ -s "$NVM_DIR/nvm.sh" ] || { echo "nvm not found; install it from https://github.com/nvm-sh/nvm" >&2; exit 1; }
# shellcheck disable=SC1091
. "$NVM_DIR/nvm.sh"
(cd "$REPO_ROOT" && nvm install >/dev/null && nvm use >/dev/null)
load_toolchain
echo "node $(node --version), npm $(npm --version)"

echo "restoring apps/api-maintenance"
(cd "$API_DIR" && dotnet restore --nologo -v q)
# Session-token signing key lives in user secrets, never in appsettings.json.
if ! (cd "$API_DIR/src/Maintenance.Api" && dotnet user-secrets list 2>/dev/null | grep -q '^Jwt:SigningKey'); then
  (cd "$API_DIR/src/Maintenance.Api" && dotnet user-secrets set Jwt:SigningKey "$(head -c 48 /dev/urandom | base64 | tr -d '\n')" >/dev/null)
  echo "generated Jwt:SigningKey in dotnet user-secrets"
fi
echo "installing apps/web"
(cd "$WEB_DIR" && npm ci --no-audit --no-fund)
echo "installing apps/e2e"
(cd "$E2E_DIR" && npm ci --no-audit --no-fund)
[ -f "$E2E_DIR/.env" ] || { cp "$E2E_DIR/.env.example" "$E2E_DIR/.env"; echo "created apps/e2e/.env from .env.example"; }

echo "setup complete; run scripts/dev.sh or scripts/test.sh"
