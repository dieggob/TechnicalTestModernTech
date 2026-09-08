# Vehicle Maintenance Tracker

## Status

Current phase: Phase 2 completed; Phase 1a clarified a second time on 2026-09-07 and a technical design exists at `Docs/designs/Vehicle Maintenance Tracker.md`. Business definition complete (fourth clarification round on 2026-09-07). Technical stack and monorepo layout are decided in `Docs/stacks/Vehicle Maintenance Tracker.md`. Phase 3 is superseded by the implementation plan in `Docs/stacks/Vehicle Maintenance Tracker.md`; implementation may start from slice S01.

## Phase 1 — Refined Story

### Original Title

Vehicle Maintenance Tracker

### Refined Title

Vehicle Maintenance Tracker

### Refined Issue

Vehicle owners currently have no dedicated way to track the maintenance history of their car, including the details and cost of each maintenance activity performed.

### Refined Solution

Build an application that lets a user record and track maintenance performed on their vehicle. For each maintenance entry, the user can log relevant details, including cost. The application must allow the user to record any type of maintenance or job performed on the vehicle — not limited to a fixed, predefined list — as well as other information relevant to the vehicle beyond maintenance records.

### Refined Acceptance Criteria

- The user can create a maintenance record associated with a vehicle.
- The user can specify a type/description of the maintenance or job performed, and this is not restricted to a predefined set of categories.
- The user can record the cost of a maintenance/job entry.
- The user can view the history of maintenance records logged for a vehicle.
- The user can record general vehicle information (e.g., identifying details about the vehicle) separate from individual maintenance entries.

### Notes

None provided.

### Assumptions

- The application tracks at least one vehicle per user; the data model should not preclude supporting multiple vehicles later.
- "Log details regarding the maintenance" implies capturing more than just cost — at minimum a description and date are also needed, since the story asks to record "any kind of maintenance or job" with associated details.
- No specific technology stack, platform (web/desktop/mobile), or persistence mechanism is mandated by the story text.

### Open Questions

- Should the application support tracking multiple vehicles per user, or is a single vehicle in scope for this iteration?
- Should the application support multiple users with authentication, or is it single-user (no login/accounts)?
- Beyond cost, what specific fields should a maintenance record capture (e.g., date performed, mileage at time of service, service provider/shop, notes)?
- Beyond maintenance history, what specific vehicle-level details should be tracked (e.g., make, model, year, VIN, license plate, current mileage)?
- Is there a required or preferred technology stack/platform for this implementation, or is that left to the implementer's discretion?

## Phase 1a — Clarified Story

### Questions Asked & Answers

| Question | Answer |
| --- | --- |
| Should the application support tracking multiple vehicles per user, or is a single vehicle in scope for this iteration? | Multiple vehicles per user must be supported. |
| Should the application support multiple users with authentication, or is it single-user (no login/accounts)? | Multi-user with authentication is required; each user's data must be private to them. |
| Beyond cost, what specific fields should a maintenance record capture (e.g., date performed, mileage at time of service, service provider/shop, notes)? | All of the suggested fields: date performed, mileage at time of service, service provider, and free-form notes — in addition to cost. |
| Is there a required or preferred technology stack/platform for this implementation, or is that left to the implementer's discretion? | Deferred by the user — keep this phase focused on the business/functional definition; the technology stack will be addressed later (Phase 3), not as a business requirement. |
| What specific vehicle-level details should be tracked for each registered vehicle? | Include make, model, year, VIN, license plate, and current mileage. |
| Confirmed: Phase 1/1a stays scoped to business definition ("what the application will do"), not implementation/technology decisions. | Confirmed — no technology stack discussion in this phase; it is deferred to Phase 3. |

### Questions Asked & Answers — second round (2026-09-07, raised by the technical design)

| Question | Answer |
| --- | --- |
| Should a user be able to edit or delete a maintenance record after logging it? | Yes, it should. |
| Should logging a maintenance record with a higher mileage automatically update the vehicle's current mileage? | Yes, it should. |
| Are service provider and notes optional, while cost, date performed, and mileage at service are mandatory? | Yes. |
| Is a single implicit currency acceptable for cost, or must the currency be recorded? | USD only. No other currency is allowed. |
| Are password reset and email verification needed in this iteration? | Yes, both are required. |
| Is VIN uniqueness per user acceptable, or must a VIN be unique across all users? | Per user. Two accounts may register the same VIN; no user is blocked by another user's entry. |
| Must login be blocked until the email is verified? | No. An unverified user can log in; the application prompts them to verify. |
| Are token lifetimes of 24 hours (verification) and 1 hour (reset) acceptable? | No. Both links expire after 30 minutes. |
| Should any feature be unavailable until the email is verified? | No. All features are available without verification, with a flag to enable or disable that behaviour as wanted. |

