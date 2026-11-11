# CLAUDE.md: Patient Management System

This file is the working contract for Claude Code in this repository. Read it before any change. It is grounded in:
- `BRD/Doc_BRD.md` (business requirements)
- `documents/brainstorm-review.md` (architecture decisions and the Decisions Log, Q1–Q22)
- `documents/implementation-plan.md` (phases, sprints, milestones)
- `documents/worktree-plan.md` (branching and worktree rules)

Where this file and a source document disagree, this file wins for coding and workflow rules. Requirements come from the BRD and the Decisions Log. Raise any conflict with the user before coding.

---

## 1. Project Summary

A browser-based Patient Management System for one general physician in one clinic. Scope: patients, 15-minute appointments, consultations (vitals, complaints, diagnosis, medications), printable prescriptions with print-time snapshots, visit history with search, CSV and PDF export, and JWT-based login for a single doctor account.

Out of scope for this release: multi-user roles, multi-clinic or tenant columns, billing, integrations, offline mode, mobile app, reminders, analytics. Do not build these without a scope change in the plan.

### Technology Stack (fixed)

| Area | Technology |
|------|-----------|
| Frontend | Angular 19 (standalone components), Angular Material, RxJS, Reactive Forms, signals for feature state |
| Backend | ASP.NET Core Web API on .NET 10 |
| Data access | Dapper (parameterized SQL), no Entity Framework Core |
| Validation | FluentValidation (server); reactive form validators mirror the rules (client) |
| Auth | JWT access tokens (RS256), refresh token rotation, ASP.NET Core authentication and policy-based authorization |
| Database | SQL Server. LocalDB `(LocalDb)\MSSQLLocalDB` with Windows authentication in development; SQL Server Standard or Azure SQL in production |
| Architecture | Clean Architecture (API, Application, Domain, Infrastructure), Repository pattern, Unit of Work, CQRS-lite folders, dependency injection, SOLID |

Do not introduce a new framework, ORM, state library, or UI library without a recorded decision in `documents/`.

---

## 2. Non-Negotiable Rules

1. **Synthetic data only in development.** Never create, copy, or commit real patient data. Seed data comes from the seeding tool.
2. **No secrets in the repository.** No keys, passwords, connection strings with credentials, or certificates. Use user secrets (development) or the production secret store.
3. **Never work on protected branches.** `main`, `develop`, and `release/*` are read-only for development (see section 7).
4. **Never run destructive git operations without explicit approval** (`reset --hard`, `clean -fd`, `push --force`, `branch -D`, `worktree remove --force`, `checkout --` on files with changes). See section 7.4.
5. **Do not push, merge, or open pull requests unless the user asks.** Do not commit unless the user asks or the active task explicitly includes committing.
6. **Do not weaken security controls** to make tests or builds pass.
7. **Do not log patient-identifying data** (names, phone numbers, DOB, diagnoses, medicines, complaints).
8. **Do not commit database files** (`.mdf`, `.ldf`, `.bak`) or `Reference/`.
9. **Prefer editing existing files** over creating new ones. Do not create documentation files unless the task asks for them.
10. **Ask before deleting** user-authored files, migrations that have been applied anywhere, or any data.

---

## 3. Folder Standards

### 3.1 Repository Layout

```
SRClaudeCode/                                  (repository root)
├── CLAUDE.md                                  This file
├── README.md                                  Local setup and run instructions
├── .gitignore
├── .gitattributes
├── .claude/
│   └── agents/                                Agent definitions (shared)
├── BRD/                                       Business requirements (read-only source)
├── documents/                                 Analysis, plans, reports (generated and maintained)
├── src/
│   ├── backend/
│   │   ├── PatientManagement.sln
│   │   ├── PatientManagement.API/             Presentation
│   │   ├── PatientManagement.Application/     Use cases (Commands/, Queries/, Validators/, Interfaces/)
│   │   ├── PatientManagement.Domain/          Entities, value objects, rules, domain exceptions
│   │   ├── PatientManagement.Infrastructure/  Dapper repositories, UoW, migrations, security, export
│   │   └── tests/
│   │       ├── PatientManagement.Domain.UnitTests/
│   │       ├── PatientManagement.Application.UnitTests/
│   │       ├── PatientManagement.Infrastructure.IntegrationTests/
│   │       └── PatientManagement.API.IntegrationTests/
│   └── frontend/
│       └── patient-management-web/            Angular 19 workspace
└── Reference/                                 Not part of the product; excluded from Git
```

Project folder names may be finalized in Sprint 1 by the tech lead. Once set, they are not renamed without a recorded decision.

### 3.2 Backend Folder Rules

