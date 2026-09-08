#!/usr/bin/env bash
# Seeds a running API with docs/data/test-data.json (accounts, vehicles, maintenance records)
# through the public endpoints. Safe to run again. Usage: scripts/seed.sh [api url]
# Default API url: http://localhost:5000 (both scripts/dev.sh and the compose stack).

# shellcheck disable=SC1091
. "$(dirname "${BASH_SOURCE[0]}")/lib/common.sh"
load_toolchain

api="${1:-$API_URL}"
node "$REPO_ROOT/scripts/lib/seed.mjs" "$api" "$REPO_ROOT/docs/data/test-data.json"
