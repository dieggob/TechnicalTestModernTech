# scripts

Repository-level Bash scripts. Run them from anywhere; each resolves the repository root itself and delegates to the per-app commands.

| Script | Purpose |
|---|---|
| `setup.sh` | One-time machine setup: dotnet on PATH, `nvm install`, restore and `npm ci` for every app, create `apps/e2e/.env` |
| `dev.sh` | Run the API and `ng serve` together; Ctrl+C stops both |
| `test.sh [--no-e2e]` | `dotnet test`, `ng test`, then Playwright against freshly started apps |
| `migrate.sh` | EF Core migrations (slice S06) |
| `generate-api-client.sh` | Regenerate the Angular API client (slice S16) |
| `compose-up.sh`, `compose-down.sh` | Container stack (slice S34) |

`lib/common.sh` holds the shared helpers (toolchain loading, starting and stopping the apps, readiness waits) and is sourced, never run.
