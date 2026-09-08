# Vehicle Maintenance Tracker

A multi-user web application where each user registers their vehicles and logs any kind of maintenance job against them, with cost in US dollars, date, mileage, service provider, and notes. Accounts use email and password with email verification and password reset. The project runs locally only, directly on the machine or as a container stack; it is never published to the internet.

## What it does

- **Accounts:** register, verify the address through an emailed link, log in, request a password reset, set a new password. Emails are written to the API log (no mail server); in the Development environment they can also be read back from `GET /api/v1/dev/emails`, which the browser suite uses.
- **Vehicles:** list, add, edit, and delete the signed-in user's vehicles (make, model, year, VIN, plate, current mileage). A VIN is unique per user. Deleting a vehicle deletes its maintenance history.
- **Maintenance history:** per vehicle, newest first. Log, edit, and delete jobs with description, cost in USD, date performed, mileage at service, and optional provider and notes. A job whose mileage is above the vehicle's advances the vehicle's mileage in the same transaction; editing or deleting a job never lowers it.
- **Verification flag:** every feature works without a verified address by default. Set `Auth__RequireEmailVerification=true` to make every endpoint outside `/auth` answer `403` with code `EmailNotVerified` until the address is verified.

## Repository layout

This is a monorepo: one folder holds everything needed to build, run, test, and document the project.

| Folder | Contents |
|---|---|
| `apps/api-maintenance/` | ASP.NET Core 8 Web API in Clean Architecture (`Maintenance.Domain`, `.Application`, `.Infrastructure`, `.Api`), EF Core with SQLite, xUnit unit and integration tests |
| `apps/web/` | Angular 22 client (standalone, zoneless, signals) with PrimeNG and Vitest; the API client under `src/app/api/` is generated |
| `apps/e2e/` | Playwright browser journeys against running apps |
| `docker/` | Dockerfiles, `nginx.conf`, and the docker compose stack for a local run |
| `docs/` | Working plan, technical design, implementation design with tracked progress, architecture decision records |
| `scripts/` | Setup, run, test, migrate, client generation, and compose scripts |
| `.claude/` | Claude Code project instructions and permissions |
| `.github/` | Pull request template |

Each app folder has its own README. [.claude/CLAUDE.md](.claude/CLAUDE.md) holds the folder map, conventions, and commands in the form used during working sessions.

## Prerequisites

Installed by hand once:

- **.NET SDK 8.0** at `~/.dotnet` (the version is pinned in `global.json`). Not required on `PATH`: the scripts add it.
- **nvm**, which installs the Node version pinned in `.nvmrc` (Node 24).
- **Google Chrome**, which the browser suite drives (`channel: chrome`, no browser download).
- **Docker Engine with the Compose plugin**, only for the container stack.

## Setup, run, and test

```bash
scripts/setup.sh          # once per machine: dotnet on PATH, nvm install, restore, npm ci, signing key, e2e .env
scripts/dev.sh            # API on http://localhost:5000 and ng serve on http://localhost:4200; Ctrl+C stops both
scripts/test.sh           # dotnet test, ng test, then Playwright against a fresh API and a production build
scripts/test.sh --no-e2e  # the two unit suites only
```

`scripts/setup.sh` generates the session-token signing key into `dotnet user-secrets`, so nothing secret is written to the repository. `scripts/dev.sh` applies pending EF Core migrations on start-up and runs the API in the Development environment, which enables Swagger at http://localhost:5000/swagger and the recorded-emails endpoint. Per app: `dotnet test` in `apps/api-maintenance`, `npm test` in `apps/web`, `npx playwright test` in `apps/e2e` with the apps running. Other scripts, including `migrate.sh` and `generate-api-client.sh`, are listed in [scripts/README.md](scripts/README.md).

### Container stack

`scripts/compose-up.sh` builds both images and starts the API and the Nginx-served client detached, with the SQLite file on a named volume; `scripts/compose-down.sh` stops them and keeps the data (`--volumes` deletes it). The first run copies `docker/.env.example` to the git-ignored `docker/.env` and generates the signing key. Open http://localhost:4200; the API answers on http://localhost:5000 and through the client's `/api` proxy.

To run the browser suite against the stack, start it in the Development environment (which exposes the recorded-emails endpoint the suite reads links from) with a rate limit high enough for parallel logins, then point Playwright at it:

```bash
ASPNETCORE_ENVIRONMENT=Development RateLimits__AuthPermitLimit=1000 scripts/compose-up.sh
(cd apps/e2e && WEB_BASE_URL=http://localhost:4200 API_BASE_URL=http://localhost:5000 npx playwright test)
scripts/compose-down.sh
```

See [docker/README.md](docker/README.md) for what each container file does.

## Configuration

The API reads standard ASP.NET Core configuration: `appsettings.json` holds the defaults, `dotnet user-secrets` the local signing key, and environment variables override both (`A__B` maps to section `A`, key `B`). The same names appear in [apps/api-maintenance/.env.example](apps/api-maintenance/.env.example) and [docker/.env.example](docker/.env.example).

| Variable | Default | Purpose |
|---|---|---|
| `ConnectionStrings__Default` | `Data Source=maintenance.db` | SQLite file; `/data/maintenance.db` on the compose volume |
| `Jwt__SigningKey` | none, required | Secret of at least 32 characters that signs session tokens; generated by `setup.sh` (user secrets) and `compose-up.sh` (`docker/.env`) |
| `Jwt__Lifetime` | `24:00:00` | Session token lifetime |
| `Tokens__Lifetime` | `00:30:00` | Lifetime of emailed verification and reset links (product decision: 30 minutes) |
| `Tokens__PurgeAfter` | `30.00:00:00` | Used and expired links older than this are deleted at start-up |
| `Client__BaseUrl` | `http://localhost:4200` | Public URL of the client, embedded in emailed links |
| `Cors__ClientOrigin` | `http://localhost:4200` | Origin allowed by CORS (unused behind the compose proxy, which serves both on one origin) |
| `Auth__RequireEmailVerification` | `false` | The verification feature flag described above |
| `RateLimits__AuthPermitLimit` | `10` | Login, resend-verification, and forgot-password attempts allowed per client address per window |
| `RateLimits__AuthWindow` | `00:01:00` | The rate-limit window |

The Angular client reads its API base URL from `src/environments/`: empty in both environments, because `ng serve` proxies `/api` through `proxy.conf.json` and Nginx proxies it in the stack. The browser suite reads `WEB_BASE_URL` and `API_BASE_URL` from `apps/e2e/.env`.

## Documentation

- [Working plan](docs/plans/Vehicle%20Maintenance%20Tracker.md): refined story, clarified requirements, code context.
- [Technical design](docs/designs/Vehicle%20Maintenance%20Tracker.md): class, database, sequence, state, component, use case, and deployment diagrams; API contract; non-functional requirements; design decisions. A presentation page sits beside it as HTML.
- [Implementation design](docs/implementation/Vehicle%20Maintenance%20Tracker.md): technology stack with every option presented and the decision taken (Phase 1), monorepo layout (Phase 2), and the 35-slice implementation plan with each slice's status, definition of done, and pattern decisions (Phase 3). Progress is recorded there, one commit per slice on `main`.
- [Architecture decision records](docs/adr/README.md): decisions made during implementation that reach beyond one slice.
