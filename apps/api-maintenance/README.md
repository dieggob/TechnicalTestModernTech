# api-maintenance

ASP.NET Core 8 Web API for accounts, vehicles, and maintenance records, in Clean Architecture with EF Core on SQLite.

| Project | Holds |
|---|---|
| `src/Maintenance.Domain` | Entities (`User`, `UserToken`, `Vehicle`, `MaintenanceRecord`) with their rules, and the repository interfaces |
| `src/Maintenance.Application` | Services (`AuthService`, `VehicleService`, `MaintenanceService`), FluentValidation validators, DTOs, options, the application exceptions, and the `IClock`, `IEmailSender`, `IUnitOfWork`, and metrics contracts |
| `src/Maintenance.Infrastructure` | `MaintenanceDbContext`, entity configurations, migrations, repositories, the log-sink email sender, the token purge on start-up |
| `src/Maintenance.Api` | Controllers under `/api/v1`, JWT bearer authentication, the email-verification policy, the auth rate limit, ProblemDetails error mapping, request logging, Swagger in Development |
| `tests/Maintenance.UnitTests` | Domain rules, services, and validators with substitutes |
| `tests/Maintenance.IntegrationTests` | The real pipeline through `WebApplicationFactory` on an in-memory SQLite connection, with a controllable clock |

Run `dotnet test` here, or `scripts/dev.sh` from the repository root to start it on http://localhost:5000 (Swagger at `/swagger`, health at `/health`). Configuration variables are documented in `.env.example`; the signing key lives in `dotnet user-secrets`. Migrations are managed with `scripts/migrate.sh` and applied at start-up.