### Refined Title

Vehicle Maintenance Tracker

### Refined Issue

Vehicle owners currently have no dedicated way to track the maintenance history of their car(s), including the details and cost of each maintenance activity performed. Since the application serves multiple users, each user's vehicles and maintenance data must remain private to them.

### Refined Solution

Build a multi-user application, gated by authentication, where each user can register one or more vehicles and record maintenance performed on them. Sign-up requires verifying the email address, and a user who forgets their password can reset it through an emailed link. Each vehicle carries identifying details: make, model, year, VIN, license plate, and current mileage. For each maintenance entry, the user can log relevant details — cost in US dollars, date performed, mileage at time of service, service provider, and free-form notes — and can record any type of maintenance or job performed, not limited to a fixed, predefined list. Cost, date performed, and mileage at service are mandatory; service provider and notes are optional. Maintenance entries can be edited and deleted after logging. When an entry's mileage is higher than the vehicle's current mileage, the vehicle's current mileage is updated to match.

### Refined Acceptance Criteria

- A user can sign up and log in; each user can only see and manage their own vehicles and maintenance records.
- A user can register and manage multiple vehicles.
- A user can record and view, for each vehicle: make, model, year, VIN, license plate, and current mileage.
- A user can create a maintenance record associated with one of their vehicles.
- A user can specify a type/description of the maintenance or job performed, not restricted to a predefined set of categories.
- A maintenance record captures cost (mandatory), date performed (mandatory), mileage at time of service (mandatory), service provider (optional), and free-form notes (optional).
- A user can view the history of maintenance records logged for a given vehicle.
- After signing up, a user receives a verification email whose link is valid for 30 minutes; an unverified user can still log in and is prompted to verify.
- All features are available without email verification by default; a configuration flag can require verification for every feature, and it can be turned on or off without a code change.
- A password-reset link is valid for 30 minutes.
- A user who has forgotten their password can request a reset email and set a new password through the emailed link.
- A user can edit and delete a maintenance record on one of their vehicles.
- When a maintenance record is created or edited with a mileage at service higher than the vehicle's current mileage, the vehicle's current mileage is updated to that value.
- All costs are recorded in US dollars; no other currency is accepted.
- A VIN is unique among a single user's vehicles; the same VIN may exist in different users' accounts.

### Remaining Open Questions (business)

None.

## Phase 2 — Code Context

### Codebase Root

`/home/diego/Source/Repos/TechnicalTestModernTech`

No code directory was given explicitly. This repository was selected because it is the only repository under `/home/diego/Source/Repos` that is not clearly unrelated (see Open Technical Questions), and it was created one minute before this plan document. Its Git remote is `git@github.com:dieggob/TechnicalTestModernTech.git`, branch `main`, single commit `adddad9 Initial commit`.

### Search Terms Used

- `vehicle`
- `maintenance`
- `mileage`
- `odometer`
- `VIN`
- `license plate`
- `service provider`

Searched recursively across all of `/home/diego/Source/Repos` (excluding `.git` and build output). The only hits were incidental uses of the words "maintenance" and "Vehicle" in skill documentation and a Java study guide — none are application code.

### Relevant Files Reviewed

| File | Why it matters |
| --- | --- |
| `TechnicalTestModernTech/README.md` | The only file in the target repository. Contains just the heading `# TechnicalTestModernTech`; no description, setup steps, or stack hints. |
| `TechnicalTestModernTech/.git` (log, branches, remote) | Confirms an empty greenfield repo: one "Initial commit", `main` tracking `origin/main`, clean working tree. |
| `Helicopx/README.md`, `Helicopx/pom.xml` | Sibling repository, inspected to rule it out. It is a Java 17 / Maven Swing arcade game with JUnit 5 tests — unrelated to this story, but shows the author's existing Java/Maven conventions (Maven standard layout, `com.dieggob.*` package root, surefire + JUnit Jupiter 5.10). |
| `GenaiSkills/*/SKILL.md` | Sibling repository of Claude skill definitions. Not application code; ruled out. |

