# docker

Container files for the local run. Start and stop the stack with `scripts/compose-up.sh` and `scripts/compose-down.sh` from the repository root.

| File | Purpose |
|---|---|
| `docker-compose.yml` | Two services: `api-maintenance` (SQLite file on the `maintenance-data` volume, health check on `/health`) and `web` (Nginx serving the Angular build and proxying `/api` to the API). Project name `vehicle-maintenance-tracker`. |
| `.env.example` | Ports, the signing key, the ASP.NET environment, and the feature flags; copied to the git-ignored `.env` by `compose-up.sh`, which also generates the key |
| `api-maintenance/Dockerfile` | `sdk:8.0` restore and publish stage, `aspnet:8.0` runtime stage running as the image's non-root user; build context `apps/api-maintenance` |
| `web/Dockerfile` | `node:24-alpine` build stage, `nginx:1.30-alpine` serve stage; build context `apps/web` plus the named context `docker-web` for `nginx.conf` |
| `web/nginx.conf` | SPA fallback (`try_files`), security headers with the Content Security Policy, `/api` proxy |

Both build contexts point at the app folders, so the images never see other apps or the repository root. Each app's `.dockerignore` keeps `bin/`, `obj/`, `node_modules/`, and local databases out of the context.

The client and the API share one origin through Nginx, so CORS never applies in the stack. The API's per-address rate limit sees Nginx's address for every browser; raise `RateLimits__AuthPermitLimit` when many logins come through the proxy at once, as the Playwright suite does.
