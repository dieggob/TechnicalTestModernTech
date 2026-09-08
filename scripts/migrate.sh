#!/usr/bin/env bash
# EF Core migrations for apps/api-maintenance.
#
#   scripts/migrate.sh add <Name>   create a migration named <Name>
#   scripts/migrate.sh update       apply pending migrations to the configured database
#   scripts/migrate.sh list         list migrations and whether each is applied

# shellcheck disable=SC1091
. "$(dirname "${BASH_SOURCE[0]}")/lib/common.sh"
load_toolchain

cd "$API_DIR"
dotnet tool restore --verbosity quiet >/dev/null

EF=(dotnet ef --project src/Maintenance.Infrastructure --startup-project src/Maintenance.Api)

case "${1:-}" in
  add)
    [ -n "${2:-}" ] || { echo "usage: scripts/migrate.sh add <Name>" >&2; exit 1; }
    "${EF[@]}" migrations add "$2" --output-dir Persistence/Migrations
    ;;
  update)
    "${EF[@]}" database update
    ;;
  list)
    "${EF[@]}" migrations list
    ;;
  *)
    sed -n '2,7p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
    exit 1
    ;;
esac