### Current Behavior Observed in Code

There is no application code. The target repository contains a single one-line README and no source files, project files, configuration, migrations, or tests. Nothing currently implements any part of the vehicle, maintenance-record, or authentication requirements. All functionality in the Phase 1a story must be built from scratch.

### Likely Affected Areas

Because the repository is empty, every area below must be created rather than modified:

- Project/solution scaffolding and build configuration at the repository root.
- Authentication: user registration, email verification, login, password reset, and per-user data isolation.
- Outbound email for verification and password-reset links, behind an interface with a local console or mailbox sink.
- Domain model and persistence for `User` (with verified flag), `UserToken` (verification and reset tokens), `Vehicle` (make, model, year, VIN, license plate, current mileage), and `MaintenanceRecord` (type/description, cost in USD, date performed, mileage at service, service provider, notes).
- Backend endpoints/services for vehicle CRUD and maintenance-record create/list/update/delete per vehicle, scoped to the authenticated user, including the rule that a higher mileage at service advances the vehicle's current mileage.
- Database schema/migrations for the entities above.
- A user interface (web/desktop/mobile — not yet decided) for sign-up, email verification, login, password reset, vehicle management, and maintenance history.
- Automated tests for the above.
- `README.md` (currently a single heading) — setup and usage instructions.

### Existing Patterns to Follow

- None exist in the target repository — there is no code to derive patterns from.
- The only observable convention comes from the author's sibling repo `Helicopx`: Java 17, Maven standard directory layout, `com.dieggob` package root, JUnit 5 tests under `src/test/java`. This is a hint about the author's familiarity, not a constraint; the stack is still an open Phase 3 decision.

### Tests Found

- None. The target repository has no test files or test configuration.

### Risks and Dependencies

- **Stack undecided.** Phase 1a explicitly deferred the technology stack to Phase 3. The plan cannot be grounded in existing code, so Phase 3 must propose the stack and structure and mark them as assumptions.
- **Local toolchain (observed on this machine, 2026-09-07):** Java 17.0.20 and Maven 3.9.12 are on `PATH`; a user-local .NET 8 SDK (8.0.424) exists at `/home/diego/.dotnet` but `dotnet` is **not** on `PATH`; Node.js/npm, Docker, and PostgreSQL client are **not** installed; Python 3.14 is available. Whatever stack Phase 3 picks should either match these or include install steps.
- **Authentication and per-user isolation** is a cross-cutting requirement that must be designed in from the first data-access layer, since retrofitting ownership checks later is error-prone.
- **Multi-vehicle per user** and **free-form maintenance type** must be reflected in the schema (no fixed category enum; vehicle has a user foreign key; record has a vehicle foreign key).
- **Empty README** means no deployment/run conventions to inherit; documentation must be written fresh.

### Open Technical Questions

- Is `/home/diego/Source/Repos/TechnicalTestModernTech` the intended repository for this application? It was inferred, not stated. If a different location is intended, Phase 3's file paths must change.
- Which stack should Phase 3 assume: Java 17 + Maven (matches installed tools and the author's sibling project), .NET 8 (SDK present but not on `PATH`), or something else requiring installation (e.g., Node.js)?
- Which persistence mechanism is acceptable for this iteration (embedded database such as SQLite/H2 vs. a server such as PostgreSQL, which is not installed locally and Docker is unavailable)?
- What form should the user interface take (server-rendered web, SPA, REST API only, desktop)? Phase 1a does not constrain this.

## Phase 3 — Working Plan

Superseded on 2026-09-07 by the implementation plan in `Docs/stacks/Vehicle Maintenance Tracker.md`, section "Phase 3 — Implementation Plan" (produced by run-implementation-design). That plan cuts the work into 35 vertical slices with tests, files, pattern proposals, and tracked status; later phases of this document that refer to "the Phase 3 plan" mean that plan.

## Phase 4 — Implementation Verification

Pending. Run only on explicit request, after the code described in the Phase 3 plan has been implemented.

## Phase 5 — Production Readiness

Pending. Run only on explicit request. See `references/production-readiness.md`.

## Phase 6 — Production Readiness Check

Pending. Run only on explicit request. See `references/production-readiness.md`.

## Phase 7 — Functional Test Cases

Pending. Run only on explicit request. See `references/production-readiness.md`.

## Phase 8 — README and Diagrams

Pending. Run only on explicit request. See `references/production-readiness.md`.
