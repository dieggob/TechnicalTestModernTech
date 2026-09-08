# Vehicle Maintenance Tracker

A multi-user web application where each user registers their vehicles and logs any kind of maintenance job against them, with cost in US dollars, date, mileage, service provider, and notes. Accounts use email and password with email verification and password reset. The project runs locally only, directly on the machine or as a container stack; it is never published to the internet.

## Contents

1. [What it does](#what-it-does)
2. [Repository layout](#repository-layout)
3. [Running the project](#running-the-project)
   - [Prerequisites](#1-prerequisites)
   - [One-time setup](#2-one-time-setup)
   - [Option A: run on the machine](#3a-run-on-the-machine)
   - [Option B: run with docker compose](#3b-run-with-docker-compose)
   - [First use: from sign-up to a logged job](#4-first-use-from-sign-up-to-a-logged-job)
   - [Stopping](#5-stopping)
4. [Running the tests](#running-the-tests)
5. [Configuration](#configuration)
6. [Troubleshooting](#troubleshooting)
7. [Documentation](#documentation)

## What it does

- **Accounts:** register, verify the address through an emailed link, log in, request a password reset, set a new password. No mail server is involved: emails are written to the API log and, in the Development environment, can be read back from `GET /api/v1/dev/emails`.
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

## Running the project

All commands run from the repository root on Linux with Bash. There are two ways to run the application: directly on the machine (Option A, best for development) or as a container stack (Option B, closest to a deployment). Both serve the client at http://localhost:4200 and the API at http://localhost:5000.

### 1. Prerequisites

Install these once by hand. Everything else is installed by the setup script.

| Tool | Needed for | How to check | How to install |
|---|---|---|---|
| .NET SDK 8.0 at `~/.dotnet` | API, Option A, tests | `~/.dotnet/dotnet --list-sdks` shows `8.0.x` | `curl -sSL https://dot.net/v1/dotnet-install.sh \| bash -s -- --channel 8.0` (installs to `~/.dotnet`; the scripts put it on `PATH`, no shell profile change needed) |
| nvm | client and browser tests, Option A | `. ~/.nvm/nvm.sh && nvm --version` | Follow https://github.com/nvm-sh/nvm#installing-and-updating, then open a new terminal. The setup script installs the Node version pinned in `.nvmrc` (Node 24) |
| Google Chrome | browser tests only | `google-chrome --version` | https://www.google.com/chrome/ (the suite drives the installed Chrome; nothing is downloaded) |
| Docker Engine with the Compose plugin | Option B only | `docker compose version` | https://docs.docker.com/engine/install/ and add your user to the `docker` group, then log out and in again |

### 2. One-time setup

```bash
git clone git@github.com:dieggob/TechnicalTestModernTech.git
cd TechnicalTestModernTech
scripts/setup.sh
```

The setup script checks the .NET SDK, installs the pinned Node through nvm, restores the API, runs `npm ci` for the client and the browser tests, generates the session-token signing key into `dotnet user-secrets`, and creates `apps/e2e/.env` from its example. It ends with `setup complete`. It is safe to run again.

Option B does not need this step: the images install their own toolchains. It only needs Docker.

### 3a. Run on the machine

```bash
scripts/dev.sh
```

The script starts the API with `dotnet run` and the client with `ng serve`, waits until both answer, and prints:

```text
API     http://localhost:5000   (Swagger: http://localhost:5000/swagger)
Client  http://localhost:4200
Logs    /tmp/vehicle-maintenance-tracker/api.log, /tmp/vehicle-maintenance-tracker/web.log
Press Ctrl+C to stop.
```

Open http://localhost:4200. The API runs in the Development environment: pending database migrations are applied on start-up to the SQLite file `apps/api-maintenance/src/Maintenance.Api/maintenance.db`, Swagger is available, and emailed links can be read from http://localhost:5000/api/v1/dev/emails. Source changes to the client reload the browser; changes to the API need a restart of the script.

### 3b. Run with docker compose

```bash
scripts/compose-up.sh
```

The first run copies `docker/.env.example` to the git-ignored `docker/.env` and generates the signing key. The script then builds two images (the API on the .NET 8 runtime image, the Angular production build on Nginx), starts them detached, waits for the API's health check, and prints:

```text
stack is up: client at http://localhost:4200, API at http://localhost:5000
```

Open http://localhost:4200. Nginx serves the client and proxies `/api` to the API container, so both run on one origin. The SQLite file lives on the named volume `maintenance-data` and survives restarts. The stack runs in the Production environment by default, which hides Swagger and the recorded-emails endpoint; to read emailed links, either look at the API log with `docker compose --project-directory docker logs api-maintenance` or start the stack in Development:

```bash
ASPNETCORE_ENVIRONMENT=Development scripts/compose-up.sh
```

Any variable in `docker/.env` can be overridden the same way from the shell for one run.

### 4. First use: from sign-up to a logged job

1. Open http://localhost:4200. You land on the login page; follow **Create an account**. Enter an email address (any syntactically valid address works, nothing is sent) and a password of at least 8 characters containing a letter and a digit, then **Create account**.
2. Log in with the same credentials. A banner reminds you that the address is not verified; every feature still works.
3. To verify, find the emailed link. With Option A, or Option B started in Development, open http://localhost:5000/api/v1/dev/emails and copy the `link` of the newest entry for your address. Otherwise read it from the API log, where each email is logged as `Email to <address>: <subject> <link>`. Open the link in the browser. Links expire after 30 minutes; **Resend verification email** in the banner issues a new one.
4. Choose **Add vehicle** and fill in make, model, year, VIN, license plate, and current mileage.
5. Click the vehicle's make in the list to open its page, then **Log maintenance**. Enter a description, the cost in USD, the date performed (not in the future), and the mileage at service; provider and notes are optional. If the mileage is above the vehicle's, the vehicle's mileage on the page updates when the job is saved.
6. Edit or delete jobs and vehicles from the pencil and bin icons on each row. Deleting asks for confirmation.
7. Forgotten password: on the login page follow **Reset it**, enter the address, then read the reset link the same way as the verification link and set a new password. **Log out** is in the toolbar.

### 5. Stopping

- Option A: press `Ctrl+C` in the terminal running `scripts/dev.sh`; it stops both processes and frees the ports.
- Option B: `scripts/compose-down.sh` stops and removes the containers and keeps the data; `scripts/compose-down.sh --volumes` also deletes the database volume.

## Running the tests

```bash
scripts/test.sh           # everything: dotnet test, ng test, then Playwright against a fresh API and a production build
scripts/test.sh --no-e2e  # the two unit suites only
```

The full script starts its own API and client on ports 5000 and 4200, so stop `scripts/dev.sh` or the compose stack first. The API gets `RateLimits__AuthPermitLimit=1000` for that run because every journey logs in from one address. Per app: `dotnet test` in `apps/api-maintenance`, `npm test` in `apps/web`, and `npx playwright test` in `apps/e2e` with the apps running.

To run the browser suite against the compose stack instead, start it in Development with the raised rate limit, then point Playwright at it:

```bash
ASPNETCORE_ENVIRONMENT=Development RateLimits__AuthPermitLimit=1000 scripts/compose-up.sh
(cd apps/e2e && WEB_BASE_URL=http://localhost:4200 API_BASE_URL=http://localhost:5000 npx playwright test)
scripts/compose-down.sh
```

Other scripts, including `migrate.sh` for EF Core migrations and `generate-api-client.sh` for the Angular API client, are listed in [scripts/README.md](scripts/README.md).

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
| `ASPNETCORE_ENVIRONMENT` | `Production` in compose, `Development` under `scripts/dev.sh` | Development enables Swagger and `GET /api/v1/dev/emails` |

Compose adds `API_PORT` and `WEB_PORT` (defaults 5000 and 4200) in `docker/.env`. The Angular client reads its API base URL from `src/environments/`: empty in both environments, because `ng serve` proxies `/api` through `proxy.conf.json` and Nginx proxies it in the stack. The browser suite reads `WEB_BASE_URL` and `API_BASE_URL` from `apps/e2e/.env`.

## Troubleshooting

| Symptom | Cause and fix |
|---|---|
| `dotnet not found at /home/you/.dotnet` | Install the SDK with the `dotnet-install.sh` command above, or set `DOTNET_ROOT` to where it is installed |
| `nvm not found` | Install nvm, then open a new terminal so its shell function is loaded |
| `port 5000 is already in use` or `port 4200 …` | Another run is active. Stop it (`Ctrl+C`, `scripts/compose-down.sh`), or find the process with `ss -ltnp \| grep :5000` |
| `permission denied while trying to connect to the docker API` | Your user is not in the `docker` group yet, or the session predates the change. Add the group and log out and in, or prefix one command with `sg docker -c '…'` |
| The verification or reset link says it is invalid | Links expire after 30 minutes and work once; request a new one with **Resend verification email** or **Reset it** on the login page |
| `429` on login during a script or tool run | The auth rate limit (10 per minute per address) was hit. Wait a minute or raise `RateLimits__AuthPermitLimit` for that run |
| `403` with code `EmailNotVerified` | `Auth__RequireEmailVerification` is `true`; verify the address or set the flag back to `false` |
| Playwright cannot find a browser | Install Google Chrome; the suite uses `channel: chrome` and does not download browsers |

## Documentation

- [Working plan](docs/plans/Vehicle%20Maintenance%20Tracker.md): refined story, clarified requirements, code context.
- [Technical design](docs/designs/Vehicle%20Maintenance%20Tracker.md): class, database, sequence, state, component, use case, and deployment diagrams; API contract; non-functional requirements; design decisions. A presentation page sits beside it as HTML.
- [Implementation design](docs/implementation/Vehicle%20Maintenance%20Tracker.md): technology stack with every option presented and the decision taken (Phase 1), monorepo layout (Phase 2), and the 35-slice implementation plan with each slice's status, definition of done, and pattern decisions (Phase 3). Progress is recorded there, one commit per slice on `main`.
- [Architecture decision records](docs/adr/README.md): decisions made during implementation that reach beyond one slice.
