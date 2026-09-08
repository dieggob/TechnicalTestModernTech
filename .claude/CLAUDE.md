# Vehicle Maintenance Tracker — project instructions

This repository is a monorepo. Everything needed to build, run, test, and document the project lives here. The authoritative descriptions are in `docs/`; this file is the short version for working sessions.

## Folder map

```text
apps/api-maintenance/   ASP.NET Core 8 Web API, Clean Architecture: src/Maintenance.{Domain,Application,Infrastructure,Api}, tests/Maintenance.{UnitTests,IntegrationTests}
apps/web/               Angular 22 client (PrimeNG, Vitest); generated API client in src/app/api/ (never edit by hand)
apps/e2e/               Playwright end-to-end tests against running apps
docker/                 docker-compose.yml, .env.example, one Dockerfile folder per app, nginx.conf for the client
docs/                   plans/ (working plan), designs/ (technical design + HTML), implementation/ (stack, layout, slice plan), adr/
scripts/                setup.sh, dev.sh, test.sh, migrate.sh, generate-api-client.sh, compose-up.sh, compose-down.sh
```

Root files: `global.json` pins the .NET SDK, `.nvmrc` pins Node, `.editorconfig` formats both ecosystems, `.gitignore` covers .NET, Node, Playwright, SQLite, and `.env` files.

## Commands

Run from the repository root unless stated.

- `scripts/setup.sh` once per machine (dotnet on PATH, `nvm install`, `npm ci` for web and e2e)
- `scripts/dev.sh` runs the API and `ng serve` together
- `scripts/test.sh` runs `dotnet test`, `ng test`, and `npx playwright test` in sequence
- `scripts/migrate.sh add <Name>` / `scripts/migrate.sh update` for EF Core migrations (run from anywhere)
- `scripts/generate-api-client.sh` regenerates `apps/web/src/app/api/` from the running API's OpenAPI document; run it after any API contract change and commit the diff
- `scripts/compose-up.sh` / `scripts/compose-down.sh` for the container stack
- Per app: `dotnet test` in `apps/api-maintenance`; `npm test` in `apps/web`; `npx playwright test` in `apps/e2e`

## Conventions

- **Naming:** app folders are lower-case kebab-case; .NET projects and namespaces start with `Maintenance.`; Angular features live in `src/app/features/<domain>/`; scripts are verbs.
- **Dependency direction:** `web → api-maintenance` over HTTP only, through the generated client; `e2e` talks to running apps only; apps never reference each other's source. Inside the API: `Api → Application → Domain` and `Infrastructure → Application → Domain`; `Api` references `Infrastructure` only to register implementations.
- **Where new code goes:** entities and repository interfaces in `Maintenance.Domain`; services, validators, and use-case contracts in `Maintenance.Application`; EF mappings, migrations, and external adapters in `Maintenance.Infrastructure`; controllers in `Maintenance.Api/Controllers`; screens in `apps/web/src/app/features/<domain>/`; browser journeys in `apps/e2e/tests/`.
- **Configuration and secrets:** each app documents its variables in its own `.env.example`; `docker/.env.example` documents compose's; real `.env` files, SQLite files, and `dotnet user-secrets` content are never committed. Angular reads its API base URL from `src/environments/`.
- **Delivery:** one commit per slice on `main`, message prefixed with the slice number (`S07: establish the error model`); tests are written before the production code of each slice.
- **Progress tracking:** slice statuses, definition-of-done checkboxes, and pattern decisions live in `docs/implementation/Vehicle Maintenance Tracker.md`, Phase 3. Update them when a slice starts or finishes.

## Principles the code follows

Clean Code, DRY, OOP, YAGNI, SOLID, as applied per slice in the implementation plan: entities own their invariants and rules; services orchestrate through the interfaces the design draws; shared helpers are introduced by the first slice that needs them; nothing is built ahead of an acceptance criterion.
