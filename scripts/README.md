# scripts

Repository-level Bash scripts. Run them from anywhere; each resolves the repository root itself and delegates to the per-app commands.

| Script | Purpose |
|---|---|
| `setup.sh` | One-time machine setup: dotnet on PATH, `nvm install`, restore and `npm ci` for every app, create `apps/e2e/.env` |
| `dev.sh` | Run the API and `ng serve` together; Ctrl+C stops both |
| `test.sh [--no-e2e]` | `dotnet test`, `ng test`, then Playwright against a fresh API and a production build of the client served by `lib/serve-web.mjs`; the API runs with `RateLimits__AuthPermitLimit=1000` (override with `E2E_AUTH_PERMIT_LIMIT`) because every journey logs in from one address |
| `migrate.sh` | EF Core migrations (slice S06) |
| `generate-api-client.sh` | Regenerate the Angular API client (slice S16) |
| `compose-up.sh`, `compose-down.sh` | Container stack (slice S34) |

`lib/common.sh` holds the shared helpers (toolchain loading, starting and stopping the apps, readiness waits) and is sourced, never run. `lib/serve-web.mjs` is a dependency-free static server with SPA fallback and an `/api` proxy, the same shape as the Nginx container.
