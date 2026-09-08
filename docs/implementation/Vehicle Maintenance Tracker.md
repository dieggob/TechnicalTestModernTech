# Implementation Design: Vehicle Maintenance Tracker

## Status

Current phase: Phase 3 completed — implementation may start; update slice statuses as work lands. No open questions.
This Phase 3 supersedes the working plan's own "Phase 3 — Working Plan" (`docs/plans/Vehicle Maintenance Tracker.md`).
Phase 1 manifest paths were revised by Phase 2 on 2026-09-07, and two Phase 1 open questions (Nginx image, Dockerfiles) were closed by Phase 2 decisions.
Inputs: working plan `docs/plans/Vehicle Maintenance Tracker.md`; design `docs/designs/Vehicle Maintenance Tracker.md`. Codebase grounding: repository `TechnicalTestModernTech` at branch `main`, which contains only a one-line `README.md` and the `Docs/` folder; the stack is greenfield and every technology is `new`.

Phase 1 was defined on 2026-09-07. Every technology was chosen by the user in five question rounds; the answers are recorded under its Questions Asked & Answers. Version lookups were made read-only against the NuGet v3 API, the npm registry, angular.dev, endoflife.date, and the .NET support policy page on 2026-09-07. Nothing was installed, restored, or built. Phase 1 was written before the skill's decision protocol required a labelled recommendation per question, so its table marks rounds where none was labelled.

## Phase 1 — Technology Stack

### Stack at a Glance

- **Language and runtime:** C# 12 on .NET SDK 8.0.424 / runtime 8.0.30, pinned by `global.json`; TypeScript 6.0.x on Node.js 24.20.0 LTS, pinned by `.nvmrc` (new)
- **Application framework:** ASP.NET Core 8.0.30 Web API with controllers; Angular 22.1.x single-page client with PrimeNG 22.1.0 components, served by a separate static host (new)
- **Persistence:** Entity Framework Core 8.0.30 with the SQLite provider everywhere, EF Core Migrations applied at start-up (new)
- **Messaging and integration:** verification and reset emails written to the log by an in-house `IEmailSender` implementation; no SMTP client, since the project is local only (new)
- **Security and identity:** custom authentication per the design; JWT bearer 8.0.30 sessions; `PasswordHasher<T>` from the shared framework; BCL random tokens hashed with SHA-256; built-in rate limiting; CORS for the separate client origin (new)
- **Observability:** built-in `ILogger` with the JSON console formatter; BCL `System.Diagnostics.Metrics` counters; ASP.NET Core health checks with the EF Core check 8.0.30 (new)
- **Testing:** xUnit 2.9.3 with FluentAssertions 7.2.2 and NSubstitute 6.2.0; `WebApplicationFactory` 8.0.30 with SQLite in-memory; coverlet 10.0.1; Vitest 5.0.0 for Angular unit tests; Playwright 1.63.0 for end-to-end (new)
- **Build, CI, and delivery:** `dotnet` CLI with central package management; Angular CLI 22.1.7 with npm; docker-compose for local multi-container run; no continuous integration in this iteration (new)
- **Infrastructure and hosting:** SQLite database file; Nginx 1.30.x static host for the Angular build; local only by decision, run directly or through docker compose; never published to the internet (new)
- **Developer tooling:** `dotnet-ef` 8.0.30 local tool; Swashbuckle.AspNetCore 10.2.3 for OpenAPI; ng-openapi-gen 1.0.5 for the Angular client; `dotnet user-secrets`; nvm 0.40.7; `.editorconfig` (new)

### Capability Needs

| Need | Source | Coverage |
|---|---|---|
| HTTP JSON API with bearer-token protected endpoints | Design, API Contract: fifteen endpoints under `/api/v1`, "All endpoints outside `/auth` require `Authorization: Bearer <token>`" | gap |
| Web user interface that calls only the API | Design, Component Diagram: "The Web UI calls only the API, never the database"; user chose Angular | gap |
| Cross-origin access from the client to the API | Derived: user chose a separate static host for the client, so the browser origin differs from the API origin | gap |
| Relational persistence with foreign keys, check constraints, unique indexes | Design, Database Diagram and Migrations: four tables, cascade deletes, `ux_vehicles_user_vin` | gap |
| Schema migrations run at start-up | Design, NFR Operations: "Migrations run automatically at API start-up on an empty schema" | gap |
| Transactional write across record and vehicle | Design, Decision "Advance vehicle mileage inside the maintenance record transaction" | gap |
| Stateless session tokens carrying the user id | Design, Decision "Stateless token-based session authentication" | gap |
| Slow password hashing | Design, NFR Security: "salted hashes from a slow algorithm such as bcrypt or Argon2id" | gap |
| Single-use, hashed, expiring verification and reset tokens | Design, Decision "Single shared token table for email verification and password reset" | gap |
| Outbound email for verification and password reset, with a local sink | Design, Component and Deployment Diagrams: `EmailSender`, "console or local mailbox sink" | gap |
| Rate limiting on login, resend, and forgot-password | Design, NFR Security: "rate-limited per email and per client address"; API Contract 429 responses | gap |
| Server-side input validation returning field errors | Design, NFR Security and API Contract: "400 Bad Request (field errors)" | gap |
| Structured request and application logging without secrets | Design, NFR Observability: "Structured request logs with method, path, status, latency, and `userId`" | gap |
| Counters for business and auth events | Design, NFR Observability: "counters for sign-ups, verifications, logins…" | gap |
| Health reporting and 503 on database failure | Design, NFR Availability: "Database connection failures return 503" | gap |
| Configuration and secrets by environment variables | Design, NFR Operations: signing secret, token lifetimes, email credentials, UI base URL | gap |
| API unit tests with fakes or mocks for services | Design, NFR Maintainability: "unit-testable with in-memory fakes"; user chose NSubstitute | gap |
| API integration tests against the embedded database with a fake email sink | Design, NFR Maintainability: "endpoints are integration-tested against the embedded database with a fake email sink" | gap |
| Client unit tests and end-to-end browser tests | Plan, Phase 2 Likely Affected Areas: "Automated tests for the above"; user chose Vitest and Playwright | gap |
| Build configuration at the repository root | Plan, Phase 2 Likely Affected Areas: "Project/solution scaffolding and build configuration at the repository root" | gap |
| Local multi-container run of API, client host, and their configuration | User choice: docker-compose for local run | gap |
| API contract documentation for the client and reviewers | Design, API Contract section; user chose Swashbuckle | gap |
| Local developer setup | Plan, Phase 2 Risks: `dotnet` not on PATH; Node and PostgreSQL not installed (Docker was installed on 2026-09-07 after Phase 2) | gap |

Not listed because the inputs do not evidence them: caching, background jobs, message brokers, blob storage, distributed tracing. Continuous integration was offered and not selected. The token purge in the design's Data NFR runs as a start-up sweep inside the API and needs no scheduler.

### Stack Definition

#### Language and runtime

| Technology | Version | Source | Status | Serves | Why | Options not chosen | License |
|---|---|---|---|---|---|---|---|
| .NET SDK | 8.0.424 | looked up: `~/.dotnet/dotnet --list-sdks` on this machine | new | every API need | User choice; already installed; pinned via `global.json` | .NET 10 LTS: offered, not chosen, would need an SDK install; .NET 9: STS, same end of support as .NET 8 | MIT |
| .NET runtime and ASP.NET Core shared framework | 8.0.30 | looked up: `~/.dotnet/dotnet --list-runtimes` | new | HTTP API, rate limiting, password hashing, health checks, configuration, logging, CORS | Ships with the SDK; provides `Microsoft.AspNetCore.RateLimiting`, `Microsoft.AspNetCore.Cors`, `Microsoft.Extensions.Identity.Core`, health checks, configuration, and `ILogger` without extra packages | none | MIT |
| C# | 12 | repo: implied by `net8.0` target | new | every API need | Language version bound to the SDK | none | MIT |
| Node.js | 24.20.0 (LTS, supported to 2028-04-30) | looked up: endoflife.date; Angular 22 requires `^22.22.3 \|\| ^24.15.0 \|\| ^26.0.0` per angular.dev | new | Angular build and tests; local developer setup | User choice; active LTS and inside Angular 22's supported range; apt's 22.22.1 is below Angular 22's minimum | Node 22 newer patch: offered, not chosen; Node 26: becomes LTS 2026-10-28, not yet | MIT |
| TypeScript | 6.0.x (resolved by Angular CLI; Angular 22 requires `>=6.0.0 <6.1.0`) | looked up: angular.dev compatibility table | new | Angular client | Fixed by the Angular major; the CLI pins the exact patch | TypeScript 7.0.2 (latest): outside Angular 22's range | Apache-2.0 |

#### Application framework

| Technology | Version | Source | Status | Serves | Why | Options not chosen | License |
|---|---|---|---|---|---|---|---|
| ASP.NET Core Web API (controllers) | 8.0.30 | looked up: shared framework | new | HTTP JSON API; `ProblemDetails` error responses | Controllers map one-to-one onto the design's `AuthController`, `VehicleController`, `MaintenanceController` | Minimal APIs: not offered; controllers match the design's class diagram | MIT |
| ASP.NET Core CORS (`Microsoft.AspNetCore.Cors`) | 8.0.30 | looked up: shared framework | new | Cross-origin access from the client | Required because the client is served from another origin; allowed origins come from configuration | none | MIT |
| Angular (`@angular/core`, `@angular/common`, `@angular/router`, `@angular/forms`, `@angular/platform-browser`) | 22.1.5 | looked up: npm registry `@angular/core@latest` | new | Web user interface that calls only the API | User choice; standalone components, signals, reactive forms, `HttpClient` with an interceptor adding the bearer header and reading `emailVerified` for the verification banner | Blazor WebAssembly, Razor Pages, Blazor Server: offered, not chosen | MIT |
| Angular CLI (`@angular/cli`, `@angular/build`) | 22.1.7 | looked up: npm registry `@angular/cli@latest`; engines `^22.22.3 \|\| ^24.15.0 \|\| >=26.0.0` | new | Angular build and tests | Generates, builds, serves, and tests the client; `ng serve` proxies `/api` to the API locally | none | MIT |
| PrimeNG | 22.1.0 (peer `@angular/core ^22.1.0`) | looked up: npm registry | new | Web user interface: forms, data table for maintenance history, dialogs | User choice; version tracks the Angular major | Angular Material, Bootstrap, plain CSS: offered, not chosen | MIT per `LICENSE.md`; package manifest declares "SEE LICENSE IN LICENSE.md", see Constraints |
| PrimeIcons | 8.0.0 | looked up: npm registry | new | Web user interface | Icon set PrimeNG components expect | none | MIT per `LICENSE.md`, same note as PrimeNG |

#### Persistence

| Technology | Version | Source | Status | Serves | Why | Options not chosen | License |
|---|---|---|---|---|---|---|---|
| Entity Framework Core | 8.0.30 | looked up: NuGet | new | Relational persistence; transactional write | Repositories in the design map onto `DbContext` queries scoped by `userId`; `IDbContextTransaction` covers the record-plus-vehicle write | Dapper: not offered | MIT |
| Microsoft.EntityFrameworkCore.Sqlite | 8.0.30 | looked up: NuGet | new | Relational persistence in local, tests, and hosted environments | User choice "SQLite everywhere": no server or Docker needed, one migration set; in-memory mode for integration tests | PostgreSQL everywhere; SQLite locally with PostgreSQL hosted: offered, not chosen | MIT |
| EF Core Migrations with Microsoft.EntityFrameworkCore.Design | 8.0.30 | looked up: NuGet | new | Schema migrations run at start-up | `Database.Migrate()` at start-up satisfies the Operations NFR; the Design package enables migration generation | FluentMigrator, DbUp: a second migration language | MIT |
| SQLite engine | 3.x bundled with the provider | looked up: NuGet | new | Relational persistence | Bundled native library; adequate for the design's volumes | none | Public domain |

#### Messaging and integration

| Technology | Version | Source | Status | Serves | Why | Options not chosen | License |
|---|---|---|---|---|---|---|---|
| Log-sink `IEmailSender` (own code on `Microsoft.Extensions.Logging`) | 8.0.30 | looked up: shared framework | new | Outbound email for verification and password reset; fake sink for integration tests | The only email sender: writes verification and reset links to the log and captures them in tests. MailKit was chosen in Phase 1 for hosted SMTP and dropped on 2026-09-07 when the project became local only; the `IEmailSender` interface keeps an SMTP adapter a local change if that ever reverses | smtp4dev, MailHog: need an install or Docker | MIT |

#### Security and identity

| Technology | Version | Source | Status | Serves | Why | Options not chosen | License |
|---|---|---|---|---|---|---|---|
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.30 | looked up: NuGet | new | Stateless session tokens | User choice "custom per design + JWT"; validates signed bearer tokens and populates the `userId` claim, the design's "Token filter"; creation via transitive `Microsoft.IdentityModel.JsonWebTokens` | ASP.NET Core Identity: offered, not chosen | MIT |
| `PasswordHasher<TUser>` (`Microsoft.Extensions.Identity.Core`) | 8.0.30 | looked up: shared framework | new | Slow password hashing | PBKDF2-HMAC-SHA512, 100,000 iterations, per-password salt, in the shared framework; implements the design's `PasswordHasher` interface | BCrypt.Net-Next: offered, not chosen | MIT |
| BCL `RandomNumberGenerator` + `SHA256` | 8.0.30 | looked up: BCL | new | Single-use, hashed, expiring verification and reset tokens | 32 random bytes, URL-safe encoded, stored as SHA-256 as the design prescribes | Identity token providers: bound to Identity stores | MIT |
| ASP.NET Core Rate Limiting | 8.0.30 | looked up: shared framework | new | Rate limiting on login, resend, and forgot-password | Built-in fixed-window limiter partitioned by email and client address, returning 429 | AspNetCoreRateLimit package: predates the built-in middleware | MIT |
| Microsoft.Extensions.Configuration + `dotnet user-secrets` | 8.0.30 | looked up: shared framework and SDK | new | Configuration and secrets by environment variables | Environment variables override `appsettings.json`; user secrets keep the signing key out of the repository | Vault or cloud secret stores: not needed for a local-only project | MIT |

#### Observability

| Technology | Version | Source | Status | Serves | Why | Options not chosen | License |
|---|---|---|---|---|---|---|---|
| `Microsoft.Extensions.Logging` with `JsonConsoleFormatter` | 8.0.30 | looked up: shared framework | new | Structured request and application logging without secrets | User choice; JSON console output with scopes for `userId`; request method, path, status, and latency come from a small custom middleware or `HttpLogging` with fields restricted so tokens never appear | Serilog.AspNetCore: offered, not chosen | MIT |
| `System.Diagnostics.Metrics` (`Meter`, `Counter<T>`) | 8.0.30 | looked up: BCL | new | Counters for business and auth events | BCL instrumentation readable locally with `dotnet-counters` and exportable later | OpenTelemetry exporters: no backend yet | MIT |
| ASP.NET Core Health Checks + Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 8.0.30 | looked up: NuGet | new | Health reporting and 503 on database failure | `/health` reports the database through the `DbContext`; the exception handler maps database unavailability to 503 | community health-check packages: not needed for one SQLite check | MIT |

#### Testing

