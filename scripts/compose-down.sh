#!/usr/bin/env bash
# Stops and removes the container stack. The SQLite volume is kept unless --volumes is given.

# shellcheck disable=SC1091
. "$(dirname "${BASH_SOURCE[0]}")/lib/common.sh"

docker compose --project-directory "$REPO_ROOT/docker" down "$@"
if [[ " $* " == *" --volumes "* || " $* " == *" -v "* ]]; then
  echo "stack removed with its data"
else
  echo "stack removed; data kept in the maintenance-data volume (use --volumes to delete it)"
fi