| Layer | Contains | Must not contain |
|-------|----------|------------------|
| API | Controllers, request and response DTOs, middleware, filters, startup configuration | SQL, business rules, direct database access |
| Application | `Commands/`, `Queries/` (one folder per feature area, one handler per use case), validators, application services, interfaces for repositories, unit of work, clock, current user, token service | HTTP types, SQL text, framework-specific web types |
| Domain | Entities, value objects, business rules, domain exceptions, repository contracts | Any reference to other layers, Dapper, ASP.NET Core, FluentValidation |
| Infrastructure | Dapper repositories, connection factory, unit of work, migrations (`Migrations/` as versioned SQL scripts), password hashing, JWT signing, audit writer, CSV and PDF generation | Business rules that belong in Domain or Application |

Feature area folders use the module keys from the implementation plan: `Auth`, `Admin`, `Patients`, `Appointments`, `Dashboard`, `Consultation`, `Prescriptions`, `History`, `Exports`, `Platform`.

### 3.3 Frontend Folder Rules

```
src/app/
├── core/          Singletons: auth service, token state, guards, interceptors, API base client, error mapping, models (API contracts)
├── shared/        Stateless, standalone presentational components, pipes, directives, validators
├── layout/        Authenticated shell (toolbar, side navigation, content area)
├── features/      One folder per module: dashboard, patients, appointments, consultation, prescription, history, export, settings
│   └── <feature>/ routes file, pages/, components/, services/, store/ (signals) when needed
├── app.config.ts  Application providers (router, HTTP client with interceptors, animations)
└── app.routes.ts  Root routes; lazy-loads each feature's routes file
```

Rules:
- `shared/` never holds state or injects feature services.
- A feature never imports another feature's internals. Shared needs go to `core/` or `shared/`.
- `core/` is imported by features; features are never imported by `core/`.

---

## 4. Naming Conventions

### 4.1 General

- Use full, descriptive names. Avoid abbreviations except widely known ones such as `Id`. Write `DateOfBirth`, not `Dob`; `BloodPressure`, not `Bp`.
- Use the domain language from the BRD: Patient, Appointment, Visit (consultation), Vitals, Complaint, Diagnosis, Medication, Prescription.
- Booleans are questions: `IsActive`, `HasDuplicate`, `CanEdit`.
- No Hungarian notation. No prefixes like `m_` in C#.
- Avoid generic names: `Manager`, `Helper`, `Util`, `Data`, `Info`. Name by responsibility.

### 4.2 C# (Backend)

| Element | Convention | Example |
|---------|-----------|---------|
| Namespace | Matches folder, `PatientManagement.<Layer>.<Feature>` | `PatientManagement.Application.Patients.Commands` |
| Class, record, struct, enum, enum member | PascalCase | `PatientService`, `AppointmentStatus.NoShow` |
| Interface | `I` prefix | `IPatientRepository` |
| Method | PascalCase, verb first | `GetPatientByIdAsync` |
| Async method | `Async` suffix | `CreatePatientAsync` |
| Private field | `_camelCase` | `_connectionFactory` |
| Parameter, local | camelCase | `patientId` |
| Constant | PascalCase | `MaximumPageSize` |
| Command | `<Verb><Noun>Command` | `CreatePatientCommand` |
| Command handler | `<Command>Handler` | `CreatePatientCommandHandler` |
| Query | `<Get, List or Search><Noun>Query` | `SearchPatientsQuery` |
| Request DTO | `<Verb><Noun>Request` | `CreatePatientRequest` |
| Response DTO | `<Noun>Response` | `PatientResponse` |
| Validator | `<Request or Command>Validator` | `CreatePatientValidator` |
| Repository | `<Aggregate>Repository` | `AppointmentRepository` |
| Exception | `<Problem>Exception` | `PatientNotFoundException` |
| Controller | `<Resource>Controller`, plural resource | `PatientsController` |
| Test class | `<ClassUnderTest>Tests` | `CreatePatientCommandHandlerTests` |
| Test method | `<Method>_<Scenario>_<Expected>` | `Handle_DuplicateNameAndDob_ReturnsWarning` |

### 4.3 SQL Server (Database)

| Element | Convention | Example |
|---------|-----------|---------|
| Tables | PascalCase, plural | `Patients`, `Visits`, `PrescriptionPrints` |
| Columns | PascalCase | `DateOfBirth`, `IsActive` |
| Primary key column | `<SingularTable>Id` | `PatientId` |
| Foreign key column | Referenced key name | `PatientId` in `Visits` |
| Primary key constraint | `PK_<Table>` | `PK_Patients` |
| Foreign key constraint | `FK_<Table>_<ReferencedTable>` | `FK_Visits_Patients` |
| Unique index | `UX_<Table>_<Columns>` | `UX_Appointments_Slot` |
| Non-unique index | `IX_<Table>_<Columns>` | `IX_Visits_PatientId_VisitDate` |
| Check constraint | `CK_<Table>_<Rule>` | `CK_Vitals_Temperature` |
| Default constraint | `DF_<Table>_<Column>` | `DF_Patients_IsActive` |
| Migration script | `V<NNNN>__<snake_case_description>.sql` | `V0003__create_patients.sql` |
| Money, dates | Dates stored as UTC `datetime2(3)`; date-only as `date` | `CreatedDate datetime2(3)` |