| Technology | Version | Source | Status | Serves | Why | Options not chosen | License |
|---|---|---|---|---|---|---|---|
| xUnit | 2.9.3 | looked up: NuGet index | new | API unit and integration tests | User choice | NUnit: offered, not chosen | Apache-2.0 |
| xunit.runner.visualstudio | 3.1.5 | looked up: NuGet nuspec, runs v2 tests, targets net8.0 | new | API tests | Test adapter for `dotnet test` and IDEs | none | Apache-2.0 |
| Microsoft.NET.Test.Sdk | 18.9.0 | looked up: NuGet nuspec, targets net8.0 | new | API tests | Required test host for `dotnet test` | none | MIT |
| FluentAssertions | 7.2.2 | looked up: NuGet nuspec; targets net6.0 and netstandard2.0, both consumable from net8.0 | new | API tests | User choice; last Apache-2.0 line, fixes only | FluentAssertions 8.x (commercial for non-OSS), Shouldly: offered, not chosen | Apache-2.0 |
| NSubstitute | 6.2.0 | looked up: NuGet nuspec, targets net8.0 | new | API unit tests with mocks for repositories, hasher, token issuer, email sender | User choice | Moq: offered, not chosen | BSD-3-Clause |
| Microsoft.AspNetCore.Mvc.Testing | 8.0.30 | looked up: NuGet | new | API integration tests with SQLite in-memory and the log-sink email sender | `WebApplicationFactory` boots the real pipeline in-process so verification and reset links can be read back in tests | Testcontainers: needs Docker in the test run | MIT |
| coverlet.collector | 10.0.1 | looked up: NuGet nuspec | new | API test coverage | Coverage collection in `dotnet test` | none | MIT |
| Vitest | 5.0.0 (exact patch resolved by Angular CLI's `unit-test` builder) | looked up: npm registry | new | Client unit tests | User choice; the Angular CLI's default runner since Angular 21 | Karma + Jasmine, Jest: offered, not chosen | MIT |
| Playwright (`@playwright/test`) | 1.63.0 | looked up: npm registry | new | End-to-end browser tests | User choice; drives the Chrome already installed on the machine or its own bundled browsers | Cypress, none: offered, not chosen | Apache-2.0 |

#### Build, CI, and delivery

| Technology | Version | Source | Status | Serves | Why | Options not chosen | License |
|---|---|---|---|---|---|---|---|
| `dotnet` CLI with a solution file and `Directory.Packages.props` | 8.0.424 | looked up: local SDK | new | Build configuration at the repository root | User choice of central package management: every NuGet version in one file | Per-project versions: offered, not chosen | MIT |
| npm | bundled with Node 24.20.0 | looked up: Node release | new | Client dependency management | User choice; `package-lock.json` committed | pnpm, yarn: offered, not chosen | Artistic-2.0 |
| Docker Engine with the Compose plugin | Engine 29.8.0, Compose v5.5.1 | looked up: `docker --version` and `docker compose version` on this machine, 2026-09-07 | new | Local multi-container run | User choice; runs the API container, the Nginx static host with the Angular build, and their environment variables; SQLite lives on a named volume | none | Apache-2.0 |
| Dockerfiles for the API (`mcr.microsoft.com/dotnet/sdk:8.0` and `aspnet:8.0` images) and for the client (`node:24-alpine` build stage, Nginx serve stage) | tags `8.0`, `8.0`, `24-alpine`, `1.30-alpine` | looked up: `docker manifest inspect` on this machine, 2026-09-07 | new | Local multi-container run | Compose can only run images that exist; no prebuilt image of this application exists, so the compose choice implies these two Dockerfiles (confirmed in Phase 2) | none | MIT (base images carry their own terms) |
| Continuous integration | none | user choice | n/a | n/a | GitHub Actions CI was offered and not selected for this iteration | GitHub Actions: offered, not chosen | n/a |

#### Infrastructure and hosting

| Technology | Version | Source | Status | Serves | Why | Options not chosen | License |
|---|---|---|---|---|---|---|---|
| SQLite database file on a persistent path or volume | 3.x bundled | looked up: NuGet | new | Relational persistence in every environment | User choice "SQLite everywhere" | PostgreSQL: not chosen | Public domain |
| Nginx as the separate static host for the Angular build | 1.30.4 (current stable cycle) | looked up: endoflife.date; assumed as the host image, see Open Questions | new | Web user interface delivery | User chose a separate static host; Nginx is the conventional container for static files with SPA fallback (`try_files`) | CDN or static site service: viable once a hosting environment is chosen | BSD-2-Clause |

#### Developer tooling

| Technology | Version | Source | Status | Serves | Why | Options not chosen | License |
|---|---|---|---|---|---|---|---|
| `dotnet-ef` (local tool manifest) | 8.0.30 | looked up: NuGet | new | Schema migrations | Pinned in `.config/dotnet-tools.json` | Global tool: unpinned | MIT |
| Swashbuckle.AspNetCore | 10.2.3 | looked up: NuGet nuspec, targets net8.0 | new | API contract documentation | User choice; OpenAPI document plus Swagger UI in Development; the Angular client can generate its typed API client from the document | Microsoft.AspNetCore.OpenApi 8.0.30, none: offered, not chosen | MIT |
| ng-openapi-gen | 1.0.5 (peers `@angular/core >=16`, `rxjs >=6.5`) | looked up: npm registry | new | Generated Angular API client (Phase 2 decision) | User delegated the choice to the simplest option: one npm package, one config file, one command that emits Angular services and models with no runtime dependency | openapi-typescript (types only, services hand-written); @hey-api/openapi-ts (framework-neutral, needs an Angular plugin); openapi-generator-cli (requires Java) | MIT |
| nvm | 0.40.7 | looked up: GitHub releases | new | Local developer setup: Node 24 install | User choice; `.nvmrc` at the repository root pins `24.20.0`; no sudo | NodeSource apt repository, official binary or fnm: offered, not chosen | MIT |
| `global.json` SDK pin, `.nvmrc`, `.editorconfig`, `Nullable` and `TreatWarningsAsErrors` in `Directory.Build.props` | n/a | n/a | new | Local developer setup | Same SDK, Node, formatting, and strictness for everyone | none | n/a |

### Decision Records

Every decision below was made by the user; the record keeps the options that were presented and the consequences the team accepts.

#### .NET 8 despite support ending 2026-11-10

- **Need:** Runtime for every API capability.
- **Decision (user):** .NET SDK 8.0.424 and runtime 8.0.30, the versions already installed.
- **Options presented:** .NET 10 LTS (supported to 2028-11-14, needs an SDK install); .NET 9 (STS, same end of support as .NET 8).
- **Consequences:** The .NET support policy page, read on 2026-09-07, ends .NET 8 support on 2026-11-10. Every package is on the 8.0.x line so a later retarget to `net10.0` is a version bump in `Directory.Packages.props` plus an SDK install. Listed under Risks.

#### Angular 22 on Node 24 LTS, served from a separate static host

- **Need:** Web user interface that calls only the API; design Open Question on UI shape.
- **Decision (user):** Angular 22.1.x with PrimeNG, Node 24.20.0 via nvm, npm, Vitest for unit tests, Playwright for end-to-end, served by a separate static host.
- **Options presented:** Blazor WebAssembly, Razor Pages, Blazor Server; Angular 21 on apt's Node 22; Node 22 newer patch; pnpm or yarn; Angular Material, Bootstrap, plain CSS; Karma or Jest; Cypress or no e2e; static files served by the API.
- **Consequences:** A second language and toolchain (Node, npm, TypeScript) must be installed and documented. The client origin differs from the API origin, so the API needs CORS with the client origin in configuration, and emailed links point at the client host's public URL. The session token lives in browser storage; use `sessionStorage`, a short session lifetime, and a Content Security Policy on the static host. Angular's TypeScript range is narrow (`>=6.0.0 <6.1.0`), so TypeScript upgrades follow Angular releases.

#### SQLite everywhere

- **Need:** Relational persistence; design Open Question on database.
- **Decision (user):** Microsoft.EntityFrameworkCore.Sqlite 8.0.30 in local, tests, and hosted environments; in-memory mode for integration tests.
- **Options presented:** PostgreSQL everywhere; SQLite locally with PostgreSQL hosted.
- **Consequences:** One provider and one migration set. SQLite has no native decimal type, so `cost_usd` is stored as TEXT and the `>= 0` check constraint needs a cast; keep cost queries in LINQ and cover the constraint with an integration test. Writers serialize on the file, acceptable at the design's stated scale. The design's Deployment Diagram assumed a database server when hosted; that assumption is superseded by this decision and should be reflected when the design is next revised.

#### Custom authentication per the design with JWT sessions

- **Need:** Session tokens, password hashing, verification and reset tokens; design `USERS` and `USER_TOKENS` tables.
- **Decision (user):** Implement the design's `AuthService`, `PasswordHasher`, `TokenIssuer`, and `UserTokenRepository`; JWT bearer sessions via Microsoft.AspNetCore.Authentication.JwtBearer 8.0.30; the shared framework's PBKDF2 `PasswordHasher<TUser>`; BCL random tokens hashed with SHA-256.
- **Options presented:** ASP.NET Core Identity; custom per design with BCrypt.Net-Next.
- **Consequences:** The team owns the auth code and its tests, including token expiry, purpose checks, and supersession. Hashing is PBKDF2 rather than a memory-hard function; swapping the `PasswordHasher` implementation later is local.

#### FluentValidation, built-in logging, xUnit with FluentAssertions 7 and NSubstitute, Swashbuckle, central package management

- **Need:** Validation, logging, testing, API documentation, build configuration.
- **Decision (user):** FluentValidation 12.1.1 in the application services; `Microsoft.Extensions.Logging` with the JSON console formatter; xUnit 2.9.3 with FluentAssertions 7.2.2 and NSubstitute 6.2.0; Swashbuckle.AspNetCore 10.2.3; `Directory.Packages.props`.
- **Options presented:** data annotations; Serilog.AspNetCore; NUnit, Shouldly, FluentAssertions 8, Moq; Microsoft.AspNetCore.OpenApi or no documentation; per-project versions.
- **Consequences:** Request logging with latency and status is a small custom middleware or `HttpLogging` with restricted fields, since no request-logging package is used. FluentAssertions 7 receives fixes only; moving to 8 would require a commercial license unless the project is open source. NSubstitute replaces the hand-written fakes the design mentioned; the design's testability NFR is still met.

#### docker-compose for local run, no continuous integration

- **Need:** Local multi-container run; build configuration at the repository root.
- **Decision (user):** docker-compose for local run; GitHub Actions CI and a standalone Dockerfile item were offered and not selected.
- **Options presented:** GitHub Actions CI; multi-stage Dockerfile; docker-compose.
- **Consequences:** Compose needs images to run, and no prebuilt image of this application exists, so two Dockerfiles (API, client-on-Nginx) are implied by this choice; confirmation is requested under Open Questions. Docker Engine 29.8.0 with Compose v5.5.1 was installed on the observed machine on 2026-09-07 and all four base image tags were confirmed to exist. Without CI, `dotnet test`, `ng test`, and Playwright run only on developer machines.

### Dependency Changes

Paths are final: Phase 2 fixed the monorepo layout on 2026-09-07 (see Phase 2, Path Changes to Phase 1). The API lives in `apps/api-maintenance/` as four Clean Architecture projects plus two test projects, the Angular client in `apps/web/`, and Playwright in `apps/e2e/`.

#### `global.json`

| Package | Version | Change | Serves |
|---|---|---|---|
| .NET SDK (`sdk.version`, `rollForward: latestPatch`) | 8.0.424 | add | Local developer setup |

#### `.nvmrc`

| Package | Version | Change | Serves |
|---|---|---|---|
| Node.js | 24.20.0 | add | Local developer setup; Angular build and tests |

#### `apps/api-maintenance/.config/dotnet-tools.json`

| Package | Version | Change | Serves |
|---|---|---|---|
| dotnet-ef | 8.0.30 | add | Schema migrations |

#### `apps/api-maintenance/Directory.Packages.props` (central package management; every NuGet version lives here)

| Package | Version | Change | Serves |
|---|---|---|---|
| Microsoft.EntityFrameworkCore.Sqlite | 8.0.30 | add | Relational persistence |
| Microsoft.EntityFrameworkCore.Design | 8.0.30 | add | Schema migrations |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.30 | add | Session tokens |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 8.0.30 | add | Health reporting |
| Microsoft.Extensions.Identity.Core | 8.0.30 | add (implied by the framework PasswordHasher choice, S09) | Slow password hashing |
| FluentValidation | 12.1.1 | add | Input validation |
| Microsoft.Extensions.DependencyInjection.Abstractions | 8.0.2 | add (implied by FluentValidation registration in `AddApplication`, S07) | Input validation |
| Microsoft.Extensions.Logging.Abstractions | 8.0.2 | add (implied by services logging email failures, S10) | Structured logging |
| Microsoft.Extensions.Options | 8.0.2 | add (implied by services reading Tokens and Client options, S10) | Configuration |
| Swashbuckle.AspNetCore | 10.2.3 | add | API contract documentation |
| xunit | 2.9.3 | add | API tests |
| xunit.runner.visualstudio | 3.1.5 | add | API tests |
| Microsoft.NET.Test.Sdk | 18.9.0 | add | API tests |
| FluentAssertions | 7.2.2 | add | API tests |
| NSubstitute | 6.2.0 | add | API unit tests |
| Microsoft.AspNetCore.Mvc.Testing | 8.0.30 | add | API integration tests |
| coverlet.collector | 10.0.1 | add | API test coverage |

#### `apps/api-maintenance/src/Maintenance.Domain/Maintenance.Domain.csproj`

| Package | Version | Change | Serves |
|---|---|---|---|
| none | | | Entities and repository interfaces have no package dependencies |

#### `apps/api-maintenance/src/Maintenance.Application/Maintenance.Application.csproj`

| Package | Version | Change | Serves |
|---|---|---|---|
| FluentValidation | 12.1.1 | add | Input validation |
| Microsoft.Extensions.DependencyInjection.Abstractions | 8.0.2 | add (implied, S07) | Validator registration |
| Microsoft.Extensions.Logging.Abstractions | 8.0.2 | add (implied, S10) | Logging from services |
| Microsoft.Extensions.Options | 8.0.2 | add (implied, S10) | Options in services |

#### `apps/api-maintenance/src/Maintenance.Infrastructure/Maintenance.Infrastructure.csproj`

| Package | Version | Change | Serves |
|---|---|---|---|
| Microsoft.EntityFrameworkCore.Sqlite | 8.0.30 | add | Relational persistence |
| Microsoft.Extensions.Identity.Core | 8.0.30 | add (implied, S09) | Slow password hashing |

#### `apps/api-maintenance/src/Maintenance.Api/Maintenance.Api.csproj`

| Package | Version | Change | Serves |
|---|---|---|---|
| Microsoft.EntityFrameworkCore.Design | 8.0.30 | add | Schema migrations (design-time) |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.30 | add | Session tokens |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 8.0.30 | add | Health reporting |
| Swashbuckle.AspNetCore | 10.2.3 | add | API contract documentation |

#### `apps/api-maintenance/tests/Maintenance.UnitTests/Maintenance.UnitTests.csproj`

| Package | Version | Change | Serves |
|---|---|---|---|
| xunit | 2.9.3 | add | API unit tests |
| xunit.runner.visualstudio | 3.1.5 | add | API unit tests |
| Microsoft.NET.Test.Sdk | 18.9.0 | add | API unit tests |
| FluentAssertions | 7.2.2 | add | API unit tests |
| NSubstitute | 6.2.0 | add | API unit tests |
| coverlet.collector | 10.0.1 | add | Coverage |

#### `apps/api-maintenance/tests/Maintenance.IntegrationTests/Maintenance.IntegrationTests.csproj`

| Package | Version | Change | Serves |
|---|---|---|---|
| xunit | 2.9.3 | add | API integration tests |
| xunit.runner.visualstudio | 3.1.5 | add | API integration tests |
| Microsoft.NET.Test.Sdk | 18.9.0 | add | API integration tests |
| FluentAssertions | 7.2.2 | add | API integration tests |
| Microsoft.AspNetCore.Mvc.Testing | 8.0.30 | add | API integration tests |
| coverlet.collector | 10.0.1 | add | Coverage |

#### `apps/web/package.json` (generated by `ng new`; the CLI fills the remaining `@angular/*` and tooling entries at 22.1.x)

| Package | Version | Change | Serves |
|---|---|---|---|
| @angular/core, @angular/common, @angular/router, @angular/forms, @angular/platform-browser (dependencies) | 22.1.5 | add | Web user interface |
| primeng | 22.1.0 | add | Web user interface |
| primeicons | 8.0.0 | add | Web user interface |
| @angular/cli, @angular/build (devDependencies) | 22.1.7 | add | Angular build and tests |
| typescript (devDependency) | 6.0.x as pinned by the CLI | add | Angular client |
| vitest (devDependency) | 5.0.0 or the patch the CLI pins | add | Client unit tests |
| ng-openapi-gen (devDependency) | 1.0.5 | add | Generated API client in `src/app/api/` |

#### `apps/e2e/package.json`

| Package | Version | Change | Serves |
|---|---|---|---|
| @playwright/test (devDependency) | 1.63.0 | add | End-to-end tests |

#### `docker/docker-compose.yml`, `docker/api-maintenance/Dockerfile`, `docker/web/Dockerfile`

| Package | Version | Change | Serves |
|---|---|---|---|
| mcr.microsoft.com/dotnet/sdk and aspnet base images | 8.0 (tags verified) | add | Local multi-container run |
| node base image | 24-alpine (tag verified) | add | Local multi-container run (client build stage) |
| nginx base image | 1.30-alpine (tag verified) | add | Web user interface delivery |

### Infrastructure and Services

| Service | Purpose | Serves | Status | Environments |
|---|---|---|---|---|
| SQLite database file | Sole relational store; path from configuration; named volume under compose | Relational persistence | new | local (file or compose volume), tests (in-memory) |
| Nginx static host | Serves the Angular build with SPA fallback and security headers; public URL used in emailed links | Web user interface delivery | new | local via compose |
| Docker Engine with the Compose plugin | Runs the local multi-container stack | Local multi-container run | new | developer machines; Engine 29.8.0 and Compose v5.5.1 installed on the observed machine |
| Secret storage for the JWT signing key | Keeps secrets out of the repository | Configuration and secrets | new | local (`dotnet user-secrets` or a git-ignored compose `.env`) |

### Constraints and Compliance

- **User decisions:** all twenty technology choices were made by the user in this run and are listed under Questions Asked & Answers; this document does not substitute its own preferences.
- **Support windows:** .NET 8 (LTS) support ends **2026-11-10**; .NET 10 (LTS) runs to 2028-11-14. Node 24 (LTS) runs to 2028-04-30. Both read on 2026-09-07.
- **Local toolchain:** `dotnet` is at `~/.dotnet` but not on PATH; Node and PostgreSQL are absent; Docker Engine 29.8.0 with Compose v5.5.1 is installed. The README must cover adding `dotnet` to PATH, installing nvm and running `nvm install`, and installing Docker from Docker's apt repository for the compose stack.
- **Angular version coupling:** Angular 22 requires Node `^22.22.3 || ^24.15.0 || ^26.0.0` and TypeScript `>=6.0.0 <6.1.0`; PrimeNG 22.1.0 requires `@angular/core ^22.1.0`. Upgrades of these three move together.
- **Licenses:** all new dependencies are MIT, Apache-2.0, BSD, or public domain. PrimeNG and PrimeIcons declare "SEE LICENSE IN LICENSE.md" in their manifests rather than an SPDX identifier; the bundled `LICENSE.md` is the MIT license, but automated license scanners may flag it. FluentAssertions is held at the 7.x Apache-2.0 line; 8.x requires a commercial license for non-open-source use.
- **Registries and approvals:** none detected; nuget.org and registry.npmjs.org are the implied sources.
- **Central package management:** every NuGet version belongs in `Directory.Packages.props`; project files must not carry versions.

### Risks

| Risk | Impact | Mitigation |
|---|---|---|
| .NET 8 leaves support nine weeks after this definition | Security patches stop for the runtime and every 8.0.x package | Keep all packages on the 8.0.x line; retargeting to `net10.0` is a `Directory.Build.props` and `Directory.Packages.props` change plus an SDK install |
| SQLite stores `decimal` as TEXT | Ordering and comparisons on `cost_usd` rely on EF Core translation; the raw check constraint needs a cast | Keep cost queries in LINQ; configure the check constraint in the EF model; cover with an integration test |
| Two origins (client host and API) | Misconfigured CORS blocks the client or opens the API too widely | Allowed origins from configuration only; no wildcard; preflight covered by an integration test |
| Bearer token held in browser storage | Exposed to script injection | `sessionStorage`, short session lifetime, Content Security Policy from the Nginx host, no inline scripts |
| No continuous integration | Regressions are caught only when a developer runs the suites | Document the three test commands in the README; revisit CI when the user chooses |
| PrimeNG license not declared as SPDX | License scanners may flag it | Record MIT from `LICENSE.md` in the license inventory |
| Narrow TypeScript range for Angular 22 | A stray `typescript` upgrade breaks the build | Let the Angular CLI own the `typescript` version; do not bump it independently |
| Microsoft.NET.Test.Sdk 18.9.0 far ahead of the 8.0 SDK | Possible MSBuild incompatibility with SDK 8.0.4xx | The nuspec targets net8.0; fall back to the 17.x line if restore fails and record it |
| Vitest integration in the Angular CLI is recent | Builder options may change between Angular minors | Pin `@angular/build` and `vitest` together; upgrade in lockstep |
| Single database file serializes writers | Throughput ceiling if usage exceeds the design's assumptions | Repository abstraction and EF provider keep a later PostgreSQL swap local to Infrastructure and migrations |

### Assumptions and Open Questions

#### Assumptions

- The .NET project layout follows the Clean Architecture split; folder and project names were fixed by Phase 2.
- Nginx is the static host image in compose; the user chose "separate static host" without naming one.
- The compose choice implies a Dockerfile for the API and one for the client build served by Nginx.
- Angular runs zoneless with signals, the default for new Angular 22 applications, so `zone.js` is not a dependency.
- Local secrets live in `dotnet user-secrets` for `dotnet run` and in a git-ignored `.env` for compose.
- Token purge runs as a start-up sweep, so no scheduler is needed.

#### Open Questions


#### Questions Asked & Answers

| Question | Recommended | Answer |
|---|---|---|
| Which ecosystem should the Vehicle Maintenance Tracker be built in? | Java 17 + Maven | .NET 8. |
| Which .NET runtime should the solution target? | not labelled (round predates the decision protocol) | .NET 8 (installed). |
| How should the web UI be built? | not labelled (round predates the decision protocol) | Angular. |
| Which database should the application use? | not labelled (round predates the decision protocol) | SQLite everywhere. |
| How should authentication be implemented? | not labelled (round predates the decision protocol) | Custom per design + JWT. |
| Which validation approach for the API's input rules? | not labelled (round predates the decision protocol) | FluentValidation 12.1.1. |
| Which logging setup? | not labelled (round predates the decision protocol) | Built-in ILogger + JSON console. |
| Which test framework and helpers? | not labelled (round predates the decision protocol) | xUnit 2.9.3, an assertion library, a mocking library. |
| How should the API send verification and reset emails when hosted? | not labelled (round predates the decision protocol) | MailKit 4.17.0 over SMTP. Superseded on 2026-09-07: project is local only, so MailKit was dropped and the log sink is the only sender. |
| Keep or drop MailKit now that the project is local only? | Keep (flows complete rather than stubbed) | Drop: "the easier and simplest one". |
| Which assertion library? | not labelled (round predates the decision protocol) | FluentAssertions 7.x. |
| Which mocking library? | not labelled (round predates the decision protocol) | NSubstitute. |
| How should the API document its OpenAPI contract? | not labelled (round predates the decision protocol) | Swashbuckle.AspNetCore 10.2.3. |
| How should NuGet package versions be declared? | not labelled (round predates the decision protocol) | Central package management. |
| Which Angular version and Node.js runtime? | not labelled (round predates the decision protocol) | Angular 22 + Node 24 LTS. |
| Which package manager for the Angular project? | not labelled (round predates the decision protocol) | npm. |
| Which UI component library for the Angular client? | not labelled (round predates the decision protocol) | PrimeNG. |
| Which unit test runner for the Angular client? | not labelled (round predates the decision protocol) | Vitest. |
| How should the Angular client be served? | not labelled (round predates the decision protocol) | Separate static host. |
| End-to-end browser tests for the client? | not labelled (round predates the decision protocol) | Playwright. |
| Which delivery pieces should this iteration include? | not labelled (round predates the decision protocol) | docker-compose for local run (GitHub Actions CI and a standalone Dockerfile item not selected). |
| How should Node 24 be installed on developer machines? | not labelled (round predates the decision protocol) | nvm. |
| Confirm Nginx as the static host image? | Nginx 1.30.x | Confirmed through the Phase 2 docker/ decision: `docker/web/Dockerfile` has a Node build stage and an Nginx serve stage. |
| Does docker-compose include writing the two Dockerfiles? | Yes | Confirmed through the Phase 2 docker/ decision: `docker/api-maintenance/Dockerfile` and `docker/web/Dockerfile`. |

## Phase 2 — Monorepo Layout

Layout fixed on 2026-09-07 from the Phase 1 stack in three question rounds (twelve decisions). Repository inspection was read-only: the repository holds `.git/`, `README.md`, and `Docs/` with the plan, the design, and this document.

### Layout at a Glance

```text
TechnicalTestModernTech/
├── .git/
├── .claude/
│   ├── CLAUDE.md                          folder map, commands, conventions for Claude Code sessions
│   └── settings.json                      project permissions for read-only and test commands
├── .github/
│   └── PULL_REQUEST_TEMPLATE.md           asks for plan, design, and test evidence links
├── apps/
│   ├── api-maintenance/                   ASP.NET Core 8 Web API (Clean Architecture)
│   │   ├── Maintenance.sln
│   │   ├── Directory.Build.props          TFM net8.0, Nullable, TreatWarningsAsErrors
│   │   ├── Directory.Packages.props       central NuGet versions (Phase 1)
│   │   ├── .config/dotnet-tools.json      dotnet-ef 8.0.30
│   │   ├── .env.example                   API variables for local runs and compose
│   │   ├── src/
│   │   │   ├── Maintenance.Domain/        entities, repository interfaces
│   │   │   ├── Maintenance.Application/   services, validators, use-case contracts
│   │   │   ├── Maintenance.Infrastructure/ EF Core + SQLite, migrations, log-sink email sender
│   │   │   └── Maintenance.Api/           controllers, auth, rate limiting, Swashbuckle, Program.cs
│   │   └── tests/
│   │       ├── Maintenance.UnitTests/     xUnit + FluentAssertions + NSubstitute
│   │       └── Maintenance.IntegrationTests/  WebApplicationFactory + SQLite in-memory
│   ├── web/                               Angular 22 client (Angular CLI workspace)
│   │   ├── package.json, package-lock.json
│   │   ├── angular.json, tsconfig*.json
│   │   ├── proxy.conf.json                ng serve proxy to the API
│   │   └── src/
│   │       ├── app/api/                   generated OpenAPI client (committed)
│   │       ├── app/                       features, PrimeNG components, specs beside components
│   │       └── environments/              API base URL per environment
│   └── e2e/                               Playwright end-to-end tests
│       ├── package.json, package-lock.json
│       ├── playwright.config.ts
│       ├── .env.example                   base URLs of web and API under test
│       └── tests/
├── docker/
│   ├── docker-compose.yml                 api-maintenance + web (Nginx) + SQLite volume
│   ├── .env.example                       compose variables
│   ├── api-maintenance/Dockerfile         sdk:8.0 build stage, aspnet:8.0 runtime stage
│   └── web/
│       ├── Dockerfile                     node:24 build stage, nginx serve stage
│       └── nginx.conf                     SPA fallback, security headers, CSP
├── docs/
│   ├── plans/Vehicle Maintenance Tracker.md
│   ├── designs/Vehicle Maintenance Tracker.md  (+ .html)
│   ├── implementation/Vehicle Maintenance Tracker.md   this document
│   └── adr/README.md                      reserved for architecture decision records
├── scripts/
│   ├── setup.sh                           dotnet on PATH, nvm install, npm install for web and e2e
│   ├── dev.sh                             API + ng serve
│   ├── test.sh                            dotnet test, ng test, playwright test
│   ├── migrate.sh                         dotnet ef migrations add / database update
│   ├── generate-api-client.sh             regenerate apps/web/src/app/api from the OpenAPI document
│   ├── compose-up.sh
│   └── compose-down.sh
├── README.md
├── global.json                            .NET SDK 8.0.424, rollForward latestPatch
├── .nvmrc                                 24.20.0
├── .editorconfig
└── .gitignore
```

### Applications

| App | Folder | Purpose | Built with (Phase 1) | Entry point | Own tests |
|---|---|---|---|---|---|
| api-maintenance | `apps/api-maintenance/` | JSON REST API for auth, vehicles, and maintenance records | .NET 8, ASP.NET Core, EF Core + SQLite, FluentValidation, JWT bearer, Swashbuckle | `src/Maintenance.Api/Program.cs`; `dotnet run --project src/Maintenance.Api` | `tests/Maintenance.UnitTests`, `tests/Maintenance.IntegrationTests` |
| web | `apps/web/` | Angular single-page client served by Nginx | Angular 22.1.x, PrimeNG 22.1.0, TypeScript 6.0.x, Vitest | `src/main.ts`; `ng serve` | Vitest specs beside components (`*.spec.ts`) |
| e2e | `apps/e2e/` | Browser tests across web and API | Playwright 1.63.0, Node 24 | `playwright.config.ts`; `npx playwright test` | is the cross-app suite |

All three fixed app folders are present; Phase 1 chose a web client and end-to-end tests.

### Folder Purposes

| Folder | Purpose | Contents | Status |
|---|---|---|---|
| `.claude/` | Claude Code project configuration | `CLAUDE.md`, `settings.json` | new |
| `.github/` | GitHub metadata | `PULL_REQUEST_TEMPLATE.md` only; no workflows (no CI in Phase 1) | new |
| `apps/` | Runnable units | `api-maintenance/`, `web/`, `e2e/` | new |
| `docker/` | Everything container-related | compose, `.env.example`, one Dockerfile folder per app, `nginx.conf` | new |
| `docs/` | Project documentation | `plans/`, `designs/`, `implementation/`, `adr/` | moved from `Docs/` |
| `scripts/` | Repository-level commands | seven Bash scripts delegating to per-app commands | new |

### Internal Structure

#### `apps/api-maintenance/`

```text
apps/api-maintenance/
├── Maintenance.sln                        references the four src projects and two test projects
├── Directory.Build.props                  shared MSBuild: net8.0, Nullable, TreatWarningsAsErrors, ImplicitUsings
├── Directory.Packages.props               every NuGet version (Phase 1 Dependency Changes)
├── .config/dotnet-tools.json              dotnet-ef
├── .env.example                           ConnectionStrings__Default, Jwt__SigningKey, Jwt__Lifetime, Cors__ClientOrigin, Client__BaseUrl, Tokens__Lifetime (00:30:00), Auth__RequireEmailVerification (false)
├── src/
│   ├── Maintenance.Domain/                User, UserToken, Vehicle, MaintenanceRecord; repository interfaces; no package references
│   ├── Maintenance.Application/           AuthService, VehicleService, MaintenanceService; FluentValidation validators; PasswordHasher, TokenIssuer, EmailSender interfaces
│   ├── Maintenance.Infrastructure/        DbContext, EF configurations, Migrations/, repositories, log-sink EmailSender, SHA-256 token hashing
│   └── Maintenance.Api/                   Controllers/, JWT bearer setup, rate limiting, CORS, health checks, Swashbuckle, JSON console logging, appsettings*.json, Program.cs
└── tests/
    ├── Maintenance.UnitTests/             services and validators with NSubstitute doubles
    └── Maintenance.IntegrationTests/      WebApplicationFactory against SQLite in-memory with the log-sink email sender
```

Dependency direction inside the app: `Api → Application → Domain`, `Infrastructure → Application → Domain`; `Api` references `Infrastructure` only to register implementations. New entities go in Domain, new rules and validators in Application, new persistence or external adapters in Infrastructure, new endpoints in Api.

#### `apps/web/`

```text
apps/web/
├── package.json, package-lock.json        Angular 22.1.x, PrimeNG, PrimeIcons, Vitest (Phase 1)
├── angular.json, tsconfig.json, tsconfig.app.json, tsconfig.spec.json
├── proxy.conf.json                        /api → http://localhost:<api-port> for ng serve
├── public/                                static assets
└── src/
    ├── main.ts, index.html, styles.css
    ├── environments/                      environment.ts, environment.development.ts (apiBaseUrl)
    └── app/
        ├── api/                           generated OpenAPI client; regenerated by scripts/generate-api-client.sh, never edited by hand
        ├── core/                          auth interceptor, auth state (signals), guards, verification banner
        ├── features/                      auth/, vehicles/, maintenance/ (standalone components, PrimeNG)
        └── shared/                        reusable components and pipes
```

Angular CLI generates the workspace files; `core/`, `features/`, and `shared/` are the only structure imposed. Specs live beside their components.

#### `apps/e2e/`

```text
apps/e2e/
├── package.json, package-lock.json        @playwright/test 1.63.0
├── playwright.config.ts                   baseURL from .env, Chromium project, HTML reporter
├── .env.example                           WEB_BASE_URL, API_BASE_URL
└── tests/                                 auth.spec.ts, vehicles.spec.ts, maintenance.spec.ts
```

`e2e` depends only on running instances of `web` and `api-maintenance`; it imports nothing from either app.

### Root Files

| File | Purpose | From Phase 1 |
|---|---|---|
| `README.md` | Entry point: what the project is, the folder map, `scripts/` usage, links into `docs/` | no |
| `global.json` | Pins .NET SDK 8.0.424 for every folder below the root | yes |
| `.nvmrc` | Pins Node 24.20.0 for `apps/web` and `apps/e2e` via nvm | yes |
| `.editorconfig` | Formatting for C#, TypeScript, JSON, YAML, Markdown | yes |
| `.gitignore` | .NET, Node, Angular, Playwright outputs; `.env`; SQLite files; `docker/.env` | no |

### Path Changes to Phase 1

| Proposed path (Phase 1) | Final path | Reason |
|---|---|---|
| `.config/dotnet-tools.json` | `apps/api-maintenance/.config/dotnet-tools.json` | Solution and tooling live inside the API app |
| `Directory.Packages.props` | `apps/api-maintenance/Directory.Packages.props` | Same; keeps the root free of .NET-only files |
| `src/VehicleMaintenanceTracker.Domain/VehicleMaintenanceTracker.Domain.csproj` | `apps/api-maintenance/src/Maintenance.Domain/Maintenance.Domain.csproj` | App folder plus the `Maintenance` namespace root from the `api-maintenance` name |
| `src/VehicleMaintenanceTracker.Application/…` | `apps/api-maintenance/src/Maintenance.Application/Maintenance.Application.csproj` | Same |
| `src/VehicleMaintenanceTracker.Infrastructure/…` | `apps/api-maintenance/src/Maintenance.Infrastructure/Maintenance.Infrastructure.csproj` | Same |
| `src/VehicleMaintenanceTracker.Api/…` | `apps/api-maintenance/src/Maintenance.Api/Maintenance.Api.csproj` | Same |
| `tests/VehicleMaintenanceTracker.UnitTests/…` | `apps/api-maintenance/tests/Maintenance.UnitTests/Maintenance.UnitTests.csproj` | App-owned tests live inside the app |
| `tests/VehicleMaintenanceTracker.IntegrationTests/…` | `apps/api-maintenance/tests/Maintenance.IntegrationTests/Maintenance.IntegrationTests.csproj` | Same |
| `client/package.json` | `apps/web/package.json` | Fixed `apps/web` folder |
| `client/package.json` (`@playwright/test` row) | `apps/e2e/package.json` | Playwright is its own app with an independent manifest |
| `docker-compose.yml` | `docker/docker-compose.yml` | Everything container-related under `docker/` |
| `src/VehicleMaintenanceTracker.Api/Dockerfile` | `docker/api-maintenance/Dockerfile` | Dockerfile per app under `docker/<app>/` |
| `client/Dockerfile` | `docker/web/Dockerfile` (+ `docker/web/nginx.conf`) | Same; confirms Nginx as the static host |
| `global.json`, `.nvmrc` | unchanged | Root pins apply to every folder below |

### Conventions

- **Naming:** app folders are lower-case kebab-case (`api-maintenance`, `web`, `e2e`); .NET projects and namespaces start with `Maintenance.`; Angular features are folders under `src/app/features/` named by domain (`vehicles`, `maintenance`, `auth`); scripts are verbs (`setup.sh`, `dev.sh`); container services in compose carry the app folder name.
- **Dependency direction:** `web → api-maintenance` over HTTP only, through the generated client in `src/app/api/`; `e2e → web` and `e2e → api-maintenance` over HTTP only; apps never reference each other's source; inside the API, `Api → Application → Domain` and `Infrastructure → Application → Domain`.
- **Where new code goes:** a new entity or repository interface in `Maintenance.Domain`; a new rule, service, or validator in `Maintenance.Application`; a new table mapping, migration, or external adapter in `Maintenance.Infrastructure`; a new endpoint in `Maintenance.Api/Controllers`; a new screen in `apps/web/src/app/features/<domain>/`; a new browser journey in `apps/e2e/tests/`; a new container concern in `docker/`; a new repository command in `scripts/`.
- **Commands (as they will exist):** `scripts/setup.sh` once per machine; `scripts/dev.sh` for API + `ng serve`; `scripts/test.sh` for all suites, or per app `dotnet test` in `apps/api-maintenance`, `npm test` in `apps/web`, `npx playwright test` in `apps/e2e`; `scripts/migrate.sh add <Name>` and `scripts/migrate.sh update`; `scripts/generate-api-client.sh` after any contract change; `scripts/compose-up.sh` / `compose-down.sh` for the container stack.
- **Configuration and secrets:** each app documents its variables in its own `.env.example`; `docker/.env.example` documents compose's; real `.env` files, `docker/.env`, SQLite files, and `dotnet user-secrets` content are never committed; Angular reads its API base URL from `environments/`, never from `.env`.

### Decision Records

#### API layering and test placement

- **Need:** Internal structure of `apps/api-maintenance/`; where its unit and integration tests live (Phase 1 Dependency Changes mapped packages to four layers).
- **Decision (user):** Clean Architecture with four projects under `src/`, tests under `tests/` inside the app.
- **Recommended:** same as decision; matches the design's class diagram and Phase 1's package-to-layer mapping.
- **Options presented:** two projects Api + Core (fewer projects / inversion by convention only); single project with folders (least ceremony / no enforced boundaries); all tests in `apps/e2e` (one place / fights both toolchains); unit in-app with integration in `apps/e2e` (keeps e2e for pipeline tests / mixes in-process .NET tests with Playwright).
- **Consequences:** Six projects to maintain; the boundaries the design draws are enforced by project references. `apps/e2e` holds only Playwright.

#### Solution file inside the app and independent Node manifests

- **Need:** Placement of workspace-level manifests: the .NET solution and the Node package manifests.
- **Decision (user):** `Maintenance.sln`, `Directory.Build.props`, `Directory.Packages.props`, and `.config/dotnet-tools.json` inside `apps/api-maintenance/`; `apps/web` and `apps/e2e` each own a `package.json` and lockfile; no root Node manifest.
- **Recommended:** same as decision; keeps the root free of ecosystem-specific files and avoids npm-workspace hoisting issues with the Angular CLI.
- **Options presented:** root `.sln` (IDE convenience / root gains .NET tooling files); root npm workspaces (one install / hoisting workarounds and a root Node manifest).
- **Consequences:** `dotnet` commands run from `apps/api-maintenance`; `global.json` stays at the root so the pin covers the app. A second API later needs its own solution or a root one. Shared Node devDependencies are declared twice.

#### API name `api-maintenance`

- **Need:** The `<name>` in `apps/api-<name>/` and the namespace root.
- **Decision (user):** `api-maintenance`; projects and namespaces start with `Maintenance.`.
- **Recommended:** same as decision; short and names what the API serves.
- **Options presented:** `api-vehicle-maintenance` (fully descriptive / long names everywhere); `api-tracker` (mirrors the product name / says nothing about the domain).
- **Consequences:** Phase 1 project names `VehicleMaintenanceTracker.*` became `Maintenance.*`; the compose service and Dockerfile folder carry the same name.

#### `docker/` with a Dockerfile per app

- **Need:** Placement of the Dockerfiles and compose files Phase 1's compose choice implies.
- **Decision (user):** `docker/docker-compose.yml`, `docker/.env.example`, `docker/api-maintenance/Dockerfile`, `docker/web/Dockerfile` with `nginx.conf`.
- **Recommended:** same as decision; everything container-related under the fixed `docker/` folder.
- **Options presented:** Dockerfile inside each app with only compose in `docker/` (conventional for single-app repos / splits container concerns across folders).
- **Consequences:** Build contexts point at `apps/<app>` from `docker/`; this decision also closed two Phase 1 open questions (Nginx as the static host, Dockerfiles implied by compose). Docker Engine 29.8.0 and Compose v5.5.1 are installed on the observed machine and the base image tags exist; the Dockerfiles themselves are verified when written.

#### `scripts/` as Bash scripts

- **Need:** Repository-level commands for setup, run, test, migrate, and the container stack.
- **Decision (user):** Seven Bash scripts, each delegating to per-app commands.
- **Recommended:** same as decision; matches the Linux development machine and needs no extra tool.
- **Options presented:** root Makefile delegating to scripts (discoverable `make help` / extra root file and `make` dependency); Bash plus PowerShell twins (cross-platform / double maintenance); npm scripts in a root `package.json` (single entry / contradicts the independent-manifest decision).
- **Consequences:** Windows contributors need WSL or Git Bash; noted under Risks. `generate-api-client.sh` was added as implied by the API client decision.

#### `.github/` holds only a pull request template

- **Need:** Contents of the fixed `.github/` folder with no CI chosen in Phase 1.
- **Decision (user):** `PULL_REQUEST_TEMPLATE.md`.
- **Recommended:** same as decision; cheap and keeps the phase documents attached to changes.
- **Options presented:** CODEOWNERS (ownership map / single developer, no teams named); Dependabot (updates for a runtime near end of support / PRs nobody validates without CI); README placeholder only (nothing to maintain / does no work).
- **Consequences:** No automation in `.github/`; dependency updates and .NET 8 end-of-support tracking remain manual.

#### `.claude/` with CLAUDE.md and settings.json

- **Need:** Contents of the fixed `.claude/` folder.
- **Decision (user):** `CLAUDE.md` (folder map, commands, conventions) and `settings.json` (project permissions). Project skills not vendored.
- **Recommended:** `CLAUDE.md` only; the user added `settings.json`.
- **Options presented:** vendored project skills (self-contained repo / duplicates and drifts from GenaiSkills); README placeholder only (nothing to maintain / no guidance for sessions).
- **Consequences:** `CLAUDE.md` must be kept in step with this layout by hand; `settings.json` encodes one contributor's permission preferences and may need per-contributor overrides in `settings.local.json`.

#### `docs/` with subfolders, moving the existing `Docs/`

- **Need:** Contents of the fixed `docs/` folder and the fate of the existing `Docs/` folder found in the repository.
- **Decision (user):** Move `Docs/` to `docs/` with `plans/`, `designs/`, `implementation/`, and a reserved `adr/`.
- **Recommended:** same as decision; lower-case matches the fixed layout and Linux case-sensitivity.
- **Options presented:** keep `Docs/` untouched (no churn / breaks the fixed layout, leaves `stacks/` misnamed); flat `docs/` (simple / plan and design share a filename).
- **Consequences:** Three files move, including this document (`Docs/stacks/` to `docs/implementation/`); links inside the plan, design, and this document must be updated at the same time. The skill's default output folder name (`stacks`) differs from this repository's `implementation/`; the explicit output directory must be given on later runs.

#### Environment files per app plus compose

- **Need:** Where environment variables are documented and how secrets flow (Phase 1 Configuration and secrets need).
- **Decision (user):** `apps/api-maintenance/.env.example`, `apps/e2e/.env.example`, Angular `environments/`, and `docker/.env.example`; real files git-ignored; `dotnet user-secrets` for local runs outside compose.
- **Recommended:** same as decision; each app documents only what it reads.
- **Options presented:** single root `.env.example` (one file / different working directories and Angular cannot read it); no `.env` files (fewer files / compose still needs one).
- **Consequences:** Three places to look; the README's setup section lists all of them. `apps/e2e/.env.example` is implied by this decision.

#### Generated API client inside `apps/web`

- **Need:** Where the Angular client generated from the Swashbuckle OpenAPI document lives.
- **Decision (user):** `apps/web/src/app/api/`, committed, regenerated by `scripts/generate-api-client.sh`.
- **Recommended:** same as decision; one consumer today, so no `packages/` folder.
- **Options presented:** `packages/api-client` shared package (reusable by e2e / extra top-level folder for one consumer); hand-written services (no tooling / drifts silently from the API).
- **Consequences:** The generator is ng-openapi-gen 1.0.5, chosen after Phase 2 when the user delegated the pick to the simplest option; it is a devDependency of `apps/web`. The top level stays at the fixed set; `packages/` is not added.

### Constraints and Compliance

- User decisions: all twelve layout choices were made by the user; eleven matched the recommendation and one (`.claude/`) added `settings.json` to it.
- Tool constraints that fixed placements: `global.json` must sit at or above every .NET project, so it stays at the root; `Directory.Build.props` and `Directory.Packages.props` must sit at or above the projects they govern, so they sit in `apps/api-maintenance/`; the Angular CLI owns `apps/web`'s workspace files; Playwright expects `playwright.config.ts` at its package root.
- Existing folders that move: `Docs/` to `docs/` (three documents), with `stacks/` renamed to `implementation/`.
- The fixed top level is complete: every folder from the skill's layout is present; no folder was added beyond it.

### Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Moving `Docs/` to `docs/` on a case-insensitive checkout | Git may treat the rename as a no-op or conflict on Windows or macOS | Rename in two steps (`Docs` → `docs-tmp` → `docs`) and update links in the same commit |
| Dockerfiles in `docker/<app>/` with build contexts in `apps/` | A wrong context path breaks image builds | Compose declares `context: ../apps/<app>` and `dockerfile: ../docker/<app>/Dockerfile` explicitly; verified by `docker compose build` once the files exist |
| Bash-only scripts | Windows contributors cannot run `scripts/` natively | Document WSL or Git Bash in the README; revisit twins if a Windows contributor joins |
| Generated client committed in `apps/web/src/app/api/` | Stale client after an API change compiles but calls the wrong contract | `scripts/generate-api-client.sh` is part of the definition of done for API changes; CLAUDE.md says so |
| `CLAUDE.md` drift from the layout | Sessions follow outdated instructions | Update `CLAUDE.md` in the same change as any folder move |
| Two lockfiles for Node apps | Duplicate devDependencies drift between `web` and `e2e` | Keep shared tooling versions listed in this document's Phase 1 tables |

### Assumptions and Open Questions

#### Assumptions

- The `Maintenance.` namespace root follows from the `api-maintenance` name; the design's class names are unchanged beneath it.
- `apps/web/src/app/` uses `core/`, `features/`, and `shared/` as the only imposed structure; everything else is what the Angular CLI generates.
- `docs/adr/` is reserved and holds only a README until the first decision record is written.
- `docker/web/nginx.conf` carries the SPA fallback and the Content Security Policy the design's security NFR requires.
- `apps/e2e/.env.example` and `scripts/generate-api-client.sh` are implied by the environment-file and API-client decisions and were not asked separately.

#### Open Questions


#### Questions Asked & Answers

| Question | Recommended | Answer |
|---|---|---|
| How should the .NET API app be layered internally? | Clean Architecture, 4 projects | Clean Architecture, 4 projects. |
| Where do each app's own unit and integration tests live? | Inside each app under `tests/` | Inside each app under `tests/`. |
| Where does the .NET solution file live? | Inside `apps/api-<name>/` | Inside `apps/api-<name>/`. |
| Should the Node apps share a root npm workspace? | Independent `package.json` per app | Independent `package.json` per app. |
| What is the `<name>` in `apps/api-<name>/`? | `api-maintenance` | `api-maintenance`. |
| How should `docker/` be organised? | Dockerfile per app under `docker/<app>/`, compose at `docker/` root | Same. |
| What goes in `scripts/` and in what form? | Bash scripts: setup, dev, test, migrate, compose | Same. |
| What goes in `.github/` given no CI? | Pull request template | Pull request template. |
| What goes in `.claude/`? | `CLAUDE.md` with folder map, commands, conventions | `CLAUDE.md` and `settings.json` with project permissions. |
| How should `docs/` be organised, and what happens to `Docs/`? | Move `Docs/` to `docs/` with `plans/`, `designs/`, `implementation/` | Same. |
| Where do environment files live and how do secrets flow? | Per-app `.env.example` plus `docker/.env.example` | Same. |
| Where does the generated Angular API client live? | Generated into `apps/web/src/app/api/` | Same. |
| Which OpenAPI client generator? | ng-openapi-gen (simplest: Angular-native, one command, no runtime dependency) | User delegated: "the simplest one". ng-openapi-gen 1.0.5. |
| Should the design's deployment diagram be revised to SQLite everywhere? | Yes | Yes; revised on 2026-09-07. |
| Where will the application be hosted? | — | Nowhere: local only, not published to the internet. |

## Phase 3 — Implementation Plan

Plan written on 2026-09-07 from the working plan's acceptance criteria, the design's flows and API contract, Phase 1's stack, and Phase 2's layout. Four plan-level choices were made by the user (three matched the recommendation); design-pattern proposals are decided by the developer while implementing.

### Plan at a Glance

- **Slices:** 35, 9 foundation and 26 feature, flat list without milestones (user choice)
- **Delivery unit:** one commit per slice on `main`, message prefixed with the slice number
- **Test timing:** test first, per slice
- **Client pairing:** API slice then client slice, consecutive
- **Progress:** 11 pending, 0 in progress, 24 done, 0 blocked (updated 2026-09-08)

### Principles

| Principle | How this plan applies it |
|---|---|
| Design patterns | 32 proposals across the slices, each with the plain alternative and one recommended; every `Decision` cell is `pending (developer)` until you record it (`S27: use IUnitOfWork`). |
| Clean Code | Every step names the class, method, or component it creates with the design's vocabulary (`AuthService.RegisterAsync`, `Vehicle.AdvanceMileage`, `VehiclesFacade`). |
| DRY | Shared pieces are introduced by the first slice that needs them and named for reuse: `ValidationRunner` (S07), `RandomTokenGenerator` (S10, reused S14), `IClock` (S11), `problem-details.ts` (S17), the login page object (S18), `Vehicle.AdvanceMileage` (S27, reused S29). |
| OOP | Entities own their invariants and rules: `User.Create`, `UserToken.IsUsable`, `Vehicle.Create/Update/AdvanceMileage`, `MaintenanceRecord.Create/Update`; services orchestrate. |
| YAGNI | Each slice lists what it leaves out and which slice picks it up; entities and tables arrive with the first slice that needs them, not in S06; no paging, roles, refresh tokens, soft delete, or CI. |
| SOLID | Project references enforce the dependency direction (S02); services depend only on the interfaces the design draws; `ICurrentUser`, `IPasswordHasher`, `ITokenIssuer`, `IEmailSender`, `IClock` are the seams; one class per responsibility in the observability and error-handling slices. |

### Source index

The working plan's acceptance criteria as cited by the slices (numbering follows the design):

| AC | Criterion |
|---|---|
| AC 1 | Sign up and log in; each user sees and manages only their own vehicles and records |
| AC 2 | Register and manage multiple vehicles |
| AC 3 | Vehicle fields: make, model, year, VIN, license plate, current mileage |
| AC 4 | Create a maintenance record for one of the user's vehicles |
| AC 5 | Free-form type/description, not a predefined list |
| AC 6 | Record fields: cost, date performed, mileage at service (mandatory); service provider, notes (optional) |
| AC 7 | View a vehicle's maintenance history |
| AC 8 | Verification email with a 30-minute link; unverified users can log in and are prompted to verify; a flag can require verification for every feature |
| AC 9 | Password reset by emailed 30-minute link |
| AC 10 | Edit and delete a maintenance record |
| AC 11 | A higher mileage at service advances the vehicle's current mileage |
| AC 12 | Costs in USD only |
| VIN rule | VIN unique per user, not across users |

### Slice Map

| # | Slice | Source | Depends on | Size | Status |
|---|---|---|---|---|---|
| S01 | Lay down the monorepo skeleton | Foundation | none | S | done |
| S02 | Create the API solution that starts and answers /health | Foundation | S01 | M | done |
| S03 | Create the Angular client that renders and runs one spec | Foundation | S01 | S | done |
| S04 | Create the Playwright suite with one smoke test | Foundation | S03 | S | done |
| S05 | Add the repository scripts for setup, dev, and test | Foundation | S02, S03, S04 | S | done |
| S06 | Wire EF Core with SQLite, start-up migration, and the database health check | Foundation | S02 | M | done |
| S07 | Establish the error model: ProblemDetails, validation errors, and status mapping | Foundation | S06 | M | done |
| S08 | Add JSON console logging, request logging, and the metrics meter | Foundation | S07 | S | done |
| S09 | Sign up creates an account (API) | AC 1 (sign up) | S07, S08 | M | done |
| S10 | Sign up issues a verification link and records emails (API) | AC 8 (verification email, 30 minutes) | S09 | M | done |
| S11 | Verify email and resend the link (API) | AC 8 | S10 | S | done |
| S12 | Log in with JWT sessions and rate limiting (API) | AC 1 (log in) | S09 | M | done |
| S13 | Protect endpoints: bearer authorization, CORS, and the verification flag | AC 1 (isolation), AC "flag can require verification" | S12 | M | done |
| S14 | Request a password reset (API) | AC 9 (reset link, 30 minutes) | S11, S12 | S | done |
| S15 | Set a new password from the reset link and purge stale tokens (API) | AC 9 | S14 | S | done |
| S16 | Generate the API client and add auth state to the Angular app | Foundation for client slices | S12, S13, S03 | M | done |
| S17 | Sign up screen (client) | AC 1, AC 8 | S16, S04 | S | done |
| S18 | Log in screen with the verification banner and resend (client) | AC 1, AC 8 ("prompted to verify") | S17 | S | done |
| S19 | Verify email page (client) | AC 8 | S18 | S | done |
| S20 | Forgot and reset password screens (client) | AC 9 | S19 | S | done |
| S21 | Register a vehicle (API) | AC 2, AC 3, AC "VIN unique per user" | S13 | M | done |
| S22 | List and view vehicles (API) | AC 1 (isolation), AC 2, AC 3 | S21 | S | done |
| S23 | Update and delete a vehicle (API) | AC 2 ("manage") | S22 | S | done |
| S24 | Vehicle list screen (client) | AC 2, AC 3 | S22, S18 | S | done |
| S25 | Create and edit vehicle form (client) | AC 2, AC 3 | S24, S23 | S | pending |
| S26 | Delete a vehicle with confirmation (client) | AC 2 | S25 | S | pending |
| S27 | Log a maintenance record and advance mileage (API) | AC 4, AC 5, AC 6, AC 11, AC 12 | S23 | M | pending |
| S28 | View maintenance history (API) | AC 7 | S27 | S | pending |
| S29 | Edit a maintenance record (API) | AC 10, AC 11 | S28 | S | pending |
| S30 | Delete a maintenance record (API) | AC 10 | S29 | S | pending |
| S31 | Maintenance history table (client) | AC 7, AC 3 | S28, S26 | S | pending |
| S32 | Add and edit maintenance record form (client) | AC 4, AC 5, AC 6, AC 10, AC 11, AC 12 | S31, S29 | S | pending |
| S33 | Delete a maintenance record (client) | AC 10 | S32, S30 | S | pending |
| S34 | Run the whole stack under docker compose | Phase 1 delivery decision (docker compose) | S33 | M | pending |
| S35 | Complete documentation and Claude configuration | Phase 2 Conventions | S34 | S | pending |

### Milestones

None by user decision; the Slice Map is the only ordering.

### Slices

#### S01 — Lay down the monorepo skeleton

- **Goal:** The repository has the Phase 2 top level, root pins, and documentation in place, and nothing else.
- **Source:** Foundation; Phase 2 Layout at a Glance, Root Files, Folder Purposes
- **Depends on:** none
- **Size:** S
- **In scope:**
  - Fixed top-level folders with one-line READMEs where empty
  - Root files: `README.md`, `global.json`, `.nvmrc`, `.editorconfig`, `.gitignore`
  - `.claude/CLAUDE.md` (folder map, commands, conventions) and `.claude/settings.json`
  - `.github/PULL_REQUEST_TEMPLATE.md`
  - Move `Docs/` to `docs/plans`, `docs/designs`, `docs/implementation` and fix links
- **Out of scope:**
  - Any application code (S02 onward)
  - `scripts/` contents (S05)
  - `docker/` contents (S34)
- **Files:**
  - `README.md` — purpose, folder map, setup pointer to `scripts/setup.sh`
  - `global.json` — SDK 8.0.424, rollForward latestPatch
  - `.nvmrc` — 24.20.0
  - `.editorconfig` — C#, TypeScript, JSON, YAML, Markdown rules
  - `.gitignore` — .NET, Node, Angular, Playwright, SQLite, `.env` files
  - `.claude/CLAUDE.md` — layout, commands, naming and dependency-direction conventions from Phase 2
  - `.claude/settings.json` — allow read-only and test commands
  - `.github/PULL_REQUEST_TEMPLATE.md` — plan, design, test evidence links
  - `docs/plans/, docs/designs/, docs/implementation/, docs/adr/README.md` — moved documents and reserved folder
  - `apps/README.md, docker/README.md, scripts/README.md` — one-line placeholders
- **Steps:**
  1. Create the folders and placeholder READMEs exactly as Phase 2 draws them.
  2. Write the root files; copy `.editorconfig` rules for both ecosystems.
  3. Move `Docs/` in two steps (`Docs` → `docs-tmp` → `docs`) into the three subfolders and update the relative links inside the plan, design, and this document.
  4. Write `.claude/CLAUDE.md` from Phase 2 Conventions and `.claude/settings.json` with read-only and test permissions.
  5. Write the pull request template.
- **Tests:**
  - none (verified by the definition of done)
- **Pattern proposals:** none needed
- **Principle checks:** DRY — the folder map lives in `CLAUDE.md` and `README.md` links to it rather than repeating it; YAGNI — no workflow, CODEOWNERS, or `packages/` folder; SOLID — not applicable.
- **Definition of done:**
  - [x] `tree -L 2` matches Phase 2 Layout at a Glance for the top level
  - [x] `git status` clean after the commit; old `Docs/` gone
  - [x] Links inside the three documents resolve
- **Status:** done

#### S02 — Create the API solution that starts and answers /health

- **Goal:** `dotnet run` in `apps/api-maintenance` starts an empty ASP.NET Core API whose `/health` returns 200, and both test projects run one passing test.
- **Source:** Foundation; Phase 1 Application framework and Testing; Phase 2 Internal Structure `apps/api-maintenance/`
- **Depends on:** S01
- **Size:** M
- **In scope:**
  - `Maintenance.sln`, `Directory.Build.props`, `Directory.Packages.props` with every Phase 1 NuGet version
  - Four `src` projects with the Phase 2 reference direction
  - `Maintenance.UnitTests` and `Maintenance.IntegrationTests` with xUnit, FluentAssertions 7, NSubstitute, `WebApplicationFactory`, coverlet
  - Minimal `Program.cs` with health checks (no database yet)
- **Out of scope:**
  - Persistence (S06)
  - Error model (S07)
  - Logging (S08)
- **Files:**
  - `apps/api-maintenance/Maintenance.sln` — solution
  - `apps/api-maintenance/Directory.Build.props` — net8.0, Nullable, ImplicitUsings, TreatWarningsAsErrors
  - `apps/api-maintenance/Directory.Packages.props` — central versions from Phase 1
  - `apps/api-maintenance/src/Maintenance.Domain/Maintenance.Domain.csproj` — no references
  - `apps/api-maintenance/src/Maintenance.Application/Maintenance.Application.csproj` — references Domain; FluentValidation
  - `apps/api-maintenance/src/Maintenance.Infrastructure/Maintenance.Infrastructure.csproj` — references Application
  - `apps/api-maintenance/src/Maintenance.Api/Maintenance.Api.csproj` — references Application and Infrastructure; Swashbuckle
  - `apps/api-maintenance/src/Maintenance.Api/Program.cs` — builder, health checks, Swagger in Development, `partial class Program` for tests
  - `apps/api-maintenance/src/Maintenance.Api/appsettings.json, appsettings.Development.json` — empty sections for later slices
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Maintenance.UnitTests.csproj` — xUnit, FluentAssertions, NSubstitute, coverlet
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Maintenance.IntegrationTests.csproj` — xUnit, FluentAssertions, Mvc.Testing, coverlet
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/ApiFactory.cs` — `WebApplicationFactory<Program>` subclass
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/HealthTests.cs` — first test
- **Steps:**
  1. Write `HealthTests.HealthEndpoint_ReturnsOk` against `ApiFactory` (fails: nothing exists).
  2. Create the solution, props files, and the six projects with `dotnet new`; set references per Phase 2 dependency direction.
  3. Write `Program.cs`: `AddHealthChecks`, `MapHealthChecks("/health")`, Swagger in Development, `public partial class Program {}`.
  4. Add one unit test `SanityTests.Xunit_Runs` so the unit project executes.
  5. Run `dotnet test` from `apps/api-maintenance`; both projects green.
- **Tests:**
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/HealthTests.cs`: GET /health returns 200
  - `apps/api-maintenance/tests/Maintenance.UnitTests/SanityTests.cs`: the unit project runs
- **Pattern proposals:** none needed
- **Principle checks:** DRY — versions live only in `Directory.Packages.props`; SOLID — project references enforce Api → Application → Domain and Infrastructure → Application → Domain; YAGNI — no controllers, no DbContext, no auth yet.
- **Definition of done:**
  - [x] `dotnet build` and `dotnet test` succeed in `apps/api-maintenance`
  - [x] GET /health returns 200 from `dotnet run`
  - [x] Swagger UI opens in Development
- **Status:** done

#### S03 — Create the Angular client that renders and runs one spec

- **Goal:** `ng serve` in `apps/web` shows a PrimeNG-styled shell page, and `ng test` runs one passing Vitest spec.
- **Source:** Foundation; Phase 1 Application framework and Testing; Phase 2 Internal Structure `apps/web/`
- **Depends on:** S01
- **Size:** S
- **In scope:**
  - `ng new` workspace with routing, standalone components, Vitest
  - PrimeNG and PrimeIcons wired with one theme
  - `proxy.conf.json` to the API, `environments/` with `apiBaseUrl`
  - `src/app/core`, `features`, `shared` folders with placeholders
- **Out of scope:**
  - Generated API client (S16)
  - Auth state and interceptor (S16)
  - Any feature screen
- **Files:**
  - `apps/web/package.json` — Angular 22.1.x, PrimeNG 22.1.0, PrimeIcons 8.0.0, Vitest
  - `apps/web/angular.json, tsconfig*.json` — CLI-generated
  - `apps/web/proxy.conf.json` — `/api` → API port
  - `apps/web/src/environments/environment.ts, environment.development.ts` — `apiBaseUrl`
  - `apps/web/src/app/app.component.ts` — shell with PrimeNG toolbar and router outlet
  - `apps/web/src/app/app.component.spec.ts` — renders the shell title
  - `apps/web/src/app/core/README.md, features/README.md, shared/README.md` — placeholders
- **Steps:**
  1. Write `app.component.spec.ts` expecting the shell title (fails: no workspace).
  2. Run `ng new web` with the Phase 1 options; install PrimeNG and PrimeIcons; configure the theme in `angular.json` styles.
  3. Add `proxy.conf.json`, environments, and the three folders.
  4. Implement the shell component until the spec passes; run `ng serve` and open it.
- **Tests:**
  - `apps/web/src/app/app.component.spec.ts`: shell renders the application title
- **Pattern proposals:** none needed
- **Principle checks:** DRY — `apiBaseUrl` is read from `environments/` only; YAGNI — no state management library, no feature folders beyond placeholders; SOLID — not applicable yet.
- **Definition of done:**
  - [x] `ng test` passes
  - [x] `ng serve` renders the shell with PrimeNG styling
  - [x] `ng build` succeeds
- **Status:** done

#### S04 — Create the Playwright suite with one smoke test

- **Goal:** `npx playwright test` in `apps/e2e` opens the running client and asserts the shell title.
- **Source:** Foundation; Phase 1 Testing (Playwright); Phase 2 Internal Structure `apps/e2e/`
- **Depends on:** S03
- **Size:** S
- **In scope:**
  - `package.json` with `@playwright/test` 1.63.0 and Chromium
  - `playwright.config.ts` reading `WEB_BASE_URL` and `API_BASE_URL` from `.env`
  - `.env.example`
  - `tests/smoke.spec.ts`
- **Out of scope:**
  - Any feature journey (S17 onward)
- **Files:**
  - `apps/e2e/package.json, package-lock.json` — Playwright
  - `apps/e2e/playwright.config.ts` — baseURL from env, HTML reporter, Chromium
  - `apps/e2e/.env.example` — `WEB_BASE_URL=http://localhost:4200`, `API_BASE_URL=http://localhost:5000`
  - `apps/e2e/tests/smoke.spec.ts` — shell title visible
- **Steps:**
  1. Write `smoke.spec.ts` (fails: no config).
  2. Initialise the package and config; document the two variables in `.env.example`.
  3. Run against `ng serve`; the smoke test passes.
- **Tests:**
  - `apps/e2e/tests/smoke.spec.ts`: shell title visible at `WEB_BASE_URL`
- **Pattern proposals:** none needed
- **Principle checks:** DRY — base URLs are read from `.env` only, never hard-coded in specs; YAGNI — no page objects until two specs share a page (S18).
- **Definition of done:**
  - [x] `npx playwright test` passes against a running client
  - [x] `.env` is git-ignored and `.env.example` documents both variables
- **Status:** done

#### S05 — Add the repository scripts for setup, dev, and test

- **Goal:** `scripts/setup.sh`, `dev.sh`, and `test.sh` run the whole repository from the root.
- **Source:** Foundation; Phase 2 `scripts/` decision and Conventions (Commands)
- **Depends on:** S02, S03, S04
- **Size:** S
- **In scope:**
  - `setup.sh`: `dotnet` on PATH check, `nvm install` from `.nvmrc`, `npm ci` in `apps/web` and `apps/e2e`
  - `dev.sh`: API (`dotnet run`) and `ng serve` together
  - `test.sh`: `dotnet test`, `ng test`, `playwright test` in sequence, failing fast
  - README section describing them
- **Out of scope:**
  - `migrate.sh` (S06)
  - `generate-api-client.sh` (S16)
  - `compose-up.sh` / `compose-down.sh` (S34)
- **Files:**
  - `scripts/setup.sh` — toolchain setup
  - `scripts/dev.sh` — run both apps
  - `scripts/test.sh` — run all suites
  - `README.md` — Getting started section
  - `scripts/README.md` — replaced by the real scripts' descriptions
- **Steps:**
  1. Write each script with `set -euo pipefail`, delegating to per-app commands from Phase 2 Conventions.
  2. Run `scripts/test.sh` on the empty apps; all three suites green.
  3. Document usage in `README.md`.
- **Tests:**
  - none (verified by the definition of done)
- **Pattern proposals:** none needed
- **Principle checks:** DRY — scripts delegate to the per-app commands rather than re-implementing them; YAGNI — only the three scripts needed today, the other three arrive with the slices that need them.
- **Definition of done:**
  - [x] `scripts/test.sh` runs all three suites and exits 0
  - [x] `scripts/dev.sh` starts both apps
  - [x] README documents the three scripts
- **Status:** done

#### S06 — Wire EF Core with SQLite, start-up migration, and the database health check

- **Goal:** The API creates its SQLite database on start-up through migrations and `/health` reports the database.
- **Source:** Foundation; design NFR Operations ("Migrations run automatically at API start-up") and Availability; Phase 1 Persistence
- **Depends on:** S02
- **Size:** M
- **In scope:**
  - `MaintenanceDbContext` with no entities yet
  - SQLite connection string from configuration, in-memory for integration tests
  - `Database.Migrate()` at start-up and the EF health check
  - `dotnet-ef` tool manifest, first (empty) migration, `scripts/migrate.sh`
- **Out of scope:**
  - Entities and tables (introduced by the slices that need them: S09, S10, S21, S27)
- **Files:**
  - `apps/api-maintenance/.config/dotnet-tools.json` — dotnet-ef 8.0.30
  - `apps/api-maintenance/src/Maintenance.Infrastructure/Persistence/MaintenanceDbContext.cs` — empty context
  - `apps/api-maintenance/src/Maintenance.Infrastructure/Persistence/Migrations/` — initial migration
  - `apps/api-maintenance/src/Maintenance.Infrastructure/DependencyInjection.cs` — `AddInfrastructure(configuration)` registering the context and health check
  - `apps/api-maintenance/src/Maintenance.Api/Program.cs` — call `AddInfrastructure`, migrate at start-up
  - `apps/api-maintenance/src/Maintenance.Api/appsettings.json` — `ConnectionStrings:Default`
  - `apps/api-maintenance/.env.example` — `ConnectionStrings__Default`
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/ApiFactory.cs` — override with an open SQLite in-memory connection
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/DatabaseTests.cs` — health reports database
  - `scripts/migrate.sh` — `dotnet ef migrations add` / `database update` wrapper
- **Steps:**
  1. Write `DatabaseTests.Health_ReportsDatabaseHealthy` and `Startup_AppliesMigrations` (checks `__EFMigrationsHistory` exists).
  2. Add the context, the `AddInfrastructure` extension, the SQLite provider, and the EF health check.
  3. Make `ApiFactory` keep one open in-memory SQLite connection for the test host's lifetime.
  4. Add the tool manifest and create the initial migration with `scripts/migrate.sh add Initial`.
  5. Call `Migrate()` before `app.Run()`; tests pass.
- **Tests:**
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/DatabaseTests.cs`: health includes the database entry as healthy; migration history table exists after start-up
- **Pattern proposals:**

  | Pattern | Where | Why | Why not | Recommended | Decision |
  |---|---|---|---|---|---|
  | Repository interfaces in Domain implemented in Infrastructure (design-fixed) | later slices | The design draws them; not a proposal | — | yes | design-fixed, implemented from S09 (2026-09-08) |
  | Plain `DbContext` injected into repositories vs an `IUnitOfWork` wrapper | `MaintenanceDbContext` | The context already is the unit of work; keeping it plain avoids an interface with one implementation | An explicit `IUnitOfWork` reads clearer in services and mocks better; revisit in S27 where the first transaction appears | yes | plain DbContext kept (recommended); revisited in S27 (2026-09-08) |

- **Principle checks:** DRY — connection handling lives only in `AddInfrastructure`; SOLID — Api depends on the extension method, never on the provider; YAGNI — no entities, no repositories yet.
- **Definition of done:**
  - [x] `dotnet run` creates `maintenance.db` and applies the migration
  - [x] GET /health lists the database as healthy
  - [x] `scripts/migrate.sh add <Name>` and `scripts/migrate.sh update` work
- **Status:** done

#### S07 — Establish the error model: ProblemDetails, validation errors, and status mapping

- **Goal:** Application exceptions map to the design's status codes with field-level details, and FluentValidation runs inside services.
- **Source:** Foundation; design API Contract error columns (400 field errors, 404, 409, 503); design NFR Availability
- **Depends on:** S06
- **Size:** M
- **In scope:**
  - `NotFoundException`, `ConflictException`, `ValidationException` (with field errors) in Application
  - Exception handler producing `ProblemDetails`, database unavailability → 503
  - FluentValidation registration and a `Validate<T>` helper used by services
  - Test-only endpoints in the integration host to exercise each mapping
- **Out of scope:**
  - Authorization errors 401/403 (S12, S13)
  - Rate-limit 429 (S12)
- **Files:**
  - `apps/api-maintenance/src/Maintenance.Application/Exceptions/NotFoundException.cs, ConflictException.cs, ValidationException.cs` — typed exceptions
  - `apps/api-maintenance/src/Maintenance.Application/Validation/ValidationRunner.cs` — runs a validator and throws `ValidationException`
  - `apps/api-maintenance/src/Maintenance.Application/DependencyInjection.cs` — `AddApplication()` registers validators from the assembly
  - `apps/api-maintenance/src/Maintenance.Api/Errors/ProblemDetailsExceptionHandler.cs` — `IExceptionHandler` mapping exceptions to status codes
  - `apps/api-maintenance/src/Maintenance.Api/Program.cs` — `AddExceptionHandler`, `AddProblemDetails`, `UseExceptionHandler`
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/ErrorMappingTests.cs` — one test per mapping via test-only endpoints
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Validation/ValidationRunnerTests.cs` — field errors collected
- **Steps:**
  1. Write `ErrorMappingTests` mapping test endpoints that throw each exception to the expected status and body shape.
  2. Write `ValidationRunnerTests` for a sample validator producing two field errors.
  3. Add the exceptions, the runner, and `AddApplication`.
  4. Implement the exception handler and register ProblemDetails; tests pass.
- **Tests:**
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/ErrorMappingTests.cs`: NotFound → 404, Conflict → 409, Validation → 400 with `errors` per field, SQLite unavailable → 503, unknown → 500 without details
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Validation/ValidationRunnerTests.cs`: aggregates field errors
- **Pattern proposals:**

  | Pattern | Where | Why | Why not | Recommended | Decision |
  |---|---|---|---|---|---|
  | Single `IExceptionHandler` with a switch on exception type | `ProblemDetailsExceptionHandler` | One place, five cases, easy to read | Grows if many exception types appear; unlikely here | yes | adopted (recommended): ProblemDetailsExceptionHandler with one switch (2026-09-08) |
  | Chain of Responsibility: one handler per exception type | `Api/Errors/` | Open for extension without editing a switch | Five tiny classes for five cases; more files than behaviour | no | declined (2026-09-08) |

- **Principle checks:** DRY — every service validates through `ValidationRunner`, never inline; SOLID — the handler is the single place that knows HTTP status codes, services know only exceptions; YAGNI — no error codes catalogue beyond `EmailNotVerified` (S13).
- **Definition of done:**
  - [x] All mapping tests pass
  - [x] Test-only endpoints exist only in the integration test host, not in `Program.cs`
- **Status:** done

#### S08 — Add JSON console logging, request logging, and the metrics meter

- **Goal:** Every request logs method, path, status, and latency as JSON with the caller's `userId` when known, and a `Maintenance` meter exists for counters.
- **Source:** Foundation; design NFR Observability; Phase 1 Observability
- **Depends on:** S07
- **Size:** S
- **In scope:**
  - `JsonConsoleFormatter` configuration
  - `RequestLoggingMiddleware` with duration and status, `userId` scope when a claim exists
  - `MaintenanceMetrics` wrapping `System.Diagnostics.Metrics.Meter` with named counters
  - Log entries never contain tokens or passwords (destructuring rule)
- **Out of scope:**
  - Counters for specific events (incremented by the slices that own them)
- **Files:**
  - `apps/api-maintenance/src/Maintenance.Api/Program.cs` — logging builder with JSON console
  - `apps/api-maintenance/src/Maintenance.Api/Observability/RequestLoggingMiddleware.cs` — one log line per request
  - `apps/api-maintenance/src/Maintenance.Api/Observability/MaintenanceMetrics.cs` — meter and counter accessors
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/RequestLoggingTests.cs` — captures a log line for a request
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Observability/MaintenanceMetricsTests.cs` — counter increments observed with `MeterListener`
- **Steps:**
  1. Write `RequestLoggingTests` using a test logger provider asserting one entry with method, path, status, and elapsed fields.
  2. Write `MaintenanceMetricsTests` observing a counter through `MeterListener`.
  3. Implement the middleware and the metrics class; register both.
  4. Tests pass; run the API and eyeball one JSON line.
- **Tests:**
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/RequestLoggingTests.cs`: a request produces one structured entry with the expected fields and no `Authorization` header value
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Observability/MaintenanceMetricsTests.cs`: counters increment
- **Pattern proposals:**

  | Pattern | Where | Why | Why not | Recommended | Decision |
  |---|---|---|---|---|---|
  | Own middleware for request logging | `RequestLoggingMiddleware` | Full control of the fields and of what is redacted | A few dozen lines to own | yes | adopted (recommended); metrics split into IMaintenanceMetrics (Application) + MaintenanceMetrics (Infrastructure) so services can count events (2026-09-08) |
  | Built-in `HttpLogging` middleware | `Program.cs` | No code | Logs headers and bodies by default; must be restricted carefully to keep tokens out; no latency field | no | declined (2026-09-08) |

- **Principle checks:** DRY — one middleware logs every request, controllers log nothing about requests; SOLID — `MaintenanceMetrics` is the only type that knows counter names; YAGNI — no exporter, no tracing.
- **Definition of done:**
  - [x] Both tests pass
  - [x] A running request prints one JSON line with method, path, status, latency
- **Status:** done

#### S09 — Sign up creates an account (API)

- **Goal:** `POST /api/v1/auth/register` stores a user with a hashed password and returns 201; duplicates return 409; bad input returns 400.
- **Source:** AC 1 (sign up); design flow "Sign up and verify email" first half; API Contract Register
- **Depends on:** S07, S08
- **Size:** M
- **In scope:**
  - `User` entity, `IUserRepository`, `USERS` table and migration with `ux_users_email`
  - `IPasswordHasher` with an implementation over `PasswordHasher<User>` from the shared framework
  - `AuthService.RegisterAsync` with `RegisterRequestValidator` (email format, password strength)
  - `AuthController.Register`
  - `sign_ups` counter
- **Out of scope:**
  - Verification token and email (S10)
  - Login (S12)
- **Files:**
  - `apps/api-maintenance/src/Maintenance.Domain/Users/User.cs` — entity with `Create(email, passwordHash)` factory, `EmailVerified` false
  - `apps/api-maintenance/src/Maintenance.Domain/Users/IUserRepository.cs` — `FindByEmailAsync`, `AddAsync`, `SaveChangesAsync`
  - `apps/api-maintenance/src/Maintenance.Application/Auth/IPasswordHasher.cs` — interface
  - `apps/api-maintenance/src/Maintenance.Application/Auth/AuthService.cs` — `RegisterAsync`
  - `apps/api-maintenance/src/Maintenance.Application/Auth/RegisterRequest.cs, RegisterRequestValidator.cs` — input and rules
  - `apps/api-maintenance/src/Maintenance.Infrastructure/Auth/IdentityPasswordHasher.cs` — adapter over `PasswordHasher<User>`
  - `apps/api-maintenance/src/Maintenance.Infrastructure/Persistence/Configurations/UserConfiguration.cs` — columns and unique index
  - `apps/api-maintenance/src/Maintenance.Infrastructure/Persistence/Repositories/UserRepository.cs` — EF implementation
  - `apps/api-maintenance/src/Maintenance.Infrastructure/Persistence/Migrations/` — `AddUsers` migration
  - `apps/api-maintenance/src/Maintenance.Api/Controllers/AuthController.cs` — `Register`
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Auth/AuthServiceRegisterTests.cs` — service rules with NSubstitute doubles
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Auth/RegisterTests.cs` — endpoint behaviour
- **Steps:**
  1. Write `RegisterTests`: 201 on new email, 409 on duplicate, 400 with field errors on bad email or weak password.
  2. Write `AuthServiceRegisterTests`: hashes the password, rejects duplicates, never stores the raw password.
  3. Add `User`, `IUserRepository`, configuration, repository, migration.
  4. Add `IPasswordHasher` and the Identity adapter; add validator, service, controller; increment `sign_ups`.
  5. Tests pass; try it in Swagger.
- **Tests:**
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Auth/RegisterTests.cs`: 201 / 409 / 400 per contract
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Auth/AuthServiceRegisterTests.cs`: hashing and duplicate rules
- **Pattern proposals:**

  | Pattern | Where | Why | Why not | Recommended | Decision |
  |---|---|---|---|---|---|
  | Adapter over `PasswordHasher<User>` behind `IPasswordHasher` | `IdentityPasswordHasher` | Keeps Application free of Identity types; swap to Argon2 later is one class | One extra class for a one-line delegation | yes | adopted (recommended): IdentityPasswordHasher (2026-09-08) |
  | Use `PasswordHasher<User>` directly in `AuthService` | `AuthService` | No adapter | Application would reference `Microsoft.Extensions.Identity.Core`; harder to mock | no | declined (2026-09-08) |
  | Static factory `User.Create` vs public constructor | `User` | Enforces invariants (lower-cased email, verified false) in one place | Slightly more ceremony than a constructor | yes | adopted (recommended) (2026-09-08) |

- **Principle checks:** DRY — email normalisation happens once in `User.Create`; SOLID — `AuthService` depends on `IUserRepository` and `IPasswordHasher` only; OOP — `User` owns its creation invariants; YAGNI — no roles, no profile fields.
- **Definition of done:**
  - [x] Both test files pass
  - [x] Migration `AddUsers` applied at start-up
  - [x] `sign_ups` counter increments
- **Status:** done

#### S10 — Sign up issues a verification link and records emails (API)

- **Goal:** Registration creates a 30-minute verification token, stores only its hash, and sends the link through `IEmailSender`, whose log-sink implementation records the message for tests.
- **Source:** AC 8 (verification email, 30 minutes); design flow "Sign up and verify email" second half; design Decision "Single shared token table"
- **Depends on:** S09
- **Size:** M
- **In scope:**
  - `UserToken` entity with `TokenPurpose`, `USER_TOKENS` table, migration, indexes
  - `IUserTokenRepository`, `TokenGenerator` (32 random bytes, URL-safe, SHA-256 hash)
  - `IEmailSender` with `LoggingEmailSender` that logs the link and keeps the last messages in memory
  - `Tokens:Lifetime` (00:30:00) and `Client:BaseUrl` configuration
  - Development-only `GET /api/v1/dev/emails` returning recorded messages (see Open Questions)
- **Out of scope:**
  - Verify endpoint (S11)
  - Resend (S11)
- **Files:**
  - `apps/api-maintenance/src/Maintenance.Domain/Users/UserToken.cs, TokenPurpose.cs` — entity and enum; `Issue(userId, purpose, hash, expiresAt)`, `IsUsable(now)`, `MarkUsed(now)`
  - `apps/api-maintenance/src/Maintenance.Domain/Users/IUserTokenRepository.cs` — `FindByHashAsync`, `AddAsync`, `SupersedeAsync(userId, purpose)`
  - `apps/api-maintenance/src/Maintenance.Application/Auth/ITokenGenerator.cs, IEmailSender.cs` — interfaces
  - `apps/api-maintenance/src/Maintenance.Application/Auth/TokenOptions.cs, ClientOptions.cs` — bound options
  - `apps/api-maintenance/src/Maintenance.Application/Auth/AuthService.cs` — `RegisterAsync` issues and emails the token
  - `apps/api-maintenance/src/Maintenance.Infrastructure/Auth/RandomTokenGenerator.cs` — BCL implementation
  - `apps/api-maintenance/src/Maintenance.Infrastructure/Email/LoggingEmailSender.cs, IRecordedEmails.cs` — log sink with in-memory recording
  - `apps/api-maintenance/src/Maintenance.Infrastructure/Persistence/Configurations/UserTokenConfiguration.cs, Repositories/UserTokenRepository.cs, Migrations/` — table, repository, `AddUserTokens` migration
  - `apps/api-maintenance/src/Maintenance.Api/Controllers/DevController.cs` — Development-only recorded emails
  - `apps/api-maintenance/.env.example` — `Tokens__Lifetime`, `Client__BaseUrl`
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Auth/AuthServiceRegisterTests.cs` — extended: token issued, email sent
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Users/UserTokenTests.cs` — usability rules
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Auth/RegisterTests.cs` — extended: a recorded email contains a link with a token
- **Steps:**
  1. Write `UserTokenTests`: usable before expiry and unused; unusable after expiry, after use, or for another purpose.
  2. Extend `AuthServiceRegisterTests`: registering issues one `EMAIL_VERIFICATION` token whose hash is stored and sends one email with the raw token in the link.
  3. Extend `RegisterTests`: after registering, `/dev/emails` holds one message with a link to `Client:BaseUrl`.
  4. Add the entity, repository, generator, options, sender, migration, and the Development-only controller.
  5. Tests pass.
- **Tests:**
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Users/UserTokenTests.cs`: expiry, single use, purpose
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Auth/AuthServiceRegisterTests.cs`: token and email on registration
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Auth/RegisterTests.cs`: recorded email carries the link
- **Pattern proposals:**

  | Pattern | Where | Why | Why not | Recommended | Decision |
  |---|---|---|---|---|---|
  | Null Object: `LoggingEmailSender` as the only `IEmailSender` | `Infrastructure/Email` | One implementation that logs and records; the interface keeps an SMTP adapter possible | Recording in memory is test-oriented behaviour inside production code; bounded to the last 50 messages | yes | adopted (recommended); records the last 50 messages; Development-only GET /api/v1/dev/emails reads them (2026-09-08) |
  | Separate `InMemoryEmailSender` registered only in tests | `IntegrationTests` | Production code stays pure | Playwright cannot reach a test-only sender; the dev endpoint would have nothing to read | no | declined (2026-09-08) |
  | Domain factory `UserToken.Issue` holding the expiry rule | `UserToken` | Expiry and purpose rules live with the data | Needs `now` passed in for testability | yes | adopted (recommended); IClock introduced here rather than S11 (2026-09-08) |

- **Principle checks:** DRY — token hashing lives only in `RandomTokenGenerator`; the same generator serves reset tokens (S14); SOLID — `AuthService` sees `ITokenGenerator` and `IEmailSender` only; OOP — `UserToken` answers `IsUsable`; YAGNI — no email templates engine, one plain-text message builder.
- **Definition of done:**
  - [x] All three test files pass
  - [x] `/dev/emails` responds only in Development
  - [x] Migration `AddUserTokens` applied
- **Status:** done

#### S11 — Verify email and resend the link (API)

- **Goal:** `POST /auth/verify-email` marks the account verified once and `POST /auth/resend-verification` issues a fresh link, superseding older ones, with the same 202 for any email.
- **Source:** AC 8; design flow "Sign up and verify email" (verify branch); API Contract Verify email, Resend verification
- **Depends on:** S10
- **Size:** S
- **In scope:**
  - `AuthService.VerifyEmailAsync(token)` and `ResendVerificationAsync(email)`
  - Two endpoints
  - `verifications` counter
- **Out of scope:**
  - Rate limiting of resend (S12 introduces the policy and applies it here)
- **Files:**
  - `apps/api-maintenance/src/Maintenance.Application/Auth/AuthService.cs` — two methods
  - `apps/api-maintenance/src/Maintenance.Api/Controllers/AuthController.cs` — `VerifyEmail`, `ResendVerification`
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Auth/AuthServiceVerifyTests.cs` — rules
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Auth/VerifyEmailTests.cs` — end-to-end through recorded emails
- **Steps:**
  1. Write `VerifyEmailTests`: register, read the link from `/dev/emails`, verify → 200; second use → 400; expired (advance a test clock) → 400; resend → 202 and a newer link, old link → 400.
  2. Write `AuthServiceVerifyTests` for purpose mismatch and supersession.
  3. Implement the two service methods and endpoints; introduce `IClock` if S10 did not.
  4. Tests pass.
- **Tests:**
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Auth/VerifyEmailTests.cs`: 200 once, 400 on reuse, expiry, and superseded links; 202 on resend for any email
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Auth/AuthServiceVerifyTests.cs`: purpose and supersession rules
- **Pattern proposals:**

  | Pattern | Where | Why | Why not | Recommended | Decision |
  |---|---|---|---|---|---|
  | `IClock` abstraction for `now` | `Application/Time/IClock.cs` | Expiry tests without waiting; one seam for every time rule | One more interface | yes | adopted (recommended); introduced in S10, TestClock in the integration factory (2026-09-08) |
  | Call `DateTime.UtcNow` directly | services | No abstraction | Expiry tests must manipulate token rows directly | no | declined (2026-09-08) |

- **Principle checks:** DRY — the same token lookup path serves verify and, later, reset; SOLID — one method per behaviour on `AuthService`; YAGNI — no resend cooldown beyond the rate limiter.
- **Definition of done:**
  - [x] Both test files pass
  - [x] `verifications` counter increments on success
- **Status:** done

#### S12 — Log in with JWT sessions and rate limiting (API)

- **Goal:** `POST /auth/login` returns a signed token with `emailVerified` for valid credentials, 401 otherwise, and 429 when rate-limited; auth endpoints share one rate-limit policy.
- **Source:** AC 1 (log in); design flow "Log in"; design Decision "Stateless token-based session authentication"; NFR Security (rate limits)
- **Depends on:** S09
- **Size:** M
- **In scope:**
  - `ITokenIssuer` with `JwtTokenIssuer` (signing key, 24 h lifetime, `sub`, `email_verified` claims)
  - `AuthService.LoginAsync` and `AuthController.Login`
  - JWT bearer authentication registered (used by S13)
  - Fixed-window rate limiter partitioned by client address and email, applied to login, resend, forgot-password
  - `logins` and `failed_logins` counters
- **Out of scope:**
  - Protecting endpoints (S13)
- **Files:**
  - `apps/api-maintenance/src/Maintenance.Application/Auth/ITokenIssuer.cs, AuthResult.cs, LoginRequest.cs, LoginRequestValidator.cs` — contracts
  - `apps/api-maintenance/src/Maintenance.Application/Auth/AuthService.cs` — `LoginAsync`
  - `apps/api-maintenance/src/Maintenance.Infrastructure/Auth/JwtTokenIssuer.cs, JwtOptions.cs` — issue and validate
  - `apps/api-maintenance/src/Maintenance.Api/Program.cs` — `AddAuthentication().AddJwtBearer`, `AddRateLimiter`, `UseRateLimiter`
  - `apps/api-maintenance/src/Maintenance.Api/RateLimiting/AuthRateLimitPolicy.cs` — partition by address and email
  - `apps/api-maintenance/src/Maintenance.Api/Controllers/AuthController.cs` — `Login`; `[EnableRateLimiting("auth")]` on login, resend, forgot-password
  - `apps/api-maintenance/.env.example` — `Jwt__SigningKey`, `Jwt__Lifetime`
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Auth/AuthServiceLoginTests.cs, JwtTokenIssuerTests.cs` — rules and token shape
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Auth/LoginTests.cs` — 200 / 401 / 429
- **Steps:**
  1. Write `LoginTests`: 200 with token and `emailVerified=false` for a fresh account, same 401 for unknown email and wrong password, 429 after N attempts.
  2. Write `JwtTokenIssuerTests`: token validates, carries `sub` and `email_verified`, expires per options.
  3. Implement issuer, options, service method, controller, rate-limit policy.
  4. Tests pass.
- **Tests:**
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Auth/LoginTests.cs`: contract statuses and identical 401 bodies
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Auth/JwtTokenIssuerTests.cs`: claims and expiry
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Auth/AuthServiceLoginTests.cs`: verify called, counters
- **Pattern proposals:**

  | Pattern | Where | Why | Why not | Recommended | Decision |
  |---|---|---|---|---|---|
  | Framework rate limiter with one named policy | `AuthRateLimitPolicy` | Built-in, declarative per endpoint | Partition key logic needs a small helper to read the email from the body | yes | adopted (recommended); partitioned by client address only, the email partition was dropped because reading the body inside the limiter is costly and local use has one address (2026-09-08) |
  | Custom middleware counting attempts in memory | `Api/RateLimiting` | Full control | Reimplements what the framework ships | no | declined (2026-09-08) |

- **Principle checks:** DRY — one policy attribute covers all three auth endpoints; SOLID — `JwtTokenIssuer` is the only type that knows signing; YAGNI — no refresh tokens, no revocation list.
- **Definition of done:**
  - [x] All three test files pass
  - [x] `Jwt__SigningKey` is read from user secrets locally, never from `appsettings.json`
- **Status:** done

#### S13 — Protect endpoints: bearer authorization, CORS, and the verification flag

- **Goal:** Endpoints outside `/auth` require a valid bearer token (401), the `RequireEmailVerification` flag returns 403 `EmailNotVerified` for unverified accounts when on, and the client origin is allowed through CORS.
- **Source:** AC 1 (isolation), AC "flag can require verification"; design Token filter description and Decision "Email verification prompts but does not block login"; Phase 1 CORS need
- **Depends on:** S12
- **Size:** M
- **In scope:**
  - Default authorization policy requiring authentication on all controllers outside `AuthController`
  - `RequireEmailVerification` option and an authorization handler that reads the user's current `EmailVerified` from `IUserRepository`
  - `ICurrentUser` exposing `UserId` from claims
  - CORS policy from `Cors:ClientOrigin`
  - Test-only protected endpoint in the integration host
- **Out of scope:**
  - Any real protected endpoint (S21 onward)
- **Files:**
  - `apps/api-maintenance/src/Maintenance.Api/Auth/CurrentUser.cs, ICurrentUser.cs (interface in Application)` — user id from claims
  - `apps/api-maintenance/src/Maintenance.Api/Auth/EmailVerifiedRequirement.cs, EmailVerifiedHandler.cs` — flag-aware requirement
  - `apps/api-maintenance/src/Maintenance.Application/Auth/AuthOptions.cs` — `RequireEmailVerification`
  - `apps/api-maintenance/src/Maintenance.Api/Program.cs` — fallback policy, CORS, `UseAuthentication`, `UseAuthorization`
  - `apps/api-maintenance/.env.example` — `Auth__RequireEmailVerification`, `Cors__ClientOrigin`
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Auth/ProtectedEndpointTests.cs` — 401, 200, 403 with flag, CORS preflight
- **Steps:**
  1. Write `ProtectedEndpointTests`: no token → 401; valid token → 200; flag on and unverified → 403 with `EmailNotVerified`; verify then retry without a new login → 200; preflight from the client origin → allowed, other origin → not.
  2. Implement `ICurrentUser`, the requirement and handler, options, fallback policy, CORS.
  3. Tests pass.
- **Tests:**
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Auth/ProtectedEndpointTests.cs`: 401 / 200 / 403 / verified-mid-session / CORS
- **Pattern proposals:**

  | Pattern | Where | Why | Why not | Recommended | Decision |
  |---|---|---|---|---|---|
  | ASP.NET authorization requirement + handler | `EmailVerifiedHandler` | Framework-native, policy composes with `[Authorize]`, testable in isolation | Handler needs a scoped repository lookup per request | yes | adopted (recommended): fallback policy with EmailVerifiedRequirement, ProblemDetails 403 with code EmailNotVerified via IAuthorizationMiddlewareResultHandler (2026-09-08) |
  | Custom middleware after authentication | `Api/Auth` | Simple to read | Bypasses the policy system; every future policy must remember the same ordering | no | declined (2026-09-08) |

- **Principle checks:** DRY — `ICurrentUser` is the only way controllers learn the caller; SOLID — the handler depends on `IUserRepository` and `IOptions<AuthOptions>`, nothing else; YAGNI — no roles or permissions.
- **Definition of done:**
  - [x] Test file passes
  - [x] Flag defaults to false in `appsettings.json` and `.env.example`
- **Status:** done

#### S14 — Request a password reset (API)

- **Goal:** `POST /auth/forgot-password` always answers 202 and, for a registered email, records a 30-minute `PASSWORD_RESET` link.
- **Source:** AC 9 (reset link, 30 minutes); design flow "Reset a forgotten password" first half
- **Depends on:** S11, S12
- **Size:** S
- **In scope:**
  - `AuthService.RequestPasswordResetAsync(email)` reusing token generation and email sending
  - Endpoint with the auth rate-limit policy
  - `reset_requests` counter
- **Out of scope:**
  - Setting the new password (S15)
- **Files:**
  - `apps/api-maintenance/src/Maintenance.Application/Auth/AuthService.cs` — `RequestPasswordResetAsync`
  - `apps/api-maintenance/src/Maintenance.Api/Controllers/AuthController.cs` — `ForgotPassword`
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Auth/AuthServiceResetTests.cs` — unknown email sends nothing, known email issues reset token
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Auth/ForgotPasswordTests.cs` — 202 either way; recorded email only for known
- **Steps:**
  1. Write both tests.
  2. Implement the method by composing `ITokenGenerator`, `IUserTokenRepository.SupersedeAsync`, and `IEmailSender.SendPasswordResetAsync`.
  3. Tests pass.
- **Tests:**
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Auth/ForgotPasswordTests.cs`: identical 202 bodies; one email for a known address
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Auth/AuthServiceResetTests.cs`: no token for unknown email
- **Pattern proposals:** none needed
- **Principle checks:** DRY — reuses the S10 token path with a different purpose; SOLID — nothing new to inject; YAGNI — no reset-attempt history.
- **Definition of done:**
  - [x] Both tests pass
  - [x] Endpoint carries the `auth` rate-limit policy
- **Status:** done

#### S15 — Set a new password from the reset link and purge stale tokens (API)

- **Goal:** `POST /auth/reset-password` replaces the hash for a usable reset token, marks it used, and the API sweeps used or expired tokens older than 30 days at start-up.
- **Source:** AC 9; design flow "Reset a forgotten password" second half; design Data NFR (token purge)
- **Depends on:** S14
- **Size:** S
- **In scope:**
  - `AuthService.ResetPasswordAsync(token, newPassword)` with password strength validation
  - Endpoint
  - `TokenPurgeOnStartup` hosted service deleting stale rows
  - `resets` counter
- **Out of scope:**
  - Logging in after reset (already S12)
- **Files:**
  - `apps/api-maintenance/src/Maintenance.Application/Auth/AuthService.cs` — `ResetPasswordAsync`
  - `apps/api-maintenance/src/Maintenance.Application/Auth/ResetPasswordRequest.cs, ResetPasswordRequestValidator.cs` — input
  - `apps/api-maintenance/src/Maintenance.Domain/Users/IUserTokenRepository.cs` — `DeleteStaleAsync(before)`
  - `apps/api-maintenance/src/Maintenance.Infrastructure/Auth/TokenPurgeOnStartup.cs` — `IHostedService` running once
  - `apps/api-maintenance/src/Maintenance.Api/Controllers/AuthController.cs` — `ResetPassword`
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Auth/ResetPasswordTests.cs` — reset then login with new password; old password fails; reused link 400
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Auth/TokenPurgeTests.cs` — deletes only stale rows
- **Steps:**
  1. Write `ResetPasswordTests` and `TokenPurgeTests`.
  2. Implement service method, validator, endpoint, repository method, hosted service.
  3. Tests pass.
- **Tests:**
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Auth/ResetPasswordTests.cs`: full reset journey through the recorded link
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Auth/TokenPurgeTests.cs`: purge boundary
- **Pattern proposals:**

  | Pattern | Where | Why | Why not | Recommended | Decision |
  |---|---|---|---|---|---|
  | `IHostedService` start-up sweep | `TokenPurgeOnStartup` | Runs once per process start, no scheduler, matches the design assumption | Long-running processes never purge again; acceptable locally | yes | adopted (recommended): TokenPurgeOnStartup with Tokens:PurgeAfter (30 days) (2026-09-08) |
  | Purge inline during `ResetPasswordAsync` | `AuthService` | No hosted service | Mixes housekeeping into a user request | no | declined (2026-09-08) |

- **Principle checks:** DRY — password validation rule shared with registration through one `PasswordRules` validator; SOLID — purge is its own class with one reason to change; YAGNI — no periodic timer.
- **Definition of done:**
  - [x] Both tests pass
  - [x] Start-up log line reports purged count
- **Status:** done

#### S16 — Generate the API client and add auth state to the Angular app

- **Goal:** `scripts/generate-api-client.sh` produces `src/app/api/` from the running API's OpenAPI document, and the client keeps the session token in `sessionStorage`, attaches it to requests, and exposes `emailVerified` as a signal.
- **Source:** Foundation for client slices; Phase 2 API-client decision; design "Log in" explanation (banner state)
- **Depends on:** S12, S13, S03
- **Size:** M
- **In scope:**
  - `ng-openapi-gen` config and script
  - `AuthState` (signals: `isAuthenticated`, `emailVerified`), `AuthInterceptor` (bearer header, 401 → logout, 403 `EmailNotVerified` → verification route), `authGuard`
  - `core/` wiring
- **Out of scope:**
  - Any screen (S17 onward)
- **Files:**
  - `apps/web/package.json` — ng-openapi-gen devDependency
  - `apps/web/ng-openapi-gen.json` — input URL, output `src/app/api`
  - `scripts/generate-api-client.sh` — fetch `swagger/v1/swagger.json` from the running API and generate
  - `apps/web/src/app/api/` — generated, committed
  - `apps/web/src/app/core/auth/auth-state.ts, auth.interceptor.ts, auth.guard.ts, token-storage.ts` — auth plumbing
  - `apps/web/src/app/app.config.ts` — provide HttpClient with the interceptor, API base path
  - `apps/web/src/app/core/auth/*.spec.ts` — unit specs
- **Steps:**
  1. Write specs: interceptor adds the header when a token exists; 401 clears state; 403 with `EmailNotVerified` navigates to `/verify`; guard redirects anonymous users to `/login`.
  2. Run the API, generate the client, commit it.
  3. Implement `TokenStorage`, `AuthState`, interceptor, guard; wire providers.
  4. Specs pass.
- **Tests:**
  - `apps/web/src/app/core/auth/auth.interceptor.spec.ts`, `auth.guard.spec.ts`, `auth-state.spec.ts`
- **Pattern proposals:**

  | Pattern | Where | Why | Why not | Recommended | Decision |
  |---|---|---|---|---|---|
  | Facade: `AuthState` as the single entry point for auth in the client | `core/auth/auth-state.ts` | Components never touch storage or the generated client directly | One more class | yes | adopted (recommended): AuthState signals over TokenStorage; interceptor and guard read it (2026-09-08) |
  | Components call the generated `AuthService` and storage directly | features | Fewer files | Session logic duplicated across screens | no | declined (2026-09-08) |

- **Principle checks:** DRY — the generated client is the only HTTP surface; SOLID — `TokenStorage` isolates `sessionStorage` so specs run without a browser; YAGNI — no refresh, no persistent login.
- **Definition of done:**
  - [x] Specs pass
  - [x] `src/app/api/` regenerates cleanly and produces no diff when the contract is unchanged
- **Status:** done

#### S17 — Sign up screen (client)

- **Goal:** A visitor registers from `/register` and sees a confirmation that a verification email was sent.
- **Source:** AC 1, AC 8; design use case "Sign up"
- **Depends on:** S16, S04
- **Size:** S
- **In scope:**
  - `features/auth/register` standalone component with a reactive form and PrimeNG inputs
  - Field errors from 400 `ProblemDetails` shown inline; 409 shown as a message
  - Route `/register`
  - Playwright journey
- **Out of scope:**
  - Login screen (S18)
- **Files:**
  - `apps/web/src/app/features/auth/register/register.component.ts, .html, .spec.ts` — form
  - `apps/web/src/app/shared/problem-details.ts` — maps `errors` to form controls (reused by every form)
  - `apps/web/src/app/app.routes.ts` — route
  - `apps/e2e/tests/auth/register.spec.ts` — journey
  - `apps/e2e/tests/support/api.ts` — helper reading `/dev/emails` via `API_BASE_URL`
- **Steps:**
  1. Write the Playwright journey: register a random email, expect the confirmation, expect `/dev/emails` to contain a link.
  2. Write component specs: submit disabled until valid; server field errors bind to controls.
  3. Implement the component, the shared ProblemDetails mapper, the route.
  4. All pass.
- **Tests:**
  - `apps/e2e/tests/auth/register.spec.ts`
  - `apps/web/src/app/features/auth/register/register.component.spec.ts`
- **Pattern proposals:**

  | Pattern | Where | Why | Why not | Recommended | Decision |
  |---|---|---|---|---|---|
  | Shared `problem-details.ts` mapper for server field errors | `shared/` | Every form binds server errors the same way | A tiny abstraction introduced at its first use because the second use (S18) is one slice away | yes | adopted (recommended): applyProblemDetails and serverError in shared/; an AuthApi facade over the generated functions was added for the auth screens (2026-09-08) |
  | Inline mapping in each component | components | No shared code | Duplicated in every form | no | declined (2026-09-08) |

- **Principle checks:** DRY — one ProblemDetails mapper; SOLID — the component knows the form, the mapper knows the error shape; YAGNI — no password strength meter beyond the server rule.
- **Definition of done:**
  - [x] Journey and specs pass
- **Status:** done

#### S18 — Log in screen with the verification banner and resend (client)

- **Goal:** A user logs in from `/login`, lands on `/vehicles` (placeholder), and sees a banner with a resend action while unverified.
- **Source:** AC 1, AC 8 ("prompted to verify"); design flow "Log in"
- **Depends on:** S17
- **Size:** S
- **In scope:**
  - `features/auth/login` component
  - `core/layout/verification-banner` component driven by `AuthState.emailVerified`, with resend
  - Route `/login`, placeholder `/vehicles` route behind `authGuard`
  - Playwright journeys; a login page object shared by later journeys
- **Out of scope:**
  - Vehicle screens (S24)
- **Files:**
  - `apps/web/src/app/features/auth/login/login.component.ts, .html, .spec.ts` — form
  - `apps/web/src/app/core/layout/verification-banner.component.ts, .spec.ts` — banner
  - `apps/web/src/app/app.routes.ts` — routes
  - `apps/e2e/tests/auth/login.spec.ts` — journeys
  - `apps/e2e/tests/support/pages/login.page.ts` — page object
- **Steps:**
  1. Write journeys: wrong password shows an error; fresh account logs in and sees the banner; resend shows a confirmation and adds an email.
  2. Write specs for the banner visibility and resend call.
  3. Implement login, banner, routes, page object.
  4. All pass.
- **Tests:**
  - `apps/e2e/tests/auth/login.spec.ts`
  - `apps/web/src/app/features/auth/login/login.component.spec.ts`, `core/layout/verification-banner.component.spec.ts`
- **Pattern proposals:** none needed
- **Principle checks:** DRY — login page object introduced now because S19 to S33 reuse it; SOLID — banner depends on `AuthState` only; YAGNI — no "remember me".
- **Definition of done:**
  - [x] Journeys and specs pass
- **Status:** done

#### S19 — Verify email page (client)

- **Goal:** Opening the emailed link at `/verify?token=…` verifies the account and updates the banner without a new login.
- **Source:** AC 8; design flow "Sign up and verify email" (verify branch)
- **Depends on:** S18
- **Size:** S
- **In scope:**
  - `features/auth/verify` component calling verify-email with the query token, success and failure states
  - `AuthState.markVerified()`
  - Playwright journey through `/dev/emails`
- **Out of scope:**
- **Files:**
  - `apps/web/src/app/features/auth/verify/verify.component.ts, .spec.ts` — page
  - `apps/web/src/app/core/auth/auth-state.ts` — `markVerified`
  - `apps/e2e/tests/auth/verify.spec.ts` — journey
- **Steps:**
  1. Write the journey: register, log in, open the link, banner disappears; open it again, see the invalid-link message.
  2. Write specs for both states.
  3. Implement; all pass.
- **Tests:**
  - `apps/e2e/tests/auth/verify.spec.ts`
  - `apps/web/src/app/features/auth/verify/verify.component.spec.ts`
- **Pattern proposals:** none needed
- **Principle checks:** DRY — reuses the `api.ts` email helper; YAGNI — no auto-login from the link.
- **Definition of done:**
  - [x] Journey and specs pass
- **Status:** done

#### S20 — Forgot and reset password screens (client)

- **Goal:** A visitor requests a reset from `/forgot-password`, opens the link at `/reset-password?token=…`, sets a new password, and logs in with it.
- **Source:** AC 9; design flow "Reset a forgotten password"
- **Depends on:** S19
- **Size:** S
- **In scope:**
  - Two components and routes
  - Playwright journey end to end
- **Out of scope:**
- **Files:**
  - `apps/web/src/app/features/auth/forgot-password/*.ts, reset-password/*.ts` — forms
  - `apps/web/src/app/app.routes.ts` — routes
  - `apps/e2e/tests/auth/reset-password.spec.ts` — journey
- **Steps:**
  1. Write the journey.
  2. Write specs (same shape as S17).
  3. Implement; all pass.
- **Tests:**
  - `apps/e2e/tests/auth/reset-password.spec.ts`
  - `apps/web/src/app/features/auth/forgot-password/*.spec.ts`, `reset-password/*.spec.ts`
- **Pattern proposals:** none needed
- **Principle checks:** DRY — shared mapper and page objects; YAGNI — no password history check.
- **Definition of done:**
  - [x] Journey and specs pass
- **Status:** done

#### S21 — Register a vehicle (API)

- **Goal:** `POST /vehicles` creates a vehicle owned by the caller with the six design fields, rejects invalid input with field errors, and rejects a duplicate VIN within the owner's vehicles with 409.
- **Source:** AC 2, AC 3, AC "VIN unique per user"; design flow "Register and manage vehicles" (create branch); API Contract Create vehicle
- **Depends on:** S13
- **Size:** M
- **In scope:**
  - `Vehicle` entity with `Create(userId, …)` and `Update(…)`
  - `IVehicleRepository` with `FindByIdAndUserIdAsync`, `FindAllByUserIdAsync`, `ExistsVinAsync(userId, vin)`, `AddAsync`, `RemoveAsync`
  - `VEHICLES` table, migration, indexes
  - `VehicleService.CreateAsync`, `VehicleInputValidator` (year range, non-negative mileage, VIN length)
  - `VehicleController.Create`
  - `vehicles_created` counter
- **Out of scope:**
  - List, get, update, delete (S22, S23)
- **Files:**
  - `apps/api-maintenance/src/Maintenance.Domain/Vehicles/Vehicle.cs, IVehicleRepository.cs` — entity and repository
  - `apps/api-maintenance/src/Maintenance.Application/Vehicles/VehicleService.cs, VehicleInput.cs, VehicleInputValidator.cs, VehicleDto.cs` — service and contracts
  - `apps/api-maintenance/src/Maintenance.Infrastructure/Persistence/Configurations/VehicleConfiguration.cs, Repositories/VehicleRepository.cs, Migrations/` — table, `AddVehicles` migration
  - `apps/api-maintenance/src/Maintenance.Api/Controllers/VehicleController.cs` — `Create`
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Vehicles/VehicleServiceCreateTests.cs, VehicleInputValidatorTests.cs` — rules
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Vehicles/CreateVehicleTests.cs` — 201 / 400 / 409 / 401
- **Steps:**
  1. Write `CreateVehicleTests` including: same VIN for a second user succeeds.
  2. Write validator and service unit tests.
  3. Add entity, repository, configuration, migration, service, validator, controller.
  4. All pass.
- **Tests:**
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Vehicles/CreateVehicleTests.cs`
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Vehicles/VehicleServiceCreateTests.cs`, `VehicleInputValidatorTests.cs`
- **Pattern proposals:**

  | Pattern | Where | Why | Why not | Recommended | Decision |
  |---|---|---|---|---|---|
  | Mapping entity → DTO with a static `VehicleDto.From(vehicle)` | `VehicleDto` | Explicit, no reflection, one place | Hand-written for each DTO | yes | adopted (recommended): VehicleDto.From (2026-09-08) |
  | Mapping library (AutoMapper or Mapster) | Application | Less boilerplate | A dependency Phase 1 did not choose; hides mapping errors until runtime | no | declined (2026-09-08) |

- **Principle checks:** DRY — VIN normalisation (upper-case, trimmed) in `Vehicle.Create` only; SOLID — `VehicleService` depends on `IVehicleRepository` and `ICurrentUser`-supplied `userId`, never on `HttpContext`; OOP — `Vehicle` guards its own invariants; YAGNI — no vehicle photos, no make/model catalogue.
- **Definition of done:**
  - [x] All tests pass
  - [x] Migration `AddVehicles` applied
- **Status:** done

#### S22 — List and view vehicles (API)

- **Goal:** `GET /vehicles` returns only the caller's vehicles and `GET /vehicles/{id}` returns 404 for any vehicle the caller does not own.
- **Source:** AC 1 (isolation), AC 2, AC 3; design flow "Register and manage vehicles" (get branch); Decision "Return 404, not 403"
- **Depends on:** S21
- **Size:** S
- **In scope:**
  - `VehicleService.ListAsync`, `GetAsync`
  - Two endpoints
- **Out of scope:**
- **Files:**
  - `apps/api-maintenance/src/Maintenance.Application/Vehicles/VehicleService.cs` — two methods
  - `apps/api-maintenance/src/Maintenance.Api/Controllers/VehicleController.cs` — `List`, `Get`
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Vehicles/ReadVehicleTests.cs` — isolation across two users
- **Steps:**
  1. Write `ReadVehicleTests`: two users, each sees only their own; other user's id → 404; unknown id → 404.
  2. Implement; pass.
- **Tests:**
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Vehicles/ReadVehicleTests.cs`
- **Pattern proposals:** none needed
- **Principle checks:** DRY — `FindByIdAndUserIdAsync` is the only single-vehicle lookup; SOLID — no new dependencies; YAGNI — no paging (design volume is tens of vehicles).
- **Definition of done:**
  - [x] Tests pass
- **Status:** done

#### S23 — Update and delete a vehicle (API)

- **Goal:** `PUT /vehicles/{id}` applies the same validation and VIN rule as creation; `DELETE /vehicles/{id}` removes the vehicle and, later, its records by cascade.
- **Source:** AC 2 ("manage"); design flow "Register and manage vehicles" (update/delete not drawn); API Contract Update vehicle, Delete vehicle
- **Depends on:** S22
- **Size:** S
- **In scope:**
  - `VehicleService.UpdateAsync`, `DeleteAsync`
  - Two endpoints
- **Out of scope:**
  - Cascade verification with records (checked again in S30)
- **Files:**
  - `apps/api-maintenance/src/Maintenance.Application/Vehicles/VehicleService.cs` — two methods
  - `apps/api-maintenance/src/Maintenance.Api/Controllers/VehicleController.cs` — `Update`, `Delete`
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Vehicles/UpdateDeleteVehicleTests.cs` — contract statuses
- **Steps:**
  1. Write tests: update returns 200 with new values, 409 on VIN collision with another own vehicle, 404 for others' vehicles; delete → 204 then 404.
  2. Implement with `Vehicle.Update`; pass.
- **Tests:**
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Vehicles/UpdateDeleteVehicleTests.cs`
- **Pattern proposals:** none needed
- **Principle checks:** DRY — `VehicleInputValidator` reused; OOP — `Vehicle.Update` keeps invariants; YAGNI — no soft delete.
- **Definition of done:**
  - [x] Tests pass
- **Status:** done

#### S24 — Vehicle list screen (client)

- **Goal:** After login the user lands on `/vehicles` showing their vehicles in a PrimeNG table with an empty state.
- **Source:** AC 2, AC 3; design use case "View and edit vehicle details"
- **Depends on:** S22, S18
- **Size:** S
- **In scope:**
  - `features/vehicles/vehicle-list` component and `VehiclesFacade` service (signals over the generated client)
  - Route replaces the S18 placeholder
  - Journey: seeded vehicle appears
- **Out of scope:**
  - Create/edit form (S25)
- **Files:**
  - `apps/web/src/app/features/vehicles/vehicles.facade.ts` — load, list signal
  - `apps/web/src/app/features/vehicles/vehicle-list/*.ts` — table
  - `apps/e2e/tests/vehicles/list.spec.ts` — journey
  - `apps/e2e/tests/support/api.ts` — helper creating a vehicle through the API
- **Steps:**
  1. Write the journey and specs.
  2. Implement facade and component; regenerate the API client after S23.
  3. All pass.
- **Tests:**
  - `apps/e2e/tests/vehicles/list.spec.ts`
  - `apps/web/src/app/features/vehicles/vehicle-list/*.spec.ts`, `vehicles.facade.spec.ts`
- **Pattern proposals:**

  | Pattern | Where | Why | Why not | Recommended | Decision |
  |---|---|---|---|---|---|
  | Facade per feature (`VehiclesFacade`) holding signals and calling the generated client | `features/vehicles` | Components stay presentational; one place for loading and error state | One class per feature | yes | adopted (recommended): VehiclesFacade with signals over the generated functions (2026-09-08) |
  | Components call the generated client directly | components | Fewer files | Loading and error handling duplicated per screen | no | declined (2026-09-08) |

- **Principle checks:** DRY — one facade per feature; SOLID — the table component only renders inputs; YAGNI — no sorting or filtering until asked.
- **Definition of done:**
  - [x] Journey and specs pass
- **Status:** done

#### S25 — Create and edit vehicle form (client)

- **Goal:** The user adds a vehicle and edits an existing one from a dialog form with inline server errors.
- **Source:** AC 2, AC 3; design use cases "Register a vehicle", "View and edit vehicle details"
- **Depends on:** S24, S23
- **Size:** S
- **In scope:**
  - `vehicle-form` dialog component reused for create and edit
  - Facade `create`, `update`
  - Journeys: create; edit; duplicate VIN shows the 409 message
- **Out of scope:**
- **Files:**
  - `apps/web/src/app/features/vehicles/vehicle-form/*.ts` — dialog
  - `apps/web/src/app/features/vehicles/vehicles.facade.ts` — two methods
  - `apps/e2e/tests/vehicles/create-edit.spec.ts` — journeys
- **Steps:**
  1. Write journeys and specs.
  2. Implement; pass.
- **Tests:**
  - `apps/e2e/tests/vehicles/create-edit.spec.ts`
  - `apps/web/src/app/features/vehicles/vehicle-form/*.spec.ts`
- **Pattern proposals:** none needed
- **Principle checks:** DRY — one form for both modes; YAGNI — no VIN decoding.
- **Definition of done:**
  - [ ] Journeys and specs pass
- **Status:** pending

#### S26 — Delete a vehicle with confirmation (client)

- **Goal:** The user deletes a vehicle after a confirmation dialog that warns about its records.
- **Source:** AC 2; design Decision "Cascade delete" consequence ("UI should confirm")
- **Depends on:** S25
- **Size:** S
- **In scope:**
  - Delete action with PrimeNG confirm dialog
  - Facade `delete`
  - Journey
- **Out of scope:**
- **Files:**
  - `apps/web/src/app/features/vehicles/vehicle-list/*.ts` — action
  - `apps/web/src/app/features/vehicles/vehicles.facade.ts` — `delete`
  - `apps/e2e/tests/vehicles/delete.spec.ts` — journey
- **Steps:**
  1. Write journey and spec.
  2. Implement; pass.
- **Tests:**
  - `apps/e2e/tests/vehicles/delete.spec.ts`
  - `apps/web/src/app/features/vehicles/vehicle-list/*.spec.ts`
- **Pattern proposals:** none needed
- **Principle checks:** YAGNI — no undo.
- **Definition of done:**
  - [ ] Journey and spec pass
- **Status:** pending

#### S27 — Log a maintenance record and advance mileage (API)

- **Goal:** `POST /vehicles/{id}/maintenance` stores a record with the design's fields, validates them, and raises the vehicle's current mileage in the same transaction when the record's mileage is higher.
- **Source:** AC 4, AC 5, AC 6, AC 11, AC 12; design flow "Record a maintenance job and advance mileage"; Decision "Advance vehicle mileage inside the maintenance record transaction"
- **Depends on:** S23
- **Size:** M
- **In scope:**
  - `MaintenanceRecord` entity, `IMaintenanceRecordRepository`, `MAINTENANCE_RECORDS` table, migration, index
  - `Vehicle.AdvanceMileage(int)` domain method
  - `MaintenanceService.CreateAsync` using an explicit transaction across both repositories
  - `MaintenanceInputValidator` (non-blank description, cost ≥ 0 with two decimals, date not in future, mileage ≥ 0)
  - `MaintenanceController.Create` returning the record and the vehicle's resulting mileage
  - `records_created`, `mileage_advances` counters
- **Out of scope:**
  - List, update, delete (S28 to S30)
- **Files:**
  - `apps/api-maintenance/src/Maintenance.Domain/Maintenance/MaintenanceRecord.cs, IMaintenanceRecordRepository.cs` — entity and repository
  - `apps/api-maintenance/src/Maintenance.Domain/Vehicles/Vehicle.cs` — `AdvanceMileage`
  - `apps/api-maintenance/src/Maintenance.Application/Maintenance/MaintenanceService.cs, MaintenanceInput.cs, MaintenanceInputValidator.cs, MaintenanceRecordDto.cs, MaintenanceWriteResult.cs` — service and contracts
  - `apps/api-maintenance/src/Maintenance.Application/Persistence/IUnitOfWork.cs` — if the transaction option below is chosen
  - `apps/api-maintenance/src/Maintenance.Infrastructure/Persistence/Configurations/MaintenanceRecordConfiguration.cs, Repositories/MaintenanceRecordRepository.cs, Migrations/` — table with decimal-as-text check constraint, `AddMaintenanceRecords` migration
  - `apps/api-maintenance/src/Maintenance.Api/Controllers/MaintenanceController.cs` — `Create`
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Vehicles/VehicleAdvanceMileageTests.cs, Maintenance/MaintenanceServiceCreateTests.cs, MaintenanceInputValidatorTests.cs` — rules
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Maintenance/CreateRecordTests.cs` — contract, mileage advance, transaction atomicity
- **Steps:**
  1. Write `VehicleAdvanceMileageTests`: higher raises, equal or lower leaves unchanged.
  2. Write `CreateRecordTests`: 201 with fields; vehicle mileage raised when higher; unchanged when lower; 400 field errors; 404 for another user's vehicle; a forced failure after the record insert leaves neither row.
  3. Write validator and service unit tests.
  4. Add entity, repository, configuration, migration (with the `cost_usd >= 0` check via cast), domain method, service with transaction, controller.
  5. All pass.
- **Tests:**
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Maintenance/CreateRecordTests.cs`
  - `apps/api-maintenance/tests/Maintenance.UnitTests/Vehicles/VehicleAdvanceMileageTests.cs`, `Maintenance/MaintenanceServiceCreateTests.cs`, `MaintenanceInputValidatorTests.cs`
- **Pattern proposals:**

  | Pattern | Where | Why | Why not | Recommended | Decision |
  |---|---|---|---|---|---|
  | Domain method `Vehicle.AdvanceMileage` (Tell, Don't Ask) | `Vehicle` | The rule lives with the data; unit-testable without EF | None of substance | yes | pending (developer) |
  | Rule inside `MaintenanceService` | service | Everything in one place | Anaemic entity; the rule is duplicated on update (S29) | no | pending (developer) |
  | Explicit `IUnitOfWork.BeginTransactionAsync` abstraction | `Application/Persistence` | Service expresses the transaction without EF types; mockable | One interface with one implementation over `DbContext.Database` | yes | pending (developer) |
  | Use `DbContext` transaction directly from the service | `MaintenanceService` | No abstraction | Application references EF Core | no | pending (developer) |

- **Principle checks:** DRY — `AdvanceMileage` is written once and reused by S29; SOLID — service orchestrates, entity decides; OOP — `MaintenanceRecord.Create` validates its own invariants; YAGNI — no attachments, no categories.
- **Definition of done:**
  - [ ] All tests pass, including atomicity
  - [ ] Migration `AddMaintenanceRecords` applied
- **Status:** pending

#### S28 — View maintenance history (API)

- **Goal:** `GET /vehicles/{id}/maintenance` returns the caller's records for that vehicle newest first, and 404 for a vehicle they do not own.
- **Source:** AC 7; design flow "View maintenance history"
- **Depends on:** S27
- **Size:** S
- **In scope:**
  - `MaintenanceService.ListByVehicleAsync`
  - Endpoint
- **Out of scope:**
- **Files:**
  - `apps/api-maintenance/src/Maintenance.Application/Maintenance/MaintenanceService.cs` — method
  - `apps/api-maintenance/src/Maintenance.Api/Controllers/MaintenanceController.cs` — `List`
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Maintenance/ListRecordsTests.cs` — order and isolation
- **Steps:**
  1. Write tests: three records return newest first; other user's vehicle → 404.
  2. Implement; pass.
- **Tests:**
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Maintenance/ListRecordsTests.cs`
- **Pattern proposals:** none needed
- **Principle checks:** DRY — ownership check reuses `FindByIdAndUserIdAsync`; YAGNI — no paging or filters.
- **Definition of done:**
  - [ ] Tests pass
- **Status:** pending

#### S29 — Edit a maintenance record (API)

- **Goal:** `PUT /vehicles/{id}/maintenance/{recordId}` updates a record with the same validation and mileage-advance rule as creation.
- **Source:** AC 10, AC 11; design flow "Edit or delete a maintenance record" (edit half)
- **Depends on:** S28
- **Size:** S
- **In scope:**
  - `MaintenanceRecord.Update(…)`
  - `MaintenanceService.UpdateAsync` reusing the transaction and `AdvanceMileage`
  - Endpoint
- **Out of scope:**
- **Files:**
  - `apps/api-maintenance/src/Maintenance.Domain/Maintenance/MaintenanceRecord.cs` — `Update`
  - `apps/api-maintenance/src/Maintenance.Application/Maintenance/MaintenanceService.cs` — `UpdateAsync`
  - `apps/api-maintenance/src/Maintenance.Api/Controllers/MaintenanceController.cs` — `Update`
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Maintenance/UpdateRecordTests.cs` — 200, mileage advance, 404 for wrong vehicle or user, 400
- **Steps:**
  1. Write tests.
  2. Implement; pass.
- **Tests:**
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Maintenance/UpdateRecordTests.cs`
- **Pattern proposals:** none needed
- **Principle checks:** DRY — the create and update paths share one private `ApplyAndAdvanceAsync`; YAGNI — no edit history.
- **Definition of done:**
  - [ ] Tests pass
- **Status:** pending

#### S30 — Delete a maintenance record (API)

- **Goal:** `DELETE /vehicles/{id}/maintenance/{recordId}` removes the record without touching the vehicle's mileage; deleting a vehicle removes its records.
- **Source:** AC 10; design flow "Edit or delete a maintenance record" (delete half); Decision "Cascade delete"
- **Depends on:** S29
- **Size:** S
- **In scope:**
  - `MaintenanceService.DeleteAsync`
  - Endpoint
  - Cascade test
- **Out of scope:**
- **Files:**
  - `apps/api-maintenance/src/Maintenance.Application/Maintenance/MaintenanceService.cs` — `DeleteAsync`
  - `apps/api-maintenance/src/Maintenance.Api/Controllers/MaintenanceController.cs` — `Delete`
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Maintenance/DeleteRecordTests.cs` — 204, mileage unchanged, cascade on vehicle delete
- **Steps:**
  1. Write tests.
  2. Implement; pass.
- **Tests:**
  - `apps/api-maintenance/tests/Maintenance.IntegrationTests/Maintenance/DeleteRecordTests.cs`
- **Pattern proposals:** none needed
- **Principle checks:** YAGNI — no recompute of mileage on delete, per the design decision.
- **Definition of done:**
  - [ ] Tests pass
- **Status:** pending

#### S31 — Maintenance history table (client)

- **Goal:** The vehicle detail page at `/vehicles/{id}` shows the vehicle's fields and its maintenance history newest first with an empty state.
- **Source:** AC 7, AC 3; design use case "View a vehicle's maintenance history"
- **Depends on:** S28, S26
- **Size:** S
- **In scope:**
  - `features/maintenance/maintenance.facade.ts`
  - `vehicle-detail` page with the PrimeNG table
  - Route and navigation from the list
  - Journey
- **Out of scope:**
  - Add/edit form (S32)
- **Files:**
  - `apps/web/src/app/features/maintenance/maintenance.facade.ts` — load by vehicle
  - `apps/web/src/app/features/vehicles/vehicle-detail/*.ts` — page
  - `apps/e2e/tests/maintenance/history.spec.ts` — journey
  - `apps/e2e/tests/support/api.ts` — helper creating records
- **Steps:**
  1. Write journey and specs.
  2. Regenerate the API client; implement; pass.
- **Tests:**
  - `apps/e2e/tests/maintenance/history.spec.ts`
  - `apps/web/src/app/features/vehicles/vehicle-detail/*.spec.ts`, `maintenance.facade.spec.ts`
- **Pattern proposals:** none needed
- **Principle checks:** DRY — same facade pattern as S24; YAGNI — no totals or charts.
- **Definition of done:**
  - [ ] Journey and specs pass
- **Status:** pending

#### S32 — Add and edit maintenance record form (client)

- **Goal:** The user logs a job and edits one from a dialog; the vehicle's displayed mileage updates when the record advances it.
- **Source:** AC 4, AC 5, AC 6, AC 10, AC 11, AC 12; design use cases "Log a maintenance job", "Edit or delete a maintenance job"
- **Depends on:** S31, S29
- **Size:** S
- **In scope:**
  - `maintenance-form` dialog (USD cost input with two decimals, date picker, mileage)
  - Facade `create`, `update` applying `vehicleCurrentMileage` from the response
  - Journeys including the mileage advance
- **Out of scope:**
- **Files:**
  - `apps/web/src/app/features/maintenance/maintenance-form/*.ts` — dialog
  - `apps/web/src/app/features/maintenance/maintenance.facade.ts` — methods
  - `apps/e2e/tests/maintenance/create-edit.spec.ts` — journeys
- **Steps:**
  1. Write journeys and specs.
  2. Implement; pass.
- **Tests:**
  - `apps/e2e/tests/maintenance/create-edit.spec.ts`
  - `apps/web/src/app/features/maintenance/maintenance-form/*.spec.ts`
- **Pattern proposals:** none needed
- **Principle checks:** DRY — one form for both modes, shared error mapper; YAGNI — no currency selector (USD only).
- **Definition of done:**
  - [ ] Journeys and specs pass
- **Status:** pending

#### S33 — Delete a maintenance record (client)

- **Goal:** The user deletes a record after confirmation and the history refreshes.
- **Source:** AC 10
- **Depends on:** S32, S30
- **Size:** S
- **In scope:**
  - Delete action with confirm dialog
  - Facade `delete`
  - Journey
- **Out of scope:**
- **Files:**
  - `apps/web/src/app/features/vehicles/vehicle-detail/*.ts` — action
  - `apps/web/src/app/features/maintenance/maintenance.facade.ts` — `delete`
  - `apps/e2e/tests/maintenance/delete.spec.ts` — journey
- **Steps:**
  1. Write journey and spec.
  2. Implement; pass.
- **Tests:**
  - `apps/e2e/tests/maintenance/delete.spec.ts`
  - `apps/web/src/app/features/vehicles/vehicle-detail/*.spec.ts`
- **Pattern proposals:** none needed
- **Principle checks:** YAGNI — no undo.
- **Definition of done:**
  - [ ] Journey and spec pass
- **Status:** pending

#### S34 — Run the whole stack under docker compose

- **Goal:** `scripts/compose-up.sh` builds and starts the API and the Nginx-served client with a persistent SQLite volume, and the Playwright suite passes against it.
- **Source:** Phase 1 delivery decision (docker compose); Phase 2 `docker/` decision; design Deployment Diagram (local)
- **Depends on:** S33
- **Size:** M
- **In scope:**
  - `docker/api-maintenance/Dockerfile` (sdk:8.0 build, aspnet:8.0 runtime)
  - `docker/web/Dockerfile` (node:24-alpine build, nginx:1.30-alpine serve) and `nginx.conf` with SPA fallback and CSP
  - `docker/docker-compose.yml`, `docker/.env.example`
  - `scripts/compose-up.sh`, `compose-down.sh`
- **Out of scope:**
- **Files:**
  - `docker/api-maintenance/Dockerfile` — multi-stage
  - `docker/web/Dockerfile, docker/web/nginx.conf` — build and serve
  - `docker/docker-compose.yml` — two services, volume, env
  - `docker/.env.example` — ports, signing key, client origin
  - `scripts/compose-up.sh, scripts/compose-down.sh` — wrappers
  - `README.md` — compose section
- **Steps:**
  1. Write the compose files and Dockerfiles with contexts pointing at `apps/<app>` per Phase 2 Risks.
  2. Bring the stack up; curl `/health`; open the client.
  3. Run `npx playwright test` with `WEB_BASE_URL` and `API_BASE_URL` pointing at the containers; pass.
- **Tests:**
  - `apps/e2e` full suite against the compose stack
- **Pattern proposals:** none needed
- **Principle checks:** DRY — the same `.env.example` variable names as the apps; YAGNI — no CI, no registry push.
- **Definition of done:**
  - [ ] `docker compose up --build` succeeds
  - [ ] `/health` healthy from the container
  - [ ] Playwright suite passes against compose
  - [ ] SQLite data survives `compose down` and `up`
- **Status:** pending

#### S35 — Complete documentation and Claude configuration

- **Goal:** `README.md` and `.claude/CLAUDE.md` describe the finished repository: setup, scripts, structure, flag, and how to record slice progress in this document.
- **Source:** Phase 2 Conventions; plan Phase 2 Likely Affected Areas (README)
- **Depends on:** S34
- **Size:** S
- **In scope:**
  - README: overview, setup, run, test, compose, configuration table including `Auth__RequireEmailVerification`
  - CLAUDE.md: final folder map and commands
  - `docs/adr/` first record if any pattern decision deserves one
- **Out of scope:**
- **Files:**
  - `README.md` — final
  - `.claude/CLAUDE.md` — final
  - `docs/adr/0001-*.md` — optional
- **Steps:**
  1. Rewrite both files against the actual repository.
  2. Walk the setup from a clean clone following only the README.
- **Tests:**
  - none (verified by the definition of done)
- **Pattern proposals:** none needed
- **Principle checks:** DRY — README links to this document for decisions instead of repeating them; YAGNI — no wiki.
- **Definition of done:**
  - [ ] A clean clone reaches a passing `scripts/test.sh` by following the README alone
- **Status:** pending

### Pattern Proposals Register

| Slice | Pattern | Where | Recommended | Decision |
|---|---|---|---|---|
| S06 | Repository interfaces in Domain implemented in Infrastructure (design-fixed) | later slices | yes | design-fixed, implemented from S09 (2026-09-08) |
| S06 | Plain `DbContext` injected into repositories vs an `IUnitOfWork` wrapper | `MaintenanceDbContext` | yes | plain DbContext kept (recommended); revisited in S27 (2026-09-08) |
| S07 | Single `IExceptionHandler` with a switch on exception type | `ProblemDetailsExceptionHandler` | yes | adopted (recommended): ProblemDetailsExceptionHandler with one switch (2026-09-08) |
| S07 | Chain of Responsibility: one handler per exception type | `Api/Errors/` | no | declined (2026-09-08) |
| S08 | Own middleware for request logging | `RequestLoggingMiddleware` | yes | adopted (recommended); metrics split into IMaintenanceMetrics (Application) + MaintenanceMetrics (Infrastructure) so services can count events (2026-09-08) |
| S08 | Built-in `HttpLogging` middleware | `Program.cs` | no | declined (2026-09-08) |
| S09 | Adapter over `PasswordHasher<User>` behind `IPasswordHasher` | `IdentityPasswordHasher` | yes | adopted (recommended): IdentityPasswordHasher (2026-09-08) |
| S09 | Use `PasswordHasher<User>` directly in `AuthService` | `AuthService` | no | declined (2026-09-08) |
| S09 | Static factory `User.Create` vs public constructor | `User` | yes | adopted (recommended) (2026-09-08) |
| S10 | Null Object: `LoggingEmailSender` as the only `IEmailSender` | `Infrastructure/Email` | yes | adopted (recommended); records the last 50 messages; Development-only GET /api/v1/dev/emails reads them (2026-09-08) |
| S10 | Separate `InMemoryEmailSender` registered only in tests | `IntegrationTests` | no | declined (2026-09-08) |
| S10 | Domain factory `UserToken.Issue` holding the expiry rule | `UserToken` | yes | adopted (recommended); IClock introduced here rather than S11 (2026-09-08) |
| S11 | `IClock` abstraction for `now` | `Application/Time/IClock.cs` | yes | adopted (recommended); introduced in S10, TestClock in the integration factory (2026-09-08) |
| S11 | Call `DateTime.UtcNow` directly | services | no | declined (2026-09-08) |
| S12 | Framework rate limiter with one named policy | `AuthRateLimitPolicy` | yes | adopted (recommended); partitioned by client address only, the email partition was dropped because reading the body inside the limiter is costly and local use has one address (2026-09-08) |
| S12 | Custom middleware counting attempts in memory | `Api/RateLimiting` | no | declined (2026-09-08) |
| S13 | ASP.NET authorization requirement + handler | `EmailVerifiedHandler` | yes | adopted (recommended): fallback policy with EmailVerifiedRequirement, ProblemDetails 403 with code EmailNotVerified via IAuthorizationMiddlewareResultHandler (2026-09-08) |
| S13 | Custom middleware after authentication | `Api/Auth` | no | declined (2026-09-08) |
| S15 | `IHostedService` start-up sweep | `TokenPurgeOnStartup` | yes | adopted (recommended): TokenPurgeOnStartup with Tokens:PurgeAfter (30 days) (2026-09-08) |
| S15 | Purge inline during `ResetPasswordAsync` | `AuthService` | no | declined (2026-09-08) |
| S16 | Facade: `AuthState` as the single entry point for auth in the client | `core/auth/auth-state.ts` | yes | adopted (recommended): AuthState signals over TokenStorage; interceptor and guard read it (2026-09-08) |
| S16 | Components call the generated `AuthService` and storage directly | features | no | declined (2026-09-08) |
| S17 | Shared `problem-details.ts` mapper for server field errors | `shared/` | yes | adopted (recommended): applyProblemDetails and serverError in shared/; an AuthApi facade over the generated functions was added for the auth screens (2026-09-08) |
| S17 | Inline mapping in each component | components | no | declined (2026-09-08) |
| S21 | Mapping entity → DTO with a static `VehicleDto.From(vehicle)` | `VehicleDto` | yes | adopted (recommended): VehicleDto.From (2026-09-08) |
| S21 | Mapping library (AutoMapper or Mapster) | Application | no | declined (2026-09-08) |
| S24 | Facade per feature (`VehiclesFacade`) holding signals and calling the generated client | `features/vehicles` | yes | adopted (recommended): VehiclesFacade with signals over the generated functions (2026-09-08) |
| S24 | Components call the generated client directly | components | no | declined (2026-09-08) |
| S27 | Domain method `Vehicle.AdvanceMileage` (Tell, Don't Ask) | `Vehicle` | yes | pending (developer) |
| S27 | Rule inside `MaintenanceService` | service | no | pending (developer) |
| S27 | Explicit `IUnitOfWork.BeginTransactionAsync` abstraction | `Application/Persistence` | yes | pending (developer) |
| S27 | Use `DbContext` transaction directly from the service | `MaintenanceService` | no | pending (developer) |

### Conventions

- **Delivery:** one commit per slice on `main`, message `S07: establish the error model`; a slice is started only when its dependencies are `done`; work on one slice at a time.
- **Verification:** `scripts/test.sh` before every slice commit; for API slices additionally `dotnet test` in `apps/api-maintenance`; for client slices `ng test` in `apps/web` and `npx playwright test` in `apps/e2e` against `scripts/dev.sh`; S34 verifies against compose.
- **Recording decisions:** reply `start S07`, `mark S07 done`, `S07 blocked: <reason>`, or `S07: use <pattern>` / `S07: no pattern`; the document is updated in place and the counts in Plan at a Glance refreshed.
- **Regenerating the API client:** run `scripts/generate-api-client.sh` and commit the diff in the same slice as the API change that caused it, or in the first client slice that consumes it.

### Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Playwright cannot read emailed links without the Development-only `/dev/emails` endpoint (S10) | Auth journeys S17 to S20 cannot run | The endpoint is compiled in but mapped only when `IsDevelopment()`; integration tests assert it is absent in Production; see Open Questions |
| S07 and S13 verify through test-only endpoints | Test host diverges from `Program.cs` | Test endpoints are mapped in `ApiFactory` only; a test asserts they return 404 from the real host |
| SQLite `decimal` stored as TEXT (Phase 1 risk) surfaces in S27 | Check constraint or ordering wrong | S27 tests cover the constraint and a LINQ comparison |
| Transaction test in S27 needs a forced failure | Flaky or contrived test | Use a repository double that throws after the insert in the unit test, and an integration test on the resulting rows |
| 11 M-sized slices (S02, S06, S07, S09, S10, S12, S13, S16, S21, S27, S34) | Longer sittings | Each M slice's steps are ordered so a partial slice still compiles; commit only when done |
| Regenerated client drift between S23 and S24, S30 and S31 | Client compiles against a stale contract | Regeneration is a listed step in S24 and S31 |
| No CI (Phase 1) | Regressions only caught locally | `scripts/test.sh` is in the definition of done of every slice |

### Assumptions and Open Questions

#### Assumptions

- `IClock` is introduced in S11 unless S10's tests already need it; the plan lists it once.
- The Development-only `/api/v1/dev/emails` endpoint exists solely so tests and Playwright can read emailed links; it is not part of the design's API contract and returns 404 outside Development.
- The `LoggingEmailSender` keeps the last 50 messages in memory; nothing is written to disk.
- Session token lifetime stays 24 hours as the design assumes; the JWT signing key comes from user secrets locally and from `docker/.env` under compose.
- Playwright runs against `scripts/dev.sh` for S17 to S33 and against compose only in S34.

#### Open Questions

- None. The Development-only `/api/v1/dev/emails` endpoint was implemented in S10 as planned (compiled in, unmapped outside Development, covered by a test); the user approved plan execution without objecting to it.

#### Questions Asked & Answers

| Question | Recommended | Answer |
|---|---|---|
| What is the delivery unit for a slice? | One commit per slice on main | One commit per slice on main. |
| When are a slice's tests written? | Test first, per slice | Test first, per slice. |
| How are the Angular screens paired with their API behaviour? | API slice then client slice, consecutive | API slice then client slice, consecutive. |
| How should milestones be named? | By what works | No milestones. |
