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

Tooling: .NET SDK 8.0 (pinned in `global.json`), Node.js 24 via nvm (pinned in `.nvmrc`), Docker with the Compose plugin for the container stack.

The setup, run, and test scripts arrive in `scripts/` as the implementation progresses; see the implementation plan for the current state.

## Documentation

- [Working plan](docs/plans/Vehicle%20Maintenance%20Tracker.md): refined story, clarified requirements, code context.
- [Technical design](docs/designs/Vehicle%20Maintenance%20Tracker.md): class, database, sequence, state, component, use case, and deployment diagrams; API contract; non-functional requirements; design decisions. A presentation page sits beside it as HTML.
- [Implementation design](docs/implementation/Vehicle%20Maintenance%20Tracker.md): technology stack (Phase 1), monorepo layout (Phase 2), and the slice-by-slice implementation plan with tracked progress (Phase 3).