Rules:
- Every table has the audit columns defined in section 6.4.
- No stored procedures in Phase 1 unless a plan change records the reason.
- No `SELECT *` in application SQL.

### 4.4 Angular (Frontend)

| Element | Convention | Example |
|---------|-----------|---------|
| File names | kebab-case with role suffix | `patient-list.component.ts`, `patients.service.ts` |
| Component class | PascalCase, `Component` suffix | `PatientListComponent` |
| Selector | `app-` prefix, kebab-case | `app-patient-card` |
| Service | `Service` suffix | `PatientService` |
| Store (signals state) | `Store` suffix | `ConsultationStore` |
| Guard | `Guard` suffix; functional | `authGuard`, `unsavedChangesGuard` |
| Interceptor | `Interceptor` suffix; functional | `authInterceptor` |
| Pipe | `Pipe` suffix, file `.pipe.ts` | `AgePipe` in `age.pipe.ts` |
| Directive | `Directive` suffix | `AutofocusDirective` |
| Validator function | `Validator` suffix, file `.validator.ts` | `phoneNumberValidator` |
| Routes file | `<feature>.routes.ts` | `patients.routes.ts` |
| Model (API contract) | `.model.ts`, interface with plain noun | `patient.model.ts`: `Patient`, `PatientSummary` |
| Test file | Same name plus `.spec.ts` | `patient-list.component.spec.ts` |
| Variables, methods, signals | camelCase; signal names are nouns | `selectedPatient`, `loadPatients()` |
| Observables | `$` suffix | `patients$` |
| Constants | UPPER_SNAKE_CASE | `DEBOUNCE_MS` |

---

## 5. Coding Standards

### 5.1 General

- Follow SOLID. One reason to change per class. Depend on abstractions at layer boundaries.
- Keep functions small and focused. If a method needs a comment to explain what it does, rename it or split it.
- Comment *why*, not *what*. Comment non-obvious SQL and business rules.
- No dead code, commented-out code, or unused imports. No TODO without a linked issue number (`TODO(#42): ...`).
- Handle errors at the boundary: validation in the API layer, business errors as domain exceptions, unexpected errors in middleware.
- Every new behaviour comes with tests (section 9).
- Keep changes minimal: no unrelated refactors in a feature branch.

### 5.2 Formatting

