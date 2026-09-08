# Vehicle Maintenance Tracker

A multi-user web application where each user registers their vehicles and logs any kind of maintenance job against them, with cost in US dollars, date, mileage, service provider, and notes. Accounts use email and password with email verification and password reset. The project runs locally only.

## Repository layout

This is a monorepo: one folder holds everything needed to build, run, test, and document the project.

| Folder | Contents |
|---|---|
| `apps/api-maintenance/` | ASP.NET Core 8 Web API (Clean Architecture, EF Core + SQLite) |
| `apps/web/` | Angular 22 client with PrimeNG |
| `apps/e2e/` | Playwright end-to-end tests |
| `docker/` | Dockerfiles and the docker compose stack for a local run |
| `docs/` | Working plan, technical design, and implementation design |
| `scripts/` | Setup, run, test, migrate, and compose scripts |
| `.claude/` | Claude Code project configuration |
| `.github/` | Pull request template |

The folder map, conventions, and commands are described in [.claude/CLAUDE.md](.claude/CLAUDE.md).

## Getting started

Tooling installed by hand once: .NET SDK 8.0 at `~/.dotnet` (pinned in `global.json`), nvm (Node 24 is pinned in `.nvmrc`), and Docker with the Compose plugin for the container stack. Everything else is scripted:

```bash
scripts/setup.sh          # once per machine: PATH, nvm install, restore, npm ci, e2e .env
scripts/dev.sh            # API on http://localhost:5000 and client on http://localhost:4200
scripts/test.sh           # dotnet test, ng test, then Playwright against fresh apps
scripts/test.sh --no-e2e  # the two unit suites only
```

Per app: `dotnet test` in `apps/api-maintenance`, `npm test` in `apps/web`, `npx playwright test` in `apps/e2e` (with the apps running). See [scripts/README.md](scripts/README.md) for the full list.

### Container stack

`scripts/compose-up.sh` builds both images and starts the API and the Nginx-served client detached, with the SQLite file on a named volume; `scripts/compose-down.sh` stops them and keeps the data (`--volumes` deletes it). The first run copies `docker/.env.example` to the git-ignored `docker/.env` and generates the signing key. Open http://localhost:4200; the API answers on http://localhost:5000 and through the client's `/api` proxy.

To run the browser suite against the stack, start it in the Development environment (which exposes the recorded-emails endpoint the suite reads links from) with a rate limit high enough for parallel logins, then point Playwright at it:

```bash
ASPNETCORE_ENVIRONMENT=Development RateLimits__AuthPermitLimit=1000 scripts/compose-up.sh
(cd apps/e2e && WEB_BASE_URL=http://localhost:4200 API_BASE_URL=http://localhost:5000 npx playwright test)
scripts/compose-down.sh
```

See [docker/README.md](docker/README.md) for what each container file does.

## Documentation

- [Working plan](docs/plans/Vehicle%20Maintenance%20Tracker.md): refined story, clarified requirements, code context.
- [Technical design](docs/designs/Vehicle%20Maintenance%20Tracker.md): class, database, sequence, state, component, use case, and deployment diagrams; API contract; non-functional requirements; design decisions. A presentation page sits beside it as HTML.
- [Implementation design](docs/implementation/Vehicle%20Maintenance%20Tracker.md): technology stack (Phase 1), monorepo layout (Phase 2), and the slice-by-slice implementation plan with tracked progress (Phase 3).
