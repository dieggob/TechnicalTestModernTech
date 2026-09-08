# Vehicle Maintenance Tracker — project instructions

This repository is a monorepo. Everything needed to build, run, test, and document the project lives here. The authoritative descriptions are in `docs/`; this file is the short version for working sessions. All 37 implementation slices are done; new work is a new slice appended to the plan's Phase 3.

## Folder map

```text
apps/api-maintenance/   ASP.NET Core 8 Web API, Clean Architecture: src/Maintenance.{Domain,Application,Infrastructure,Api}, tests/Maintenance.{UnitTests,IntegrationTests}
apps/web/               Angular 22 client (standalone, zoneless, signals, PrimeNG, Vitest); generated API client in src/app/api/ (never edit by hand)
apps/e2e/               Playwright journeys against running apps (Google Chrome via channel: chrome); tests/support/ holds ApiHelper and page objects
docker/                 docker-compose.yml, .env.example, api-maintenance/Dockerfile, web/Dockerfile + nginx.conf (SPA fallback, CSP, /api proxy)
docs/                   plans/ (working plan), designs/ (technical design + HTML), implementation/ (stack, layout, slice plan with progress), adr/
scripts/                setup.sh, dev.sh, test.sh, migrate.sh, generate-api-client.sh, compose-up.sh, compose-down.sh; lib/ holds common.sh and serve-web.mjs
```

Root files: `global.json` pins the .NET SDK, `.nvmrc` pins Node, `.editorconfig` formats both ecosystems, `.gitignore` covers .NET, Node, Playwright, SQLite, and `.env` files.

## Commands

Run from the repository root unless stated.

- `scripts/setup.sh` once per machine (dotnet on PATH, `nvm install`, restore, `npm ci` for web and e2e, signing key into user secrets, `apps/e2e/.env`)
- `scripts/dev.sh` runs the API (Development: Swagger and `GET /api/v1/dev/emails` enabled) and `ng serve` together
- `scripts/test.sh` runs `dotnet test`, `ng test`, then Playwright against a fresh API and a production build served by `scripts/lib/serve-web.mjs`; the API gets `RateLimits__AuthPermitLimit=1000` because every journey logs in from one address. `--no-e2e` skips the browser suite
- `scripts/migrate.sh add <Name>` / `scripts/migrate.sh update` / `scripts/migrate.sh list` for EF Core migrations (run from anywhere)
- `scripts/generate-api-client.sh` regenerates `apps/web/src/app/api/` from the running API's OpenAPI document (operation ids are `{Controller}_{Action}`, so functions are `vehicleList`, `maintenanceCreate`, …); run it after any API contract change and commit the diff
- `scripts/compose-up.sh` / `scripts/compose-down.sh [--volumes]` for the container stack; `ASPNETCORE_ENVIRONMENT=Development RateLimits__AuthPermitLimit=1000 scripts/compose-up.sh` prepares it for Playwright with `WEB_BASE_URL` and `API_BASE_URL` pointing at the containers
- Per app: `dotnet test` in `apps/api-maintenance`; `npm test` in `apps/web`; `npx playwright test` in `apps/e2e`

## Conventions

- **Naming:** app folders are lower-case kebab-case; .NET projects and namespaces start with `Maintenance.`; Angular features live in `src/app/features/<domain>/` (`auth`, `vehicles`, `maintenance`); scripts are verbs.
- **Dependency direction:** `web → api-maintenance` over HTTP only, through the generated client; `e2e` talks to running apps only; apps never reference each other's source. Inside the API: `Api → Application → Domain` and `Infrastructure → Application → Domain`; `Api` references `Infrastructure` only to register implementations.
- **Where new code goes:** entities and repository interfaces in `Maintenance.Domain`; services, validators, options, and use-case contracts in `Maintenance.Application`; EF mappings, migrations, and external adapters in `Maintenance.Infrastructure`; controllers in `Maintenance.Api/Controllers`; screens in `apps/web/src/app/features/<domain>/` behind that feature's facade; browser journeys in `apps/e2e/tests/<domain>/`.
- **Client patterns:** one facade per feature holds signals and is the only caller of the generated client; forms bind server field errors through `shared/problem-details.ts`; dialogs and confirmations are signal-driven (`p-dialog` with `[visible]`), because the app is zoneless; test ids are `data-testid` attributes and Playwright locates through them or labels.
- **API patterns:** one `IExceptionHandler` maps application exceptions to ProblemDetails; `IClock` is the only source of time; `IUnitOfWork` wraps multi-aggregate writes; the fallback authorization policy carries the email-verification requirement; the auth rate limit is one named policy partitioned by client address.
- **Configuration and secrets:** each app documents its variables in its own `.env.example`; `docker/.env.example` documents compose's; real `.env` files, SQLite files, and `dotnet user-secrets` content are never committed. Angular reads its API base URL from `src/environments/`; the PrimeUI license key is the one build-time value, read from `apps/web/.env` by the scripts (`--define PRIMEUI_LICENSE`) and from `docker/.env` by the compose build.
- **Delivery:** one commit per slice on `main`, message prefixed with the slice number (`S07: establish the error model`); tests are written before the production code of each slice; `scripts/test.sh` passes before every commit.
- **Progress tracking:** slice statuses, definition-of-done checkboxes, and pattern decisions live in `docs/implementation/Vehicle Maintenance Tracker.md`, Phase 3 (Slice Map, per-slice sections, Pattern Proposals Register). Update them when a slice starts or finishes; decisions that reach beyond one slice get a record in `docs/adr/`.
- **Technology decisions:** are the developer's. Present options with why and why not for each, recommend one, and wait for the choice; never pick a technology, library, or pattern unilaterally.

## Principles the code follows

Clean Code, DRY, OOP, YAGNI, SOLID, as applied per slice in the implementation plan: entities own their invariants and rules; services orchestrate through the interfaces the design draws; shared helpers are introduced by the first slice that needs them; nothing is built ahead of an acceptance criterion.