- Editors respect `.editorconfig` at the repository root (created in Sprint 1). Settings: UTF-8, LF line endings in the repository (`.gitattributes`), 4 spaces for C#, 2 spaces for TypeScript, HTML, SCSS, JSON, and YAML.
- Run the formatter before committing (`dotnet format` for C#, Prettier for Angular code).
- Maximum line length: 120 characters for C#, 100 for TypeScript.

### 5.3 Comments and Documentation

- Public API endpoints are documented through XML comments (for Swagger) and summary text.
- Public service and repository interfaces have a one-line summary.
- Angular components have a one-line description only when the purpose is not obvious from the name.
- Update `documents/` only when the task asks for it or when a decision changes.

---

## 6. .NET Standards

### 6.1 Project Settings

- Target framework: .NET 10 for all projects.
- Nullable reference types enabled in every project. No `#nullable disable`.
- Warnings treated as errors in the Release build configuration, checked locally before each commit.
- Implicit usings enabled. File-scoped namespaces.
- `records` for DTOs, commands, and queries. `class` for entities.
- Analyzer packages enabled (Microsoft.CodeAnalysis.NetAnalyzers) at the warning level agreed in Sprint 1.

### 6.2 Clean Architecture Rules

- Dependency direction: API → Application → Domain; Infrastructure → Application and Domain. Domain depends on nothing.
- Controllers call handlers or application services only. No repository or Dapper calls in controllers.
- Handlers depend on interfaces (repository, unit of work, clock, current user). Concrete classes are registered only in Infrastructure and the API composition root.
- Domain entities protect their invariants. Setters are private; changes go through methods that validate (for example, `Appointment.Reschedule` checks the future date and the slot).
- Domain exceptions derive from one base type and are mapped to HTTP responses by middleware, never by controllers.

### 6.3 Dapper and Data Access

- All database access goes through the connection factory. Never construct a connection in a handler, service, or controller.
- Repositories return domain entities or read models. Mapping to DTOs happens in the API or Application layer.
- Use parameterized queries only. Never concatenate values into SQL. Never use `SELECT *`. List columns explicitly.
- Every read query filters `IsActive = 1` unless it is an explicit admin or audit read, and the method name says so (for example, `GetIncludingInactiveAsync`).
- Use `CancellationToken` in every async data call.
- Write operations that touch more than one table run inside a unit of work transaction. Read-only queries do not open a transaction.
- Update statements that can conflict check the `RowVersion` and treat zero rows affected as a concurrency conflict.
- Use `QueryMultiple` or joins to avoid N+1 queries. Never query inside a loop.
- Command timeouts are set explicitly. Queries slower than 500 ms are logged at warning level in development.

### 6.4 Audit and Time

- Every transactional table has: `IsActive` (default 1), `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy`, `RowVersion`.
- Audit fields are set in one place (the unit of work or a shared helper), never in controllers or individual handlers.
- Time comes from an injected `IClock` returning UTC. Never call `DateTime.Now` or `DateTime.UtcNow` directly in application code.
- The current user comes from the `ICurrentUser` abstraction, which reads JWT claims.
- Every data change writes an audit row (table, record id, action, changed fields as old and new JSON values, UTC timestamp, user id) in the same transaction.
- The audit log is append-only. The application login has insert and select rights only on it.

### 6.5 API Design

- Versioned routes: `/api/v1/<resource>`. Plural resource names, lowercase, hyphenated for compound names.
- Standard verbs: GET (read), POST (create), PUT (replace or update), DELETE (soft delete). State changes use a named sub-resource (for example, `POST /api/v1/appointments/{id}/complete`).
- Paged list responses use one shape: items, total count, page number, page size. Page size is capped at 100.
- Errors use one shape with a stable error code, a user-safe message, and a correlation id. No stack traces or SQL text in responses.
- Status codes: 200 (read or update), 201 (created, with location), 204 (no content), 400 (validation), 401 (not authenticated), 403 (not authorized), 404 (not found, also used for other users' data), 409 (conflict, for example, slot taken), 422 (business rule), 500 (unexpected, generic message).
- Vital-sign warnings are returned in the response body as warnings, with HTTP 200 or 201. Impossible values return 400.

### 6.6 Validation

- FluentValidation runs for every request before the handler. One validator per request.
- Validators check format, length, required fields, and ranges. Business state checks (for example, "appointment is in the past") belong in the domain.
- Phone numbers are normalized to digits for storage and search; display format is applied by the client.
- Text fields have explicit maximum lengths (complaint text 2000 characters, other free text as defined in the BRD).

### 6.7 Logging and Errors

- Use `ILogger<T>` with structured properties. Never string-interpolate values into the message.
- Levels: Error (unexpected failures), Warning (rejected requests, lockouts, slow queries), Information (startup, migrations applied, configuration), Debug (development only).
- Never log passwords, tokens, refresh tokens, connection strings, or patient-identifying data. Log record ids only.
- Every request has a correlation id that appears in logs and error responses.

### 6.8 Configuration

- Configuration is read through the options pattern, one options class per section, validated at startup.
- `appsettings.json` holds non-secret defaults only. `appsettings.Development.json` holds LocalDB settings and the dev CORS origin (`http://localhost:4200`).
- Secrets come from user secrets (development) or the production secret store. Never from source files.

### 6.9 Dependency Injection

- Register services in extension methods per layer (`AddApplication`, `AddInfrastructure`, `AddApi`).
- Lifetimes: repositories and unit of work scoped per request; validators and handlers transient or scoped; clock and options singleton.
- No service locator. No `IServiceProvider` injected into handlers.

---

## 7. Git Conventions

### 7.1 Branches

| Branch | Purpose | Protected |
|--------|---------|-----------|
| `main` | Released code; tagged `v1.0.0` and later | Yes |
| `develop` | Integration; default branch | Yes |
| `release/vX.Y.Z` | Stabilization from Sprint 11 | Yes |
| `feature/`, `fix/`, `refactor/`, `test/`, `docs/`, `chore/`, `spike/` | Work in progress | No |
| `hotfix/` | Production fixes, PR to `main` | No (PR target only) |

Branch names:
- Format `<type>/<issue-number>-<module>-<short-slug>`, or `<type>/<module>-<short-slug>` when no issue exists.
- Lowercase, hyphen-separated, ASCII, at most 50 characters after the prefix.
- Module keys: `platform`, `auth`, `admin`, `patients`, `appointments`, `dashboard`, `consult`, `rx`, `history`, `export`, `ui`, `db`, `ops`.
- Examples: `feature/42-auth-refresh-rotation`, `fix/57-appointments-slot-timezone`, `spike/search-fulltext-localdb`.
- Never reuse a branch name after its PR is merged.

### 7.2 Worktrees

- One task, one branch, one worktree. Worktrees live outside the repository at `d:\wt\pms\`. Folder name = branch name with `/` and `.` replaced by `-`.
- Never work in the main working tree for feature changes. It stays on `develop` and is used for worktree commands and syncing.
- Maximum two worktrees per developer at a time.
- The release worktree is locked for its lifetime.

### 7.3 Commits

- Conventional commit subject: `<type>(<scope>): <imperative summary>`, under 72 characters, no trailing period.
- Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `db` (migration, always in the same commit or PR as the code that uses it).
- Scope is the module key (`patients`, `auth`, `consult`).
- Example: `feat(patients): add duplicate warning on name and date of birth`
- Body (optional) explains why and any non-obvious trade-off. Wrap at 72 characters.
- One logical change per commit. Do not mix formatting-only changes with behaviour changes.
- AI-authored commits end with the trailer:
  `Co-Authored-By: Claude Haiku 5.5 <noreply@anthropic.com>`
  The user's own instructions about attribution take precedence over this line.
- Never commit secrets, `bin/`, `obj/`, `node_modules/`, database files, `appsettings.*.local.json`, `.claude/settings.local.json`, or `Reference/`. Check `git status` before every commit.

### 7.4 Destructive Operations (Require Explicit Approval)

Describe the impact and ask before running any of these:

- `git reset --hard`, `git clean -fd`, `git checkout -- <file>` or `git restore` on files with uncommitted changes
- `git push --force` or `--force-with-lease` (never to protected branches, under any circumstance)
- `git branch -D`, `git worktree remove --force`, `git worktree prune` on folders that still exist
- `git stash drop`, `git rebase` of a shared branch
- Deleting any file the user authored (not generated build output)

Before any approved destructive step: check `git status`, confirm the worktree is clean or the changes are understood, and list the commits that would be lost.

Approved cleanup that does not need a new approval: after a PR is confirmed `MERGED`, removing its clean worktree and deleting its local branch (see `documents/worktree-plan.md`, section 9.2).

### 7.5 Pull Requests

- Target `develop` for all work except release and hotfix PRs.
- Title matches the squash commit subject.
- Description follows the template in `documents/worktree-plan.md` (Summary, Changes, Testing, Impact, Closes #issue).
- Production code change per PR: aim for about 400 lines. Split larger work.
- Migrations are reviewed by the tech lead.
- PR description ends with the attribution line `🤖 Generated with [Claude Code](https://claude.com/claude-code)` when Claude Code created it.
- Never merge a PR without approval and with the local checks (worktree-plan section 8.3) recorded as passed. Never auto-merge.

### 7.6 Syncing

- Sync feature branches by merging `develop` into them (`git fetch origin` then `git merge origin/develop`). Do not rebase shared branches.

---

## 8. Angular Standards

### 8.1 Project Setup

- Angular 19, standalone components only. Do not create NgModules.
- Application providers live in `app.config.ts`: router with lazy routes, HTTP client with functional interceptors, animations, and Material theme configuration.
- Services use `providedIn: 'root'`. No `forRoot()` pattern.
- Strict TypeScript: `strict: true`, `noImplicitAny`, `noPropertyAccessFromIndexSignature`. No `any`. Use `unknown` and narrow.
- Control flow uses built-in blocks: `@if`, `@for` (with `track`), `@switch`, `@defer` for non-critical UI.

### 8.2 Components

- Every component uses `changeDetection: ChangeDetectionStrategy.OnPush`.
- Use the `inject()` function for dependencies, not constructor parameters.
- Smart (container) components own feature state and services. Presentational components receive inputs with `input()` and emit outputs with `output()`.
- Templates contain no business logic. Move logic to the component class, a store, or a pipe.
- No direct DOM manipulation. No `innerHTML` with user content (Angular sanitization is not a substitute for not doing it).
- Accessibility: use Material components; provide labels for every input; keep focus visible; dialogs trap focus; errors are announced (`role="alert"` or live regions).

### 8.3 Forms

- Reactive Forms only (`FormBuilder` or typed `FormGroup`). No template-driven forms.
- Use typed forms (`FormControl<string | null>` etc.). No untyped `FormGroup`.
- Validators: built-in validators first; custom validators in `shared/validators/` with unit tests.
- Show the error under the field on touch or submit, not on first render.
- Submit is disabled only while the form is invalid or saving. Show a progress indicator while saving.
- Vitals form: normal-range values show a warning banner and allow save; plausible-range violations block save with an inline error.
- Forms with unsaved changes use the `unsavedChangesGuard`.

### 8.4 State and Data

- Feature state lives in a signals-based store (one per feature area) that exposes read-only and computed signals.
- HTTP calls return RxJS Observables from services. Bridge to signals with `toSignal` or `rxResource` where it simplifies templates.
- Search input uses `debounceTime`, `distinctUntilChanged`, and `switchMap`.
- Subscriptions are closed with `takeUntilDestroyed()` or the `async` pipe. No unmanaged subscriptions.
- Consultation drafts are saved to the server. Browser storage is used only for per-user conveniences (for example, remembering the last tab), never for patient data.
- Do not store the access token in local storage or session storage. Keep it in memory. The refresh token is an HTTP-only cookie managed by the server.

### 8.5 Routing and Lazy Loading

- Each feature has its own routes file loaded with `loadChildren`. Heavy pages can use `loadComponent`.
- Authenticated routes sit under the shell route guarded by `authGuard`.
- Use `PermissionGuard` with a required policy for protected pages. The server remains the authority.
- Route paths are lowercase and hyphenated: `/patients/:patientId/visits/:visitId`.

### 8.6 HTTP

- Functional interceptors: auth (attach token, single refresh on 401, concurrent requests wait), error (map to user-friendly messages, show retry for network and 5xx errors), and optional loading.
- Services never read raw `HttpClient` errors directly. The error interceptor normalizes them.
- API base URL comes from environment files. No hard-coded hosts.

### 8.7 Styling and Theme

- Angular Material for components. Theme defined once with design tokens (colors, spacing, typography) in `assets/styles/`.
- Blue and green healthcare palette, checked for WCAG AA contrast.
- Layout is desktop-first and works at tablet width (768 px and up). Consultation controls are touch-friendly (minimum 44 px target).
- No inline styles for layout. Component styles use SCSS with component-scoped selectors. Global styles are limited to `styles/`.

### 8.8 Internationalization

- All user-visible strings come from translation files (`assets/i18n/en.json`) via keys. No hard-coded English text in templates or TypeScript (except test data).
- Dates and numbers use locale-aware pipes with the clinic's locale.

### 8.9 Angular Naming and Structure

Follow section 4.4 for file and symbol names and section 3.3 for folder placement.

---

## 9. Testing Standards

### 9.1 Coverage Targets

| Scope | Target |
|-------|--------|
| Overall (backend and frontend) | 70% line coverage |
| Consultation, prescription, and authentication code | 90% line coverage |
| New code in a PR | No drop below the target; coverage recorded locally in the PR |

The local coverage check must pass before a PR is opened; a PR below target is not ready for review. Critical-path targets are reviewed each sprint.

### 9.2 Backend Tests

- Framework: xUnit. Mocking: NSubstitute. Assertions: FluentAssertions or xUnit assertions (one style per test project, chosen in Sprint 1).
- Unit tests (Domain, Application): no database, no HTTP, fixed clock, fake or substitute repositories.
- Repository and unit of work integration tests: Infrastructure.IntegrationTests against a dedicated test database on LocalDB, created from migrations for each run. Each test uses its own data and cleans up or uses a transaction scope.
- API integration tests: in-process host with the real test database. Cover authentication, authorization (including patient ownership), validation errors, conflicts, paging, and error shape.
- Test data comes from the seeding tool or test builders. Never from a copy of real data.
- Test names follow `<Method>_<Scenario>_<Expected>`. Arrange, Act, Assert sections are separated by blank lines or comments only when the test is long.
- One behaviour per test. No logic (loops, conditionals) inside tests.

### 9.3 Frontend Tests

- Framework: Vitest for unit and component tests (decision recorded in Sprint 1; Jasmine is acceptable only if the team decides so in writing). Karma is not used.
- Test stores, services (with `HttpTestingController`), guards, interceptors, pipes, validators, and form components.
- Component tests use Angular testing utilities with harnesses for Material components.
- Do not test Angular Material internals.
- End-to-end: Playwright, running on Chromium and WebKit. Firefox is not supported (Q11).
- E2E critical journeys: sign in and out; create patient; schedule appointment; start consultation; record vitals (warn and block); add medications; complete visit; print prescription; reprint; history search; export CSV and PDF.
- E2E tests use the seeded synthetic dataset and never depend on another test's data.

### 9.4 Accessibility and Performance

- Automated accessibility checks (axe-based) run locally on main screens from Sprint 7. Critical violations must be fixed before the release candidate (Sprint 11).
- Performance targets from the BRD: page load under 2 seconds; patient search under 2 seconds; history under 3 seconds; export under 10 seconds; prescription print dialog under 5 seconds.
- Load tests run in Sprint 11 against 10,000 synthetic patients and 100,000 visits.

### 9.5 Test Hygiene

- Tests are deterministic: no real time, no random data without a fixed seed, no dependency on order.
- Flaky tests are fixed or quarantined with an issue number within one working day. They are never retried silently.
- Do not weaken assertions or delete tests to make a build pass. Report failing tests and their output.

---

## 10. Security Standards

### 10.1 Authentication

- Single doctor account in Phase 1. Username and password login returns an access token in the response body and a refresh token in an HTTP-only, Secure, SameSite=Strict cookie scoped to the refresh endpoint.
- Access token: RS256 signed, 15-minute lifetime, claims for user id, username, and permissions. Validate issuer, audience, signing key, and lifetime.
- Refresh token: 8-hour absolute lifetime, rotated on every use, reuse of a rotated token revokes the token family. Only a hash is stored. Logout revokes the current refresh token.
- Inactivity timeout: 30 minutes. The client logs out after 30 minutes without user input. The server rejects a refresh if the token has been idle for more than 30 minutes.
- Password change revokes all refresh tokens for the user.

### 10.2 Passwords

- Hashing: bcrypt with a work factor agreed in Sprint 1 and stored in configuration. Salt per password.
- Policy: minimum 8 characters with upper-case, lower-case, digit, and symbol.
- Lockout: 5 failed attempts within 15 minutes locks the account for 15 minutes. Failure count and lock time are stored on the user record.
- Login errors are generic ("invalid username or password"). Never reveal whether a username exists.
- No password, hash, or token is ever logged, returned in a response, or stored in plain text.

### 10.3 Authorization

- Policy-based authorization with named policies (for example, `CanRecordDiagnosis`, `CanExportData`). Never check raw role names in code.
- Every patient-scoped endpoint checks ownership in addition to the policy.
- Return 404 for records that exist but belong to another user, to avoid confirming existence.
- Front-end guards mirror policies for navigation only.

### 10.4 Transport and Headers

- HTTPS only. HTTP is redirected in production. TLS 1.2 minimum, 1.3 preferred.
- HSTS in production. Security headers on all responses: Content-Security-Policy, X-Content-Type-Options, X-Frame-Options, Referrer-Policy.
- CORS: development allows `http://localhost:4200` only. Production allows the production site origin only. Never `*` with credentials.
- Swagger UI is enabled in development only.
- Rate limiting: tight limit on the login endpoint; per-user limit on the general API.

### 10.5 Data Protection

- Production: TDE and encrypted backups on the database. No column-level encryption in Phase 1 (keyword search depends on indexes).
- Development: LocalDB has no TDE. Synthetic data only.
- No patient data in logs, analytics, error messages, file names, or test fixtures copied from production.
- PDF and CSV exports are generated on the server and downloaded with `Content-Disposition: attachment`. Filenames use the record type and date, never patient names.
- Print and PDF layouts escape all text. No active content in PDFs.

### 10.6 Input and Output

- Server-side validation with FluentValidation on every request. Client-side validation mirrors it for usability only.
- SQL uses parameters exclusively. Reviewers reject string-built SQL.
- Angular templates use default escaping. Do not bypass the sanitizer or use `bypassSecurityTrust*` methods.
- Text length limits enforced on the server.

### 10.7 Secrets and Configuration

- No secrets, certificates, or connection strings with credentials in source control. Check before every commit.
- Development: user secrets for the JWT signing key and any credentials. LocalDB uses Windows authentication, so no password is stored.
- Production: secret store (Azure Key Vault or protected configuration, decided in Sprint 11). The application database login has data read and write rights only; the migration login is separate and used only during deployment.

### 10.8 Dependencies

- Dependency scanning (NuGet and npm audit) is run locally at least weekly and before each release candidate. Critical and high findings must be fixed before release.
- Add new packages only when needed. Record the reason in the PR description. Prefer well-maintained packages from the official ecosystem.
- Keep Angular and .NET on supported versions. Check support status before each sprint.

### 10.9 Audit

- Every data change is audited (section 6.4). Logins, exports, prints, and record views are not audited (Q17), but failed-login counts are stored for lockout.

### 10.10 Backups and Recovery

- Backups are a go-live blocker (Q18). Development does not need backups because it uses synthetic data.
- Do not deploy to production, and do not run schema migrations against any environment that holds real data, until the backup and restore plan is in place and tested.

---

## 11. Agent Workflow

This section defines how Claude Code works in this repository. Agent definitions are in `.claude/agents/`.

### 11.1 Agents and Outputs

| Agent | Purpose | Output |
|-------|---------|--------|
| BrainstormingAgent | Analyze BRD, options, risks, open questions | `documents/brainstorm-review.md` |
| PlanningAgent | Phases, modules, dependencies, milestones | `documents/implementation-plan.md` |
| WorktreeAgent | Branch and worktree setup, audit, cleanup | Worktrees and audit output; `documents/worktree-plan.md` |
| ImplementationAgent | Code changes inside an approved worktree | Code, tests, migrations in the feature branch |
| VerificationAgent | Runs and reports build and test results | Verification report in the task output |
| GapAnalysisAgent | Checks requirements against implementation | Gap report |
| CodeReviewAgent | Technical review (quality, security, performance, tests) | `documents/code-review-report.md` |
| FinishingAgent | Delivery readiness (no code changes) | `documents/final-readiness-report.md` |

### 11.2 Standard Task Flow

For each task (one issue, one branch, one worktree):

1. **Discover.** Read this file, the issue, and the relevant sections of the BRD and plan. Confirm the sprint and milestone. Check `git status`, the current branch, and `git worktree list`.
2. **Plan.** State the files to change, the tests to add, and any migration. If the task conflicts with the BRD, the Decisions Log, or this file, stop and ask.
3. **Branch.** Use WorktreeAgent to create the branch and worktree from `origin/develop`. Never work on a protected branch.
4. **Implement.** Make the smallest change that meets the acceptance criteria. Follow sections 3 to 9.
5. **Verify.** Build, run unit and integration tests, run lint and format, and run the relevant E2E tests. Report exact results, including failures.
6. **Review.** Self-review the diff. Check security (section 10) and that no patient data or secrets are included.
7. **Hand off.** Summarize what changed, the test results, and any open risk. Commit and push only when the user asks; open a PR only when the user asks.
8. **Clean up** after the PR is merged, following section 7.4.

### 11.3 Sprint and Milestone Flow

- Each sprint follows the plan (`documents/implementation-plan.md`). Milestone gates (M1–M10) are checked before the next phase begins.
- Gate checks use GapAnalysisAgent for requirement coverage and VerificationAgent for test results.
- Before feature freeze (M6), CodeReviewAgent reviews the release scope.
- Before go-live (M9), FinishingAgent produces the final readiness report. Any unresolved backup item is reported as a deployment blocker.

### 11.4 Rules for Claude Code

- Read the source documents before proposing design changes. Cite the BRD section or Decisions Log item (Q1–Q22) that drives each decision.
- Do not re-decide answered questions. Treat the Decisions Log as final unless the user changes it.
- When a requirement is ambiguous, state the most likely reading, implement it, and list the assumption in the handoff. Ask only when the choice changes the data model, security, or a user-visible contract.
- Keep answers concise. Put long analysis into `documents/` when the user asks for a document.
- Report failures honestly: include the failing test name and the relevant output. Do not claim success for steps that were not run.
- Use the scratchpad directory for temporary files, not the repository.
- Do not modify files in `BRD/` or `Reference/`.
- Do not modify `documents/` planning files unless the task asks for an update.
- When creating a document for others to read, state its audience in one line in the reply.

### 11.5 Escalation Triggers

Stop and ask the user before continuing when any of these apply:
- A change touches the authentication model, token lifetimes, encryption, or audit scope.
- A migration would drop or rewrite data, or would change a table already applied outside development.
- A requirement conflicts with the BRD or the Decisions Log.
- A destructive git operation is needed (section 7.4).
- A new package introduces a licence or security concern.
- Production deployment is requested while the backup gate is open (Q18).

---

## 12. Commands

These commands are valid after Sprint 1 scaffolding. Run them from the worktree root unless noted. There is no CI/CD pipeline in Phase 1, so these commands are the quality gates: run the build, test, format and audit commands before opening a PR.

| Purpose | Command |
|---------|---------|
| Restore and build API | `dotnet build src/backend/PatientManagement.sln` |
| Run API (development) | `dotnet run --project src/backend/PatientManagement.API` |
| Backend unit tests | `dotnet test src/backend/PatientManagement.sln --filter "FullyQualifiedName!~IntegrationTests"` |
| Backend integration tests | `dotnet test src/backend/tests/PatientManagement.Infrastructure.IntegrationTests` and `...API.IntegrationTests` |
| Format backend | `dotnet format src/backend/PatientManagement.sln` |
| Install frontend packages | `npm ci` in `src/frontend/patient-management-web` |
| Run frontend (development) | `npm start` in `src/frontend/patient-management-web` (serves `http://localhost:4200`) |
| Frontend unit tests | `npm test` in `src/frontend/patient-management-web` |
| Frontend lint and format | `npm run lint` and `npm run format:check` in `src/frontend/patient-management-web` |
| E2E tests | `npx playwright test` in `src/frontend/patient-management-web` |
| Dependency audit | `dotnet list src/backend/PatientManagement.sln package --vulnerable --include-transitive` and `npm audit` |
| Worktree list | `git worktree list` |

If a command in this table does not exist yet, the scaffolding task that creates it must update this table in the same PR.

---

## 13. Definition of Done

A task is done only when all of these are true:
- Acceptance criteria in the issue are met and demonstrated.
- Unit and integration tests cover the new behaviour; coverage targets are met.
- Build has no warnings-as-errors failures; lint and format pass.
- No secrets, patient data, database files, or `bin/`/`obj/` output are included.
- Security checks in section 10 were considered for the change, and any new endpoint has authorization tests.
- Migrations (if any) are in the same PR, reviewed, and tested on a fresh LocalDB.
- Accessibility basics are met for new screens (labels, keyboard, contrast).
- Strings are in translation files.
- Documentation in `documents/` is updated only if the task asked for it or a decision changed.
- The handoff states what was verified, what was not, and any risks.

---

## 14. Open Items That Affect Coding

These come from the Decisions Log and the implementation plan. Resolve them before the related sprint starts, and update this file when a decision is recorded.

| Item | Needed by | Source |
|------|-----------|--------|
| Angular 19 support status check and version decision | Sprint 1 | Plan risk K3. Decided 2026-11-11: advisories accepted until the M1 review, upgrade before release (see `documents/risk-decisions.md`, R1) |
| Full-text search feasibility on LocalDB | Sprint 1 | Plan risk K2; Q5 |
| Bcrypt work factor on development hardware | Sprint 1 | Section 10.2 |
| Test frameworks confirmed (Vitest, xUnit, NSubstitute) | Sprint 1 | Sections 9.2, 9.3 |
| Vital-sign ranges confirmed by the doctor | Sprint 7 | Plan risk T6; Q7 |
| Password reset procedure (operator command) | Sprint 2 | Plan assumption A18 |
| Production hosting (IIS or App Service) and secret store | Sprint 11 | Q9; plan Phase 6 |
| Backup and recovery plan | Before production | Q18 (blocker) |
| Repository bootstrap approval | Before Sprint 1 | `documents/worktree-plan.md` section 4 |

---

## 15. Learned Constraints

Add a bullet here when a mistake or surprise changes how the team should work. Keep each bullet to one line with the date and the reason.

- (none yet)
