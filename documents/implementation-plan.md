# Implementation Plan: Patient Management System

**Date**: 2026-11-10
**Prepared by**: PlanningAgent
**Inputs**: `BRD/Doc_BRD.md`, `documents/brainstorm-review.md` (including the Decisions Log for Q1–Q22)
**Status**: Baseline draft for approval
**Confidence**: MEDIUM-HIGH for scope and sequencing; MEDIUM for effort (±20%, team size assumed, see Assumptions)

---

# Executive Summary

## Project Overview

A browser-based Patient Management System for one general physician in a single clinic. It covers patient registration and profiles, 15-minute appointment scheduling, the consultation workflow (mandatory vitals, complaints, diagnosis, medications), printable prescriptions with a print-time snapshot, visit history with date and keyword search, and CSV/PDF export.

The stack is fixed by the request: Angular 19 with Angular Material, RxJS and Reactive Forms on the front end; ASP.NET Core Web API on .NET 10 with Dapper, JWT and FluentValidation on the back end; SQL Server (LocalDB for development, SQL Server Standard or Azure SQL for production). The architecture is Clean Architecture with Repository and Unit of Work patterns, dependency injection and SOLID principles, plus a CQRS-lite folder convention (no mediator library).

## Scope

- **In scope**: six delivery phases covering the Phase 1 functional scope in the BRD and the Decisions Log answers.
- **Out of scope for this release**: receptionist and other multi-user access, billing, insurance, lab or pharmacy integration, AI features, offline mode, a mobile app, analytics, multi-clinic support, and follow-up reminders (BRD Out of Scope list).

## Objectives

1. Replace paper consultation records with structured digital records.
2. Let the doctor complete a consultation record in 2–3 minutes (BRD success criterion).
3. Return patient search and full history within 2–5 seconds.
4. Produce prescriptions that print cleanly and stay accurate on reprint.
5. Export patient and visit data as CSV and PDF.
6. Achieve the 80% paper reduction target, measured after go-live.

## Deliverables

| # | Deliverable | Produced in |
|---|-------------|-------------|
| D1 | Working development environment (Angular CLI, API on Kestrel/IIS Express, LocalDB) | Phase 1 |
| D2 | Authentication with username/password, JWT access tokens, refresh token rotation, lockout and idle timeout | Phase 1 |
| D3 | Clinic profile settings used on prescriptions; own-password change | Phase 1 |
| D4 | Patient management: registration, profile, search, recent patients | Phase 2 |
| D5 | Appointment calendar (15-minute slots), status management, dashboard | Phase 3 |
| D6 | Consultation capture (vitals, complaints, diagnosis, medications) with audit trail | Phase 4 |
| D7 | Prescription generation, print and reprint with snapshots; visit history with filters | Phase 4 |
| D8 | CSV and PDF export (patient list, visit data, prescription PDF) | Phase 5 |
| D9 | Hardening report, security review, performance results, production deployment package and runbook | Phase 6 |
| D10 | UAT sign-off by the doctor, release notes, hypercare handover | UAT and go-live |

## Headline Plan

| Item | Plan |
|------|------|
| Build effort | About 240 developer-days (base), ±20% |
| Team assumed | 1 tech lead (50%), 3 developers (1 back end, 1 front end, 1 full-stack), 0.5 QA, doctor as product owner for UAT |
| Build duration | 11 two-week sprints (22 weeks) for Phases 1–6 |
| UAT | 2 weeks (Sprint 12), doctor on synthetic data |
| Contingency | 1 reserved sprint (Sprint 13), used only if triggered |
| Base go-live | End of Week 24 (Sprint 12); Week 26 if contingency is used |
| Hypercare | 4 weeks after go-live (development team) |
| Go-live blocker | Backup and recovery decision (Q18) must be implemented before production go-live |

The brainstorm review estimated 12–16 weeks. That was a top-down figure. This plan is bottom-up per module and layer. It gives 22 weeks of build plus 2 weeks of UAT with a 3-developer team. If the team is 4 developers, the build compresses to about 16–18 weeks. The difference is a staffing decision, not a scope change.

---

# Project Scope

## In Scope Features

| Area | Features (Phase 1 release) |
|------|-----------------------------|
| Access | Username/password login (JWT); refresh token rotation; 5 failed logins in 15 minutes locks the account for 15 minutes; 30-minute inactivity timeout; change own password |
| Clinic profile | Editable text settings used on prescriptions: clinic name, doctor name, qualifications, registration number, address, phone, footer notes. No logo |
| Patients | Add, edit, view; fields: first name, last name, DOB (age calculated), gender, phone, address (optional), email (optional), blood group (fixed list), chronic conditions (free text); soft delete; duplicate warning on same name plus DOB; search by partial name (case-insensitive) or phone; recent patients (last 20) |
| Appointments | 15-minute fixed slots; daily and weekly calendar; create; reschedule (future appointments only); status Scheduled → Completed / Cancelled / No-show; one patient per slot |
| Dashboard | Today's appointments, upcoming appointments, recent patients, quick actions (new patient, new appointment, search) |
| Consultation | Start from an appointment; vitals (temperature, blood pressure, pulse) with warn-and-allow for abnormal values and block for impossible values; complaints (free text, 2000 characters); diagnosis notes with optional diagnosis code; medications (name, dosage, frequency, duration, instructions) with autocomplete from the doctor's own history; draft save; complete consultation |
| Editing | Completed visits remain editable; every change is audited with old and new values; last-modified shown on the visit |
| Prescription | Generated from the visit; header from clinic profile; patient details; visit date; vitals; diagnosis; medications; signature area; footer. Print dialog; reprint; a snapshot is stored at each print |
| History | Chronological visit list per patient; visit detail; date-range filter; keyword search across complaints, diagnosis and medicine names within one patient's history |
| Export | Patient list (CSV); visit data (CSV and PDF) with date range; prescription (PDF) |
| Audit | Data changes only (create, update, delete, with old and new values). Failed-login counts kept for lockout |
| Platform | Structured application logs; health endpoint; API documentation (Swagger/OpenAPI); centralized error handling |

## Out of Scope Features

- Receptionist, nurse, admin and any multi-user access; patient portal
- Billing, invoicing, insurance
- Lab or pharmacy integration; drug master table
- AI diagnosis or recommendations
- Offline mode; mobile application (phones not optimised)
- Analytics and advanced reporting dashboards
- Multi-clinic or multi-tenant support (no ClinicId columns, per Q22)
- Follow-up alerts, reminders, email or SMS notifications
- Login and API-access audit trail (per Q17)
- Retention purge jobs (per Q8: keep indefinitely, soft delete only)
- Logos on prescriptions (per Q4)
- Firefox formal support (per Q11)
- Translated UI (English only; i18n-ready structure only, per Q14)

## Future Enhancements (Phase 2 and later, not planned here)

| Enhancement | Trigger | Notes |
|-------------|---------|-------|
| Multi-user roles (Admin, Nurse, Receptionist) | Second user needed | Roles and policies are data-driven from Phase 1 (only Doctor seeded). NgRx SignalStore only if concurrent editing appears |
| Patient portal | Separate BRD needed | Identity verification and consent handling required |
| SSO / EMR integration | EMR integration planned | Phase 1 uses username/password |
| Standard drug and diagnosis code lists (ICD-10) | Doctor requests code lookup | Phase 1 is free text with optional code |
| Advanced search filters, clinic statistics | After go-live usage data | Phase 1.5 per brainstorm |
| Two-factor authentication (TOTP) | Security review finding or policy | Phase 1.5 per brainstorm |
| Follow-up reminders and notifications | Business request | Requires a new scope decision |
| Prescription logo and custom templates | Doctor request | Phase 1 uses editable text only |
| Keyset paging, caching, read replicas | Measured performance need | Only if load tests fail targets |

## Key Constraints

- **Timeline**: no fixed deadline (Q19). Milestones are relative to kickoff (Week 0), not calendar dates.
- **Resources**: 3 developers and 0.5 QA assumed. No dedicated DBA; the tech lead covers database tasks.
- **Technical**: Angular 19 and .NET 10 are mandated. Development runs on LocalDB with Windows authentication. TDE is not available in LocalDB.
- **Infrastructure and budget**: no budget figure was given. Production hosting (IIS, App Service or Azure SQL) is decided in Phase 6.
- **Data**: development uses synthetic data only. Real patient data is not allowed until production controls are in place.
- **Regulatory**: designed for future HIPAA and local compliance; not certified in Phase 1 (brainstorm Assumption 8).

---

# Architecture Plan

## High-Level Architecture

```
Browser (Chrome / Edge / Safari, latest two versions)
   │  HTTPS (TLS 1.2+; HTTP redirected in production)
   ▼
Angular 19 SPA  ──  Angular CLI dev server on localhost:4200 (development)
   │  JSON over HTTPS, JWT bearer token, /api/v1/...
   ▼
ASP.NET Core Web API (.NET 10)  ──  Kestrel / IIS Express (development)
   │  Presentation → Application → Domain ← Infrastructure
   │  Dapper (parameterized SQL), Unit of Work, connection factory
   ▼
SQL Server  ──  LocalDB (development, (LocalDb)\MSSQLLocalDB, Windows auth)
                 SQL Server Standard or Azure SQL with TDE (production)
```

Cross-cutting: structured logging, health endpoint, Swagger/OpenAPI, centralized error handling, rate limiting, security headers, a clock abstraction and a current-user abstraction.

## Frontend Architecture

- Standalone components throughout (Angular 19); no NgModules. Providers are set once in the application configuration.
- Feature-based folders (dashboard, patients, appointments, consultation, prescription, export, settings). Each feature has its own route file, lazy-loaded from the root route table.
- An authenticated shell (toolbar, side navigation, content area) wraps all feature routes. The auth guard is applied once at the shell.
- State: signals-based feature stores for UI state; RxJS for HTTP and search streams. No NgRx in Phase 1.
- Angular Material for components; custom theme in blue and green; design tokens so the theme can change without touching components.
- Reactive Forms for every data-entry screen. Validation messages come from a shared mapping that mirrors the API's FluentValidation rules.
- Responsive layout for desktop and tablet widths (768 px and up). Touch-friendly consultation controls. Not optimised for phones.
- Accessibility: semantic markup, keyboard navigation, focus management in dialogs, colour contrast; automated checks run locally (WCAG 2.1 AA best effort, per Q13).
- Internationalization: all user-facing strings in translation files from day one (English only, per Q14).

## Backend Architecture

- Clean Architecture in four layers: Presentation (API), Application, Domain, Infrastructure. Dependencies point inward; the Domain layer has no framework references.
- Application layer uses a CQRS-lite convention: commands (writes) and queries (reads) in separate folders, with one handler class per use case. No mediator library and no separate read store (per the brainstorm CQRS evaluation, score 2/5).
- Repository pattern over Dapper for aggregate access. A Unit of Work owns one connection and one transaction per request and exposes the repositories that share it.
- Dependency injection throughout. Services are registered by feature extension methods to keep Program startup small.
- FluentValidation for request validation in the Application layer; domain rules enforced in entities and value objects.
- Policy-based authorization (for example "CanRecordDiagnosis"). Only the Doctor role is seeded in Phase 1.
- Versioned URLs (`/api/v1/...`); consistent response envelope; standard error format without internal details.

## Database Architecture

- SQL Server, normalized to 3NF. Medications, vitals, complaints and diagnoses are separate tables, not delimited columns.
- Master (reference) tables for slow-changing values: genders, blood groups, appointment statuses, vital-sign ranges, clinic profile, users.
- Transaction tables for patients, appointments, visits and their child records, prescriptions and prescription prints, and the audit log.
- Audit columns on every table (see Database Design Plan).
- Soft delete via an active flag on all user-facing records. No hard deletes in Phase 1.
- Append-only audit log: the application's database login has insert and select rights only on it.
- Prescriptions store a snapshot of the header, patient details and medications at each print (legal document requirement).
- Schema changes are versioned SQL migration scripts applied in order, each one recorded in a migrations history table.
- No tenant or clinic identifiers (per Q22).

## Security Architecture

- Transport: HTTPS only. TLS 1.2 minimum, TLS 1.3 preferred (Q15). HSTS and HTTP-to-HTTPS redirection in production. Development uses the ASP.NET Core development certificate.
- Authentication: username and password; access token (JWT, RS256) held in memory by the SPA; refresh token in an HTTP-only, Secure, SameSite cookie with rotation and reuse detection.
- Authorization: policy-based claims; patient ownership check on every data access (single owner, so this is a defense-in-depth check on the clinic's own data).
- Data at rest: TDE and encrypted backups in production (Q16). No column-level encryption in Phase 1 so keyword search stays possible. LocalDB has no TDE, so development data is synthetic only.
- Secrets: never in source control. Development uses per-developer user secrets; production uses the hosting platform's secret store (decided in Phase 6).
- Audit: all data changes in one transactional audit writer.
- Input: FluentValidation on the server; reactive form validation on the client; parameterized SQL only.
- Headers and CORS: security headers (HSTS, Content-Security-Policy, X-Frame-Options, X-Content-Type-Options). CORS allows `http://localhost:4200` only in development.

---

# Module Breakdown

Priority uses MoSCoW: Must, Should, Could. Complexity: Low, Medium, High.

## Module 1: Authentication and Authorization

| Attribute | Detail |
|-----------|--------|
| Purpose | Secure single-user access; enforce policies on every endpoint |
| Features | Login; JWT access token; refresh token rotation; logout (revokes refresh token); lockout after 5 failures in 15 minutes (15-minute lock); 30-minute inactivity timeout; change own password; password policy (minimum 8 characters, complexity); password reset mechanism (see Assumptions) |
| Dependencies | Foundation (Phase 1 S1); Clinic profile is independent. Every other module depends on this one |
| Complexity | High (security-sensitive; token lifecycle; idle timeout across client and server) |
| Priority | Must |
| Phase | 1 |

## Module 2: Dashboard

| Attribute | Detail |
|-----------|--------|
| Purpose | Give the doctor the day's view on arrival |
| Features | Today's appointments in time order; upcoming appointments; recent patients panel (lazy-loaded with `@defer`); quick actions (new patient, new appointment, search) |
| Dependencies | Authentication; Patient Management (recent patients); Appointment Management (schedule data) |
| Complexity | Low |
| Priority | Should (Must for the first-screen experience; can be simplified if the phase runs late) |
| Phase | 3 |

## Module 3: Patient Management

| Attribute | Detail |
|-----------|--------|
| Purpose | Maintain the patient database and quick lookup |
| Features | Add, edit, view patient; soft delete; age from DOB; phone normalization (digits-only search key); duplicate warning on same name plus DOB (user may still save); paged patient list; search by partial name or phone (under 2 seconds at 10,000 patients); recent patients (last 20 per user); blood group and chronic conditions; patient profile with visit summary |
| Dependencies | Authentication; Foundation (data access, audit, paging). Appointments and Medical Records depend on it |
| Complexity | Medium |
| Priority | Must |
| Phase | 2 |

## Module 4: Appointment Management

| Attribute | Detail |
|-----------|--------|
| Purpose | Manage the day's schedule in 15-minute slots |
| Features | Daily and weekly calendar; create appointment (patient search or recent); reschedule (future only); status transitions Scheduled → Completed / Cancelled / No-show; one patient per slot (enforced in the database and the API); past appointments: date and time locked, status set once; appointment notes (optional); appointment history on the patient profile |
| Dependencies | Patient Management (hard: patient must exist); Authentication |
| Complexity | High (slot concurrency, status rules, calendar UI, time handling) |
| Priority | Must |
| Phase | 3 |

## Module 5: Medical Records

| Attribute | Detail |
|-----------|--------|
| Purpose | Capture consultations and make the record available afterwards |
| Features | Start consultation from appointment; vitals (mandatory; warn-and-allow for abnormal, block for impossible); complaints; diagnosis (text and optional code); medications with autocomplete from own history; draft save; complete consultation (single transaction); edit completed visits with change audit and last-modified; prescription generation, print, reprint with per-print snapshot; visit history with date range and keyword search; visit detail view |
| Dependencies | Appointment Management (soft: visits may exist without appointments, but the workflow starts from one); Patient Management (hard); Clinic profile (hard, for prescription header); Authentication; Foundation (audit, transactions) |
| Complexity | High (largest module; transactions, audit of edits, snapshot correctness, print layout, keyword search) |
| Priority | Must |
| Phase | 4 (prescription and history in Phase 4 as well; see Phase Breakdown) |

## Module 6: Reporting

| Attribute | Detail |
|-----------|--------|
| Purpose | Data portability and backup support (BRD data export requirement) |
| Features | Patient list export (CSV); visit data export (CSV and PDF) with date range; prescription export (PDF) from stored snapshot; export to complete in under 10 seconds for typical ranges; large exports handled with paging or background generation |
| Dependencies | Patient Management; Medical Records (visit data and prescription snapshots) |
| Complexity | Medium |
| Priority | Must (BRD requirement) |
| Phase | 5 |

## Module 7: Administration

| Attribute | Detail |
|-----------|--------|
| Purpose | Clinic configuration and account settings |
| Features | Clinic profile (editable text settings, no logo); vital-sign range configuration is seeded, not editable in Phase 1; change own password; read-only audit log viewer (Could; only if Phase 6 has spare capacity) |
| Dependencies | Authentication |
| Complexity | Low |
| Priority | Must (clinic profile, needed for prescriptions); Could (audit viewer) |
| Phase | 1 (clinic profile); audit viewer optional in Phase 6 |

## Module 8: Notifications

| Attribute | Detail |
|-----------|--------|
| Purpose | Tell the doctor what happened: confirmations, errors, warnings |
| Features | Success and error snack bars; confirmation dialogs (for example, delete patient); inline validation messages; warning banners for out-of-range vitals; "try again" on retryable errors. No email, SMS or reminders (out of scope) |
| Dependencies | Foundation (error model); none of the functional modules are blocked by it |
| Complexity | Low |
| Priority | Must (in-app only) |
| Phase | 1 (shared components), extended per phase |

## Module Dependency Graph

```
Foundation (platform, data access, audit, errors)
   └─→ Authentication & Authorization
          ├─→ Administration (clinic profile)  ───────────────┐
          └─→ Patient Management                             │
                 ├─→ Appointment Management                  │
                 │      └─→ Dashboard                        │
                 └─→ Medical Records ←──────────────────────┘
                        └─→ Reporting

Notifications: shared, used by all feature modules from Phase 1
```

---

# User Roles and Permissions

Only the **Doctor** role is built in Phase 1. The other roles are documented so the permission model and schema do not need redesign later. Permissions are stored as data and checked by policy, not by hard-coded role name.

## Doctor (Phase 1)

- **Responsibilities**: consultations, patient records, appointments, prescriptions, history review, data export, own account and clinic profile.
- **Functional access**: all Phase 1 features. Can edit completed visits (Q2), with every change audited.
- **Data access**: all patient data in the clinic (single owner). Cannot access other accounts (single-user). Cannot purge data. Cannot read the audit log in Phase 1 unless the optional audit viewer is delivered.

## Admin (Phase 2)

- **Responsibilities**: manage user accounts and roles, clinic settings, oversee backups, review audit logs.
- **Functional access**: user and role management, clinic profile, audit log (read), trigger exports.
- **Data access**: no clinical data entry. Read access to clinical records only if the clinic's policy allows it.

## Nurse (Phase 2)

- **Responsibilities**: record vitals before consultation; update contact details.
- **Functional access**: read patient profile and today's appointments; create and edit vitals for an open visit.
- **Data access**: no diagnosis, medications, prescriptions or exports. Cannot edit a completed visit.

## Receptionist (Phase 2; out of BRD scope)

- **Responsibilities**: register patients; schedule, reschedule and cancel appointments; mark no-shows.
- **Functional access**: patient demographics (create and edit), full appointment management, patient search.
- **Data access**: minimum necessary. No complaints, diagnosis, medications, prescriptions or visit history.

## Patient (Phase 3 or later; separate BRD needed)

- **Responsibilities**: view own appointments and prescriptions (portal).
- **Functional access**: read-only, own records.
- **Data access**: strictly own data. Requires identity verification and consent handling.

## Role-Permission Summary (target model)

| Capability | Doctor (P1) | Admin | Nurse | Receptionist | Patient |
|------------|-------------|-------|-------|--------------|---------|
| Patient demographics | CRUD | Read | Edit contact | CRUD | Own (read) |
| Appointments | CRUD | Read | Read | CRUD | Own (read) |
| Vitals | CRUD | — | Create/edit (open visit) | — | — |
| Complaints, diagnosis, medications | CRUD | — | — | — | — |
| Prescription generate and print | Yes | — | — | — | Own (read) |
| Visit history | Yes | Policy-based | Limited | — | Own (read) |
| Export (CSV/PDF) | Yes | Yes | — | — | — |
| Clinic profile | Yes | Yes | — | — | — |
| Own password | Yes | Yes | Yes | Yes | Yes |
| Audit log | Read (optional) | Read | — | — | — |

---

# Database Design Plan

## Master Tables

| Table | Purpose | Notes |
|-------|---------|-------|
| Users | Doctor account (and later other accounts) | Username unique; password hash; failed-login count and lock-until time; last login; IsActive |
| Genders | Fixed list (Male, Female, Other) | Seeded |
| BloodGroups | Fixed list (A+, A−, B+, B−, AB+, AB−, O+, O−, Unknown) | Seeded (Q3) |
| AppointmentStatuses | Scheduled, Completed, Cancelled, NoShow | Seeded; transitions enforced in the application |
| VitalRanges | Normal and plausible bounds for temperature, systolic, diastolic, pulse | Seeded; drives warn vs block (Q7) |
| ClinicProfile | Single row of prescription header and footer text settings | Editable by doctor (Q4) |
| Roles and Permissions (or claims table) | Policy data; only Doctor seeded in Phase 1 | Supports Phase 2 without redesign |
| MigrationsHistory | Records applied schema versions | Maintained by the migration runner |

## Transaction Tables

| Table | Purpose | Key relationships |
|-------|---------|-------------------|
| Patients | Patient demographics, blood group, chronic conditions | Gender, BloodGroup |
| RecentPatients | Last 20 patients opened, per user | User, Patient |
| Appointments | 15-minute slot bookings | Patient, AppointmentStatus |
| Visits | One consultation; Status (InProgress, Completed); last modified | Patient, Appointment (optional, 0–1) |
| Vitals | One record per visit (temperature, systolic, diastolic, pulse) | Visit (1:1) |
| Complaints | Free-text complaint entries per visit | Visit (1:N) |
| Diagnoses | Diagnosis text and optional code per visit | Visit (1:N) |
| Medications | One row per medicine per visit | Visit (1:N) |
| Prescriptions | Current prescription for a visit (generated from visit data) | Visit (1:1) |
| PrescriptionPrints | One row per print or reprint with the snapshot used | Prescription (1:N) |
| AuditLogs | Append-only change history | User; table name; record id |

## Relationships

- Users (1) → (N) Patients (created by)
- Patients (1) → (N) Appointments
- Patients (1) → (N) Visits
- Appointments (1) → (0..1) Visits
- Visits (1) → (1) Vitals
- Visits (1) → (N) Complaints, Diagnoses, Medications
- Visits (1) → (1) Prescriptions → (N) PrescriptionPrints
- Users (1) → (N) RecentPatients (N) → (1) Patients
- Users (1) → (N) AuditLogs

## Audit Columns

Every transaction table carries:

| Column | Purpose |
|--------|---------|
| IsActive | Soft-delete flag (default 1) |
| CreatedDate, CreatedBy | Creation time (UTC) and user |
| ModifiedDate, ModifiedBy | Last change time (UTC) and user |
| RowVersion | Optimistic concurrency token |

AuditLogs columns: id, table name, record id, action (Create, Update, Delete), changed fields as old and new JSON values, UTC timestamp, user id. IP address is not required (login and API audit is out of scope, Q17), but the column can be added later without redesign.

Audit scope (Q17): data changes only. Logins, exports, prints and record views are not audited. Failed-login counts stay on the user record for lockout.

## Soft-Delete Strategy

- Patients, Appointments and Visits: soft delete through IsActive; the row and its history remain.
- Child records of a visit (complaints, diagnoses, medications): soft delete as well, so the audit trail keeps old values when a doctor edits a completed visit.
- All read queries filter on IsActive = 1 by default. Admin or audit reads can include inactive rows explicitly.
- No purge jobs in Phase 1 (Q8). Revisit when the legal retention period is confirmed.
- Hard delete only through a future, separately approved retention process.

## Entity Relationship Overview

```
Users ──< Patients >── Genders, BloodGroups
  │          │
  │          ├──< Appointments >── AppointmentStatuses
  │          │         │
  │          │         └──0..1── Visits ──1── Vitals (uses VitalRanges for checks)
  │          │                     ├──< Complaints
  │          └──< Visits          ├──< Diagnoses
  │                                ├──< Medications
  │                                └──1── Prescriptions ──< PrescriptionPrints (snapshot)
  ├──< RecentPatients >── Patients
  └──< AuditLogs
ClinicProfile (single row, used by prescription header)
```

## Index Recommendations

| Table | Index | Reason |
|-------|-------|--------|
| Patients | Clustered on Id | Primary key |
| Patients | Name index on (LastName, FirstName) including Phone, DOB, IsActive | Partial name search and duplicate check |
| Patients | Phone-digits index (normalized search key) | Phone search tolerant of formatting |
| Patients | Filtered index on IsActive = 1 | Default list and search |
| Appointments | Unique index on (AppointmentDate, SlotStart) where IsActive = 1 and status is not Cancelled | One patient per slot, enforced at the database |
| Appointments | Index on (AppointmentDate, SlotStart) including PatientId, Status | Daily and weekly calendar |
| Appointments | Index on PatientId | Patient appointment history |
| Visits | Index on (PatientId, VisitDate) | History list and date-range filter |
| Visits | Index on AppointmentId (filtered, not null) | Link from appointment to visit |
| Complaints, Diagnoses, Medications | Index on VisitId | Visit detail load (single query, no N+1) |
| Medications | Index on MedicineName | Autocomplete from own history |
| Prescriptions | Unique index on VisitId | One current prescription per visit |
| AuditLogs | Index on (TableName, RecordId) and on Timestamp | Record history and date review |
| RecentPatients | Unique index on (UserId, PatientId); index on (UserId, LastViewedAt) | Recent list and upsert |

Keyword search across complaints, diagnosis and medicine text (Q5) cannot use a normal index for contains-style matching. Plan: SQL Server full-text indexes on those text columns, confirmed by a spike in Sprint 1 (LocalDB support must be verified). Fallback: restrict the scan to one patient's visits, which is small enough to meet the 2-second target.

---

# Angular 19 Development Plan

## Folder Structure

```
src/
├── app/
│   ├── core/                       Singleton services, auth, interceptors, guards, models
│   │   ├── auth/                   Auth service, token state, auth guard, refresh logic
│   │   ├── http/                   API base client, error mapping, loading state
│   │   ├── config/                 API base URL and environment values
│   │   └── models/                 API contract interfaces (patients, appointments, visits, ...)
│   ├── shared/                     Standalone presentational pieces (no state)
│   │   ├── components/             Page header, confirm dialog, empty state, error alert, loading indicator, warning banner
│   │   ├── pipes/                  Age from DOB, phone format, date format
│   │   ├── directives/             Autofocus, required-field marker
│   │   └── validators/             Phone, age, date-range, vital-range validators
│   ├── features/
│   │   ├── dashboard/
│   │   ├── patients/               List, detail, form, search, history view
│   │   ├── appointments/           Calendar (day, week), appointment form
│   │   ├── consultation/           Consultation form (vitals, complaints, diagnosis, medications)
│   │   ├── prescription/           Prescription view and print
│   │   ├── history/                Visit history list, visit detail, keyword search
│   │   ├── export/                 Export screens and download handling
│   │   └── settings/               Clinic profile, change password
│   ├── layout/                     Authenticated shell: toolbar, side navigation, content area
│   ├── app.config.ts               Application providers
│   ├── app.routes.ts               Root route table (lazy-loads feature routes)
│   └── app.component.ts
├── environments/                   Development and production API base URLs
└── assets/
    ├── i18n/                       English translation files
    ├── styles/                     Theme tokens, Material theme, global styles
    └── images/
```

Each feature folder contains its own route file, pages, components, a service and a signals store where state is needed.

## Core Module (standalone equivalent)

Provided once in the application configuration:
- Router configuration (`provideRouter`) with lazy routes and preloading strategy for the core features
- HTTP client with interceptors (`provideHttpClient` with the interceptor list)
- Animation support (`provideAnimationsAsync`)
- Global error handler and notification service
- Auth service, current-user state and token handling
- Date and locale configuration (English, date formats)

Services use `providedIn: 'root'`. No `forRoot()` pattern is needed.

## Shared Module (standalone equivalent)

- Only presentational, stateless pieces: components, pipes, directives and validators.
- No shared "barrel" module. Each component imports exactly the pieces and Material components it uses, so unused code is tree-shaken.
- Stateful services stay in `core/` or inside a feature.

## Feature Modules and Lazy Loading

| Feature | Route file | Loading | Notes |
|---------|------------|---------|-------|
| Dashboard | dashboard.routes | Lazy (default after login) | Recent-patients panel in a `@defer` block |
| Patients | patients.routes | Lazy | List, detail, form, history |
| Appointments | appointments.routes | Lazy | Calendar and form |
| Consultation | consultation.routes | Lazy | Heaviest screen; loaded on demand |
| Prescription | prescription.routes | Lazy | Print view isolated from app chrome |
| History | history.routes | Lazy | Visit list, detail, keyword search |
| Export | export.routes | Lazy | Download handling |
| Settings | settings.routes | Lazy | Clinic profile, change password |

The root table lazy-loads each feature route file (`loadChildren`). Individual heavy pages can use `loadComponent`. The authenticated shell is one route that wraps all features and applies the auth guard once.

## Route Guards

| Guard | Type | Purpose |
|-------|------|---------|
| AuthGuard | Activate | Requires a logged-in session; redirects to login and returns to the requested URL after login |
| PermissionGuard | Activate | Checks a required policy claim per route (only Doctor in Phase 1); reused by Phase 2 roles |
| UnsavedChangesGuard | Deactivate | Warns before leaving a consultation or patient form with unsaved changes |

A separate PrefetchGuard is not planned. Guards handle navigation and authorization only. Data loading belongs in route resolvers or in the component's store, not in guards.

## Interceptors

| Interceptor | Responsibility |
|-------------|----------------|
| Auth interceptor | Attaches the access token to API calls; on 401, runs a single refresh (concurrent requests wait for one refresh); on refresh failure, clears state and redirects to login |
| Error interceptor | Maps API errors to user-friendly messages; shows "try again" for retryable failures (network, 5xx); logs technical details to the console in development only; never shows stack traces |
| Loading interceptor | Tracks in-flight requests for the global progress bar; optional, can be merged into the error interceptor if simpler |

## Services

| Service | Responsibilities |
|---------|------------------|
| AuthService | Login, logout, refresh, idle-timeout tracking, current user |
| PatientService | Patient CRUD, search, recent patients, duplicate check, patient visit summary |
| AppointmentService | Calendar for a day or week, create, reschedule, status update |
| ConsultationService | Start visit, save draft, save vitals, complaints, diagnosis and medications, complete visit, edit completed visit, medicine suggestions |
| PrescriptionService | Generate, fetch, print (record print), reprint |
| HistoryService | Visit list with date range and keyword search, visit detail |
| ExportService | CSV and PDF exports; download handling |
| ClinicProfileService | Read and update clinic profile |
| NotificationService | Snack bars and confirmation dialogs |

## State Management Approach

- **Feature stores**: one injectable store per feature (patients, appointments, consultation). Each holds state in signals and exposes read-only and computed signals to components.
- **HTTP**: all calls return RxJS Observables. `toSignal` or `rxResource` bridges results into signals where it simplifies templates.
- **Search**: RxJS `debounceTime`, `distinctUntilChanged` and `switchMap`, so only the latest search result is shown.
- **Subscriptions**: `takeUntilDestroyed` or the `async` pipe only. No manual unsubscribe patterns that can leak.
- **Consultation draft**: saved to the server as the visit is in progress (InProgress status). This protects against data loss, which is stronger than browser storage. Browser storage is used only for per-viewer conveniences (such as remembering the last tab).
- **Revisit trigger**: adopt NgRx SignalStore only if Phase 2 adds concurrent editing by several roles.

## Forms and Validation

- Reactive Forms (FormBuilder) on every data-entry screen. Nested groups for consultation sections (vitals, complaints, diagnosis, medications).
- Client validators mirror server rules: required, length limits (complaint 2000 characters), phone pattern, age range (not negative, not over 120), appointment not in the past, vital ranges.
- Vitals: values outside the normal range show a warning banner and still save. Values outside the plausible range block the save with an inline error.
- Submit disabled until the form is valid; inline messages under each field; focus moves to the first invalid field.
- Duplicate patient check on name plus DOB shows a warning with a link to the existing patient; the doctor can still save.

---

# ASP.NET Core Web API Plan

## Clean Architecture Layers

| Layer | Contains | Rules |
|-------|----------|-------|
| Presentation (API) | Controllers, request and response DTOs, middleware, filters, Swagger setup, authentication setup | Thin controllers; no SQL; maps to application use cases |
| Application | Command and query handlers (CQRS-lite folders), application services, FluentValidation validators, DTO mapping, interfaces for repositories, unit of work, clock, current user, token service | Orchestrates use cases; owns transaction boundaries through the unit of work |
| Domain | Entities (Patient, Appointment, Visit, Vitals, Complaint, Diagnosis, Medication, Prescription), value objects (PhoneNumber, BloodPressure), domain exceptions, business rules (age, slot rules, status transitions, vital ranges) | No framework or data-access references |
| Infrastructure | Dapper repositories, connection factory, unit of work implementation, migration runner, JWT signing and validation, password hashing, audit writer, PDF and CSV generation | Implements interfaces from Application and Domain |

## Solution Structure

```
PatientManagement.sln
├── API/                    Presentation: Controllers, DTOs, Middleware, Filters, Program
├── Application/            Commands/, Queries/, Validators/, Services/, Interfaces/, Mapping/
├── Domain/                 Entities/, ValueObjects/, Rules/, Exceptions/, Interfaces/ (repository contracts)
├── Infrastructure/         Persistence/ (connection factory, unit of work), Repositories/,
│                           Migrations/ (versioned SQL), Security/, Auditing/, Export/
└── Tests/
    ├── Domain.UnitTests/
    ├── Application.UnitTests/
    ├── Infrastructure.IntegrationTests/   (against a dedicated test database)
    └── Api.IntegrationTests/              (in-process host, real database)
```

Project names follow the BRD's architecture. Folder names may be adjusted by the tech lead in Sprint 1 without changing the layering rules.

## API Modules and Controllers

| Controller (API module) | Resource | Phase |
|-------------------------|----------|-------|
| AuthController | Login, refresh, logout, change password | 1 |
| ClinicProfileController | Clinic profile read and update | 1 |
| PatientsController | Patients CRUD, search, duplicate check, visit summary | 2 |
| RecentPatientsController | Recent patients list (or part of PatientsController) | 2 |
| AppointmentsController | Calendar (day and week), create, reschedule, status | 3 |
| DashboardController | Today and upcoming summary | 3 |
| VisitsController | Start visit, draft save, vitals, complaints, diagnosis, medications, complete, edit completed | 4 |
| MedicineSuggestionsController | Autocomplete from own history | 4 |
| PrescriptionsController | Generate, fetch, record print, reprint | 4 |
| HistoryController | Visit list (date range, keyword), visit detail | 4 |
| ExportController | Patient list CSV, visit CSV and PDF, prescription PDF | 5 |
| HealthController | Health check endpoint | 1 |

Endpoints follow REST conventions. Action names are avoided in URLs except for explicit state changes (for example, `complete`). Pagination parameters are standard for all list endpoints.

## Services and Use Cases

Use cases are grouped by feature in the Application layer. Each command or query has one handler:

- Authentication: login, refresh token, logout, change password
- Patients: create patient, update patient, deactivate patient, get patient, search patients, check duplicate, list recent patients
- Appointments: create appointment, reschedule appointment, change status, get day schedule, get week schedule
- Visits: start visit, save draft, record vitals, record complaints, record diagnosis, record medications, complete visit, edit completed visit
- Prescriptions: generate prescription, record print
- History: list visits, get visit detail, keyword search
- Exports: export patient list, export visit data, export prescription
- Clinic profile: get profile, update profile

Business rules live in the Domain (for example, slot rules and status transitions); handlers coordinate them.

## Repositories and Unit of Work

- One repository per aggregate root (patients, appointments, visits, prescriptions, users). Child records (vitals, complaints, diagnoses, medications) are written through the visit repository.
- Repository interfaces sit in the Domain layer; implementations sit in Infrastructure.
- The Unit of Work provides begin, commit and rollback, and exposes the repositories that share one connection and transaction. It is registered per request.
- Read-only use cases use the repositories directly without an explicit transaction.

## DTOs and Validators

- Request DTOs and response DTOs are separate from domain entities and from each other. Response DTOs never expose password hashes, refresh token values or internal audit fields.
- One FluentValidation validator per request DTO. Validators cover format and length; domain rules cover state and business meaning.
- Paged responses use one shape: items, total count, page number, page size.
- Vital-sign validation uses the VitalRanges master data: normal range produces a warning in the response; plausible-range violation produces a validation error.

## Middleware

| Middleware | Responsibility |
|------------|----------------|
| Exception handling | Maps domain, validation and unexpected exceptions to a standard error response; hides internal details; logs the full exception server-side |
| Correlation ID | Adds a request identifier to logs and responses |
| Request logging | Logs method, route, status, duration; no request bodies with patient data |
| Security headers | HSTS (production), Content-Security-Policy, X-Frame-Options, X-Content-Type-Options |
| Rate limiting | Login: tight limit per IP and per username (lockout rules apply on top); general API: per-user limit (100 requests per minute, from the brainstorm) |
| Authentication and authorization | Bearer token validation; policy checks |
| CORS | Development origin only (`http://localhost:4200`) |

## Cross-Cutting Services

- **Clock**: injectable UTC clock. Tests use a fixed clock. No direct system time calls in handlers.
- **Current user**: read from JWT claims through an abstraction. Used to set CreatedBy and ModifiedBy.
- **Audit writer**: called by the unit of work so the audit row commits with the data change.
- **Logging**: structured logs; levels Error, Warning, Information, Debug; no patient-identifying values in logs.
- **Health checks**: database connectivity and application status.

---

# Dapper Strategy

## Query Organization

- SQL lives in repository classes as named constants, grouped per aggregate. For large or complex queries, a per-aggregate query class holds them.
- Column lists are explicit. `SELECT *` is not allowed.
- All values are parameters. No string concatenation of input. Dynamic filters are built from a fixed set of allowed clauses, each with parameters.
- Every read query applies the soft-delete filter (IsActive = 1) unless it is an explicit admin or audit read.
- Async methods throughout.
- Comments explain non-obvious queries (paging, keyword search, slot-conflict checks).

## Repository Implementation

- A generic base repository covers common operations (get by id, insert, update with row version check, soft delete). Entity repositories extend it and add their own queries.
- Repositories return domain entities or read models. Mapping to DTOs happens in the Application layer.
- Multi-row reads that need child records use one query with a join or multiple result sets (`QueryMultiple`), never one query per row.
- Concurrency: update statements include the row version. Zero rows affected means a conflict, which maps to a standard conflict response.
- Value objects (BloodPressure, PhoneNumber) use type handlers registered only for those types, not whole entities.
- The connection comes from a connection factory. Services never construct connections.

## Transaction Management

Transactions are required for:
- Completing a consultation: visit status, vitals, complaints, diagnoses, medications, appointment status and audit rows.
- Editing a completed visit: child rows and audit rows together.
- Creating a patient with its audit row.
- Rescheduling an appointment: old slot released and new slot taken together, protected by the unique slot index.
- Generating a prescription and recording a print with its snapshot.

Read-only queries do not use a transaction. Transactions use a sensible isolation level (read committed; snapshot isolation evaluated in Sprint 11 if needed). Rollback runs on any exception.

## Stored Procedure Strategy

- **Phase 1 default**: parameterized inline queries in repositories. Easier to version, test and review alongside C# code.
- **Candidates for stored procedures later, only if profiling shows a need**: visit-history aggregation for large histories, large export datasets, and any reporting query used in a loop.
- Stored procedures, if added, are versioned with the migrations and covered by integration tests.
- No business rules move into the database in Phase 1 beyond integrity constraints (unique slot, foreign keys, check constraints for ranges and statuses).

## Performance Optimization

- Indexes as listed in the Database Design Plan. Index review at the end of each phase using execution plans.
- Paging on every list (server-side OFFSET and FETCH, deterministic ORDER BY, page size capped at 100).
- Patient search uses the indexed name and phone columns; typed input is debounced on the client.
- Visit history loads the visit list first; visit detail loads child records in one query per child group, not per row.
- Medicine autocomplete uses a distinct query over the indexed medicine name column with a result limit (for example, 10).
- Command timeouts set explicitly (default 30 seconds); slow queries (over 500 ms) logged in development.
- Exports stream rows instead of loading the whole dataset into memory. Large exports run asynchronously with progress if they exceed a threshold.
- Load test in Sprint 11 against synthetic data at 10,000 patients and 100,000 visits to confirm targets.

---

# Security Plan

## JWT Authentication

- RS256 asymmetric signing. The private key stays on the server (per-developer key in development; secret store in production).
- Access token lifetime: 15 minutes. This is shorter than the brainstorm's 1 hour, so a stolen token expires sooner. The 30-minute inactivity timeout still governs the session.
- Claims: user id, username, roles, permissions (policy claims), token id.
- Token validation: issuer, audience, signing key, lifetime, no clock skew beyond 1 minute.
- The SPA keeps the access token in memory only. It is not stored in local or session storage (this refines the brainstorm's local-storage approach to reduce exposure to script injection).

## Refresh Tokens

- Refresh token: 8-hour absolute lifetime with idle tracking. The server rejects a refresh if the token has been idle for more than 30 minutes (matches the inactivity requirement).
- Delivered in an HTTP-only, Secure, SameSite=Strict cookie scoped to the refresh endpoint path. Only its hash is stored in the database.
- Rotation: each refresh issues a new refresh token and invalidates the old one.
- Reuse detection: presenting an already-rotated refresh token revokes the whole token family and forces login.
- Logout revokes the current refresh token and clears the cookie.
- Password change revokes all refresh tokens for the user.
- The client refreshes the access token shortly before expiry and on a 401 response; concurrent requests share one refresh.

## Role-Based Access Control

- Roles and permissions are data (Roles, Permissions, RolePermissions, UserRoles tables or claims in the user record).
- Phase 1 seeds one role (Doctor) with all clinical and administrative permissions.
- Endpoints use policies (for example, "CanRecordDiagnosis", "CanExportData"), never raw role names.
- Front-end guards mirror the server policies for navigation only. The server is the authority.
- Patient ownership is checked on every patient-scoped call, even though the clinic has one owner, so later role changes do not open access by accident.

## Password Security

- Hashing: bcrypt with a work factor tuned on the production hardware (cost chosen in Sprint 2 and recorded in configuration). Salt is generated per password by the library.
- Policy: minimum 8 characters, at least one upper-case letter, one lower-case letter, one digit and one symbol (complexity required, per BRD).
- No password is ever logged, returned or stored in plain text.
- Lockout: 5 failed attempts within 15 minutes locks the account for 15 minutes. The failure count and lock time are stored on the user record.
- Login errors are generic ("invalid username or password"); the response does not reveal whether the username exists.
- Password reset: no email service exists in Phase 1, so reset is an operator action on the server (a documented command that sets a new password and revokes sessions). This is a planning assumption to confirm in Sprint 2.

## API Security

- HTTPS only. HTTP is refused in production; HSTS enabled.
- CORS limited to the development origin in development and the production site origin in production.
- Rate limiting: login endpoint tightly limited; general API limited per user (100 requests per minute).
- Security headers on all responses.
- Error responses are generic in production; stack traces never returned.
- Swagger UI enabled in development only; disabled in production.
- Request size limits on text fields (for example, complaint text 2000 characters) enforced server side.
- Dependency vulnerability scanning run locally (NuGet and npm audit) at least weekly and before each release candidate; critical and high findings must be fixed before release.

## Validation Strategy

- Server: FluentValidation for every request DTO; domain rules in entities and value objects; database constraints as the last line of defense (unique slot, foreign keys, check constraints for status and ranges).
- Client: reactive form validators mirror server rules; server errors mapped back to fields.
- Output: Angular's default template escaping; no `innerHTML` with user content; print and PDF output escape all text.
- SQL: parameterized queries only; code review checklist includes a check for string-built SQL.

## Data Protection and Secrets

- In transit: TLS 1.2 minimum, 1.3 preferred (Q15).
- At rest (production): TDE on the database and encrypted backups (Q16). Development on LocalDB has no TDE, so only synthetic data is used there.
- Secrets: development uses per-developer user secrets; production uses the hosting platform's secret store (Azure Key Vault if App Service is chosen; protected configuration if IIS is chosen). Decided in Sprint 11.
- Database access: separate logins for migrations (schema changes, used only at deploy) and for the application (data read and write only, no schema changes). Audit table grants insert and select only.
- Backups: encrypted; not yet decided (Q18 blocker; see Risks).
- Logs: no patient names, phone numbers, diagnoses or medicine names in logs.

---

# Development Phases

Effort is in developer-days (dev-days) for the 3-developer team, base estimate, including design, development, unit and integration tests, code review and fixes. Testing is included in each phase, not deferred to Phase 6.

## Phase 1: Foundation, Authentication and User Management (Sprints 1–2; Weeks 1–4)

**Deliverables**
- Solution with four layers and test projects; Angular workspace with authenticated shell
- LocalDB database with versioned migrations; migration history
- Logging, error model, health endpoint, Swagger, local build and test scripts (no CI pipeline in Phase 1)
- Audit writer and unit of work (with a passing integration test)
- Login, refresh token rotation, logout, lockout, 30-minute idle timeout, change own password, password reset operator command
- Clinic profile settings (used later by prescriptions)
- Shared notification components and error interceptor
- Spike: full-text search feasibility on LocalDB (Sprint 1)

**Dependencies**: none (start). Everything else depends on this phase.

**Estimated effort**: 50 dev-days (Sprint 1: 24; Sprint 2: 26).

**Exit criteria**: a doctor can sign in, sign out, change password, and edit the clinic profile; idle and lockout rules verified by tests; one audited change verified end-to-end.

## Phase 2: Patient Management (Sprints 3–4; Weeks 5–8)

**Deliverables**
- Patient CRUD with validation, age from DOB, phone normalization, soft delete
- Duplicate warning on name plus DOB
- Paged patient list; search by partial name and by phone (under 2 seconds at 10,000 patients)
- Recent patients (last 20) and patient profile page with blood group and chronic conditions
- Patient visit summary placeholder (filled in Phase 3 and 4)

**Dependencies**: Phase 1 (authentication, audit, paging, error model). Hard dependency on Foundation; Phase 3 and 4 depend on this phase.

**Estimated effort**: 40 dev-days.

**Exit criteria**: all patient functions covered by unit and integration tests; search meets the time target on the synthetic 10,000-patient set; doctor demo accepted.

## Phase 3: Appointment Management and Dashboard (Sprints 5–6; Weeks 9–12)

**Deliverables**
- Appointment model with fixed 15-minute slots and unique slot enforcement
- Daily and weekly calendar; create; reschedule (future only); status transitions
- Past-appointment rules (date and time locked; status set once)
- Dashboard: today's and upcoming appointments, recent patients, quick actions
- Appointment history on the patient profile

**Dependencies**: Phase 2 (hard: patients must exist). Dashboard depends on the recent-patients feature from Phase 2.

**Estimated effort**: 40 dev-days.

**Exit criteria**: calendar loads within 2 seconds on the synthetic dataset; double-booking tests pass at the database level; status rules covered by unit tests; doctor accepts the calendar layout.

## Phase 4: Medical Records (Sprints 7–9; Weeks 13–18)

**Deliverables**
- Start consultation from appointment; visit lifecycle (InProgress, Completed)
- Vitals with normal-range warnings and plausible-range blocking (configuration seeded from VitalRanges)
- Complaints, diagnosis (with optional code), medications with autocomplete from own history
- Draft save; complete consultation in one transaction; unsaved-changes protection
- Edit completed visits with field-level audit (old and new) and last-modified display
- Prescription generated from visit data; print view; print records a snapshot; reprint uses the stored snapshot
- Visit history: chronological list, date-range filter, keyword search within one patient, visit detail

**Dependencies**: Phase 3 (hard: consultation starts from an appointment), Phase 2 (hard: patient), Phase 1 clinic profile (hard: prescription header), Phase 1 audit (hard).

**Estimated effort**: 65 dev-days. This is the largest phase and sits on the critical path.

**Exit criteria**: end-to-end consultation (appointment to completed visit to printed prescription) runs on the synthetic dataset; consultation completion measured in UAT rehearsal at or under 3 minutes; prescription snapshot tests pass (reprint matches the original print); keyword search meets its target.

## Phase 5: Reporting (Sprint 10; Weeks 19–20)

**Deliverables**
- Patient list CSV export
- Visit data CSV and PDF export with date range
- Prescription PDF export from stored snapshot
- Progress indication and asynchronous generation for large exports
- Export formatting checks (standard CSV, readable PDF)

**Dependencies**: Phase 4 (hard: visit and prescription data), Phase 2 (hard: patient list).

**Estimated effort**: 20 dev-days.

**Exit criteria**: exports complete within 10 seconds for typical ranges; CSV opens cleanly in a spreadsheet tool; PDF layout approved by the doctor; feature freeze at the end of this sprint.

## Phase 6: Hardening and Optimization (Sprint 11; Weeks 21–22)

**Deliverables**
- Load test results (search, history, export) against 10,000 patients and 100,000 visits
- Performance tuning from execution plans; index review
- Security review: OWASP-based checklist, OWASP ZAP baseline scan, dependency scan, token and cookie tests
- Accessibility pass with automated checks (WCAG 2.1 AA best effort)
- Backup and recovery decision implemented and restore test passed (Q18 blocker for production)
- Hosting decision for production (IIS or App Service) and deployment package; secret store configured
- Runbook, release notes draft, UAT pack with synthetic data and seeded demo dataset
- Release candidate

**Dependencies**: Phases 1–5 (all features complete). The backup decision has no code dependency but blocks go-live.

**Estimated effort**: 25 dev-days.

**Exit criteria**: no open critical or high security findings; load targets met or accepted with a documented exception; restore test passed; release candidate deployed to the staging environment.

## UAT and Go-Live (Sprint 12; Weeks 23–24)

- Doctor UAT for 2 weeks on the release candidate, synthetic data only (Q20)
- Daily triage of UAT findings; fix cycle within the sprint
- Go-live checklist: backup in place (blocker), TDE and TLS verified, deployment rehearsed, rollback plan tested
- Doctor sign-off on UAT and go-live

**Estimated effort**: 10 dev-days of fix capacity, plus doctor time.

## Hypercare (4 weeks after go-live)

- Development team on call for 1 month (Q21); daily issue review in the first week, then twice weekly
- Measure success criteria (consultation time, search time, paper usage survey)
- Handover to ongoing support on request

---

# Effort and Capacity Summary

Sprint capacity: 2 weeks × 3 developers × 10 working days = 30 dev-days, minus 15% for ceremonies, leave and support, so about 25 productive dev-days per sprint.

| Phase | Sprints | Weeks | Dev-days (base) |
|-------|---------|-------|-----------------|
| 1. Foundation, Auth, User Management | S1–S2 | 1–4 | 50 |
| 2. Patient Management | S3–S4 | 5–8 | 40 |
| 3. Appointments and Dashboard | S5–S6 | 9–12 | 40 |
| 4. Medical Records | S7–S9 | 13–18 | 65 |
| 5. Reporting | S10 | 19–20 | 20 |
| 6. Hardening and Optimization | S11 | 21–22 | 25 |
| **Build total** | **S1–S11** | **1–22** | **240** |
| UAT and fixes | S12 | 23–24 | 10 (fix capacity) |
| Contingency (reserved) | S13 | 25–26 | Used only if triggered |
| Hypercare | 4 weeks after go-live | — | Support, not build |

Range: ±20% on build (192–288 dev-days), which moves the base go-live between Week 22 and Week 29. The contingency sprint covers most of the upper range.

---

# Sprint Planning

Two-week sprints. Each sprint ends with a demo to the doctor (product owner) and a retrospective. Sprint numbers are relative to kickoff.

| Sprint | Weeks | Focus | Main scope | Exit gate |
|--------|-------|-------|------------|-----------|
| S1 | 1–2 | Foundation | Solution layers, test projects, Angular shell, LocalDB and migrations, logging, errors, Swagger, health, local build and test scripts; full-text feasibility spike; password hashing spike | Empty app runs end to end; local build and tests pass |
| S2 | 3–4 | Auth and account | Login, JWT and refresh rotation, lockout, idle timeout, password change, reset command, audit writer, clinic profile | **M1**: sign-in and audited profile change work |
| S3 | 5–6 | Patients (1) | Patient model, create, edit, view, validation, soft delete, phone key, duplicate warning, paged list | Patient CRUD tests green |
| S4 | 7–8 | Patients (2) | Name and phone search, recent patients, profile page, blood group and chronic conditions, search performance | **M2**: patient management complete |
| S5 | 9–10 | Appointments (1) | Appointment model, 15-minute slots, unique slot index, day and week calendar, create | Double-booking tests pass |
| S6 | 11–12 | Appointments (2) and dashboard | Reschedule (future only), status transitions, past-appointment rules, dashboard, patient appointment history | **M3**: schedule and dashboard complete |
| S7 | 13–14 | Consultation (1) | Start visit from appointment, visit lifecycle, vitals with warn and block, complaints, diagnosis, draft save | Vitals rules verified against the agreed ranges |
| S8 | 15–16 | Consultation (2) | Medications and autocomplete, complete-visit transaction, unsaved-changes guard, edit completed visit with field audit | **M4**: consultation capture complete |
| S9 | 17–18 | Prescription and history | Prescription generation, print and reprint with snapshot, print records, visit history with date range and keyword search, visit detail | **M5**: prescription and history complete |
| S10 | 19–20 | Reporting | CSV patient list, visit CSV and PDF, prescription PDF, large-export handling | **M6**: feature freeze; exports meet time target |
| S11 | 21–22 | Hardening | Load tests, tuning, security review and scans, accessibility pass, backup decision and restore test, hosting decision, deployment package, runbook | **M7**: release candidate on staging |
| S12 | 23–24 | UAT | Doctor UAT (2 weeks), fix cycle, go-live checklist | **M8/M9**: UAT signed off; go-live readiness |
| S13 | 25–26 | Contingency | Reserved; used only if UAT or Phase 6 slips | Go-live (**M10**) |

Notes:
- The UAT sprint runs the doctor's sessions in parallel with fixes; the doctor also reviews each sprint demo from S3 onward so UAT is not a surprise.
- Phase 1 work that does not gate M1 (for example, extra local tooling) can move into S3 without affecting the critical path.

---

# Milestones and Gates

| # | Milestone | Target (week) | Deliverables | Definition of Done | Gate / approver | Status |
|---|-----------|---------------|--------------|--------------------|-----------------|--------|
| M0 | Kickoff and environment ready | Week 0 | Team, environments, backlog, risk log | LocalDB and Angular running locally; backlog estimated | Product owner | Planned |
| M1 | Foundation and authentication | End of Week 4 | Phase 1 deliverables | Sign-in, sign-out, lockout, idle timeout, audited profile change, local build and tests green | Product owner (demo) | Planned |
| M2 | Patient management complete | End of Week 8 | Phase 2 deliverables | Patient features and search time target met | Product owner | Planned |
| M3 | Appointments and dashboard complete | End of Week 12 | Phase 3 deliverables | Double-booking prevented; dashboard accepted | Product owner | Planned |
| M4 | Consultation capture complete | End of Week 16 | Phase 4 part 1 | Vitals rules; complete-visit transaction; edit audit | Product owner and tech lead | Planned |
| M5 | Prescription and history complete | End of Week 18 | Phase 4 part 2 | Prescription snapshot and reprint verified; history search works | Product owner (doctor review of print layout) | Planned |
| M6 | Feature freeze | End of Week 20 | Phase 5 deliverables | No new scope after this gate; exports meet time target | Product owner | Planned |
| M7 | Release candidate | End of Week 22 | Phase 6 deliverables | No open critical or high security findings; load targets met or excepted | Tech lead; security review sign-off | Planned |
| M8 | UAT start | Week 23 | Release candidate on staging; UAT pack | Synthetic data loaded; doctor access working | Product owner | Planned |
| M9 | UAT sign-off and go-live readiness | End of Week 24 | UAT report; go-live checklist | Backup in place and restore tested (Q18); TDE and TLS verified; rollback rehearsed | Doctor (UAT); product owner (readiness) | Planned |
| M10 | Go-live | End of Week 24 (Week 26 if contingency used) | Production deployment | Deployment successful; smoke tests passed | Product owner | Planned |
| M11 | Hypercare exit | 4 weeks after M10 | Hypercare report | Success criteria measured; support handover | Product owner | Planned |

Traffic-light status is set at each gate using the actual progress against these targets. All milestones are "Planned" at baseline.

---

# Dependency Matrix and Critical Path

## Task-Level Dependencies

| Task | Depends on | Type | Reason |
|------|-----------|------|--------|
| Authentication | Foundation | Hard | Needs API host, database, audit writer |
| Clinic profile | Foundation, Authentication | Hard | Protected settings; prescription header depends on it |
| Patient CRUD | Authentication | Hard | All patient endpoints are authenticated |
| Patient search and recent patients | Patient CRUD | Hard | Uses patient data and indexes |
| Appointments (calendar, create) | Patient CRUD | Hard | Appointments belong to patients |
| Appointments: reschedule and status | Appointments (calendar) | Hard | Uses slot model |
| Dashboard | Appointments, Recent patients | Data | Reads today's schedule and recent list |
| Consultation: start visit | Appointments (status), Patient | Hard | Starts from an appointment |
| Consultation: vitals, complaints, diagnosis | Start visit | Hard | Child records of a visit |
| Medication autocomplete | Medications (saved data) | Data | Suggestions come from history; works with empty history |
| Complete visit | Vitals, complaints, diagnosis, medications | Hard | Completion requires mandatory vitals and the transaction |
| Edit completed visit | Complete visit, audit | Hard | Needs baseline data and audit writer |
| Prescription generation | Complete visit, Clinic profile | Hard | Snapshot built from visit and profile |
| Prescription print and reprint | Prescription generation | Hard | Print records use the snapshot |
| Visit history and keyword search | Complete visit; full-text spike result | Data (search) | Search depends on the full-text decision |
| Exports (patient CSV) | Patient CRUD | Hard | Export data |
| Exports (visit CSV/PDF, prescription PDF) | Visit history, Prescription generation | Hard | Export data and snapshots |
| Hardening (load, security, a11y) | All features | Hard | Measures completed functionality |
| Backup and recovery implementation | Hosting decision (Phase 6) | Hard | Backups depend on hosting platform |
| UAT | Release candidate | Hard | Doctor tests a stable build |
| Frontend screens | Backend contract (OpenAPI) for that feature | API | Contract-first; front end can start once contract agreed |

## Critical Path

The longest dependency chain, which sets the minimum duration:

Foundation (S1) → Authentication (S2) → Patient CRUD (S3) → Patient search (S4) → Appointment calendar (S5) → Appointment status and dashboard (S6) → Start visit and vitals (S7) → Medications and complete-visit transaction (S8) → Prescription snapshot and history (S9) → Exports (S10) → Hardening (S11) → UAT (S12)

Any slip on the critical path moves go-live day for day. The contingency sprint is the only float.

## Parallel Work

- Clinic profile UI can run alongside Authentication in S2.
- Patient list and form UI can start once the patient contract is agreed in S3, in parallel with the back-end handlers.
- Export back-end handlers for patient CSV can be built in S8 (during consultation work) to reduce S10 load.
- Medicine autocomplete endpoint and UI can run in parallel with the complete-visit transaction in S8.
- Load-test scripts and seed data generators can be written in S9 while the prescription work finishes.

## Team Specialization Constraints

- The database and transaction design (audit, snapshot, unique slot) is owned by the tech lead; it is on the critical path and needs review before S5.
- Security work (token lifecycle, cookie handling, lockout) is paired by the back-end and full-stack developers in S2.

---

# Risks and Mitigation

Impact and likelihood: High, Medium, Low.

## Timeline and Delivery Risks

| ID | Risk | Category | Impact / Likelihood | Mitigation | Contingency | Owner |
|----|------|----------|---------------------|------------|-------------|-------|
| T1 | Team is smaller than assumed (2 developers instead of 3) | Resource | High / Medium | Confirm team at M0; reassess at M1; drop Phase 6 optional items first | Extend schedule by about 1.5× or cut Could items | Product owner, tech lead |
| T2 | Doctor not available for weekly demos or 2-week UAT | Delivery | High / Medium | Book UAT sessions at M0; invite doctor to every demo from S3 | Use contingency sprint S13 | Product owner |
| T3 | Scope creep (receptionist, reminders, logo, multi-user) | Delivery | High / High | Written scope; change control; Phase 2 backlog; policy-based design only | Defer to Phase 2 | Product owner |
| T4 | Estimates off by more than 20% | Delivery | Medium / Medium | Re-estimate at each sprint start; track velocity from S3 | Use contingency sprint | Tech lead |
| T5 | Consultation module (largest, critical path) overruns | Delivery | High / Medium | Split into two sprints; review design before S7; start early spikes | Move Reporting work into S11 if needed; trim Could items | Tech lead |
| T6 | Vital-sign ranges not confirmed by the doctor in time for S7 | Clinical / Delivery | Medium / Medium | Proposed ranges (below) confirmed at start of S7; stored as seeded data so changes need no code change | Use proposed ranges and confirm at UAT | Product owner |

Proposed vital-sign ranges for confirmation (from the BRD and brainstorm): temperature normal 35–42 °C, plausible 30–45 °C; systolic normal 90–140 mmHg, plausible 50–260; diastolic normal 60–90 mmHg, plausible 30–160; pulse normal 40–200 BPM, plausible 20–250.

## Technical Risks

| ID | Risk | Category | Impact / Likelihood | Mitigation | Contingency | Owner |
|----|------|----------|---------------------|------------|-------------|-------|
| K1 | LocalDB differs from production SQL Server (feature gaps such as full-text or versions) | Technical / Database | Medium / Medium | Spike in S1; use only features in both; run integration tests against a SQL Server edition close to production in S11 | Adjust queries or features; document the difference | Tech lead |
| K2 | Keyword search over long text is slow or unavailable | Performance | Medium / Medium | Full-text spike in S1; fallback to scan limited to one patient's visits | Reduce keyword scope (for example, medicine names and diagnosis only) | Tech lead |
| K3 | Angular 19 leaves active support during the build | Technical / Security | Medium / High | Check Angular's support calendar before S1; decide with product owner whether to hold 19 (as requested) or move to the current supported major before M1; keep dependencies scanned | Upgrade in Phase 6 with regression tests | Tech lead |
| K4 | Editable visits with full field audit make data and audit tables large or complex | Technical | Medium / High | Audit only changed fields as old and new values; index audit table; test with 100,000 visits | Summarize older audit entries (later phase) | Tech lead |
| K5 | Dapper mapping or SQL errors (wrong column, null handling) | Technical | Medium / Medium | Integration test per repository; explicit column lists; code review checklist | Fix and add regression test | Developers |
| K6 | Performance target missed with 100,000 visits | Performance | Medium / Low | Indexes from design; execution-plan review each phase; load test in S11 | Keyset paging for history; targeted caching | Tech lead |
| K7 | Calendar time handling (time zones, daylight saving, slot boundaries) | Technical | Medium / Medium | Store UTC; render in clinic time zone; tests for DST boundaries; slot rules in Domain | Constrain slots to a fixed local time zone | Developers |

## Security Risks

| ID | Risk | Category | Impact / Likelihood | Mitigation | Contingency | Owner |
|----|------|----------|---------------------|------------|-------------|-------|
| S1 | Real patient data placed in development LocalDB (no TDE, no production controls) | Security / Compliance | High / Medium | Synthetic data only; seeding tool; rule in onboarding; checks in review | Purge data, rotate credentials, record incident | Tech lead |
| S2 | Token theft or refresh-token misuse | Security | High / Medium | Access token in memory; refresh cookie HTTP-only and rotated with reuse detection; security tests | Revoke token families; force password change | Developers, tech lead |
| S3 | Password reset with no email service is weak or unavailable | Security / Delivery | Medium / High | Operator reset command with audit and session revocation, decided in S2 | Manual reset procedure documented | Tech lead |
| S4 | Vulnerable third-party packages | Security | Medium / Medium | Dependency scans run locally at least weekly and before each release candidate; critical and high findings must be fixed before release; monthly update window | Patch or pin; emergency release | Tech lead |
| S5 | Injection, XSS or insecure output (including print and PDF) | Security | High / Low | Parameterized SQL; validation; output escaping; ZAP scan in S11 | Hotfix release | Developers |
| S6 | Compliance (HIPAA or local rules) not verified before production | Compliance | Medium / Medium | Q8 (keep indefinitely) and TDE and TLS decided; compliance check with clinic before M9 | Limit production use until checked | Product owner |

## Database and Data Integrity Risks

| ID | Risk | Category | Impact / Likelihood | Mitigation | Contingency | Owner |
|----|------|----------|---------------------|------------|-------------|-------|
| D1 | No backup plan (Q18): data loss or inability to roll back a failed migration | Database / Reliability | High / High (production) | Backup decision and restore test are a go-live blocker (M9); plan a daily full backup and transaction log backups in S11 as the default, subject to the product owner's confirmation; migration rollback relies on a pre-deployment backup | Do not go live; keep the system in pilot use with synthetic data until a backup is in place | Product owner, tech lead |
| D2 | Prescription snapshot incorrect or reprint differs from the original | Clinical / Compliance | High / Medium | Snapshot tests; golden-file print tests; reprint test in S9 | Fix and reissue; doctor checks printed output | Developers |
| D3 | Migration script error on production data | Database | High / Low | Migrations tested on a copy of synthetic data; deployment rehearsal; backup before deploy | Restore from backup (requires D1 fixed) | Tech lead |
| D4 | Soft-delete leaks inactive records into searches | Data | Medium / Medium | Filter in every read query; integration tests for each search | Fix query; audit data check | Developers |
| D5 | Unique slot constraint conflicts during reschedule | Data | Medium / Medium | Single transaction for reschedule; tests for concurrent booking | Show "slot taken" message and reload | Developers |

## Performance Risks

| ID | Risk | Category | Impact / Likelihood | Mitigation | Contingency | Owner |
|----|------|----------|---------------------|------------|-------------|-------|
| P1 | Patient search or history loading exceeds targets | Performance | Medium / Low | Indexes; paging; debounce; load test in S11 | Keyset paging; caching of recent patients | Tech lead |
| P2 | Large CSV or PDF export causes timeouts | Performance | Medium / Low | Streaming; paging; async for large exports | Limit ranges; split files | Developers |
| P3 | Print dialog slower than 5 seconds | Performance | Low / Medium | Lightweight print view; no external fonts or images in print layout | Simplify layout | Developers |

## Operational and Deployment Risks

| ID | Risk | Category | Impact / Likelihood | Mitigation | Contingency | Owner |
|----|------|----------|---------------------|------------|-------------|-------|
| O1 | Hosting decision (IIS or App Service) made late | Deployment | Medium / Medium | Decide in S11; keep deployment package host-neutral | Stay on IIS if App Service cannot be approved | Tech lead |
| O2 | Availability target (99%) not achievable on a single machine | Operations | Medium / Medium | Hosting choice matches target; health checks; monitored; downtime plan | Agree realistic target with product owner | Product owner |
| O3 | Production secrets handled unsafely | Security / Operations | High / Low | Secret store only; no secrets in source; review at M7 | Rotate secrets | Tech lead |

---

# Testing Strategy

## Unit Testing

- **Scope**: domain rules (age, slot rules, status transitions, vital ranges, password policy), application handlers with fake repositories and a fixed clock, validators, Angular stores, guards, interceptors and pipes.
- **Targets**: 70% overall; 90% on consultation, prescription and authentication code (BRD and brainstorm).
- **Tools (planned)**: xUnit for .NET with a mocking library; Vitest or Jest for Angular (Karma is deprecated; Jasmine acceptable if the team prefers it).
- **Automation**: coverage is measured by the local test command. Developers run it before opening a pull request and record the result in the PR description. Coverage below target blocks the release candidate.

## Integration Testing

- **Repositories**: Dapper queries against a dedicated test database on LocalDB, rebuilt from migrations for each test run; each repository tested for insert, update with row version, soft delete and each filter.
- **Unit of Work**: commit and rollback behaviour; audit row written with the change; failure in audit rolls back the change.
- **Database rules**: unique slot index; check constraints; foreign keys; soft-delete filters in search queries.
- **Third-party services**: none in Phase 1 (no email, SMS or external APIs), so no mocks are needed beyond the clock and file system.
- **Test data**: deterministic synthetic seed (patients, appointments, visits) created by a script; no real data.

## API Testing

- **Contract**: OpenAPI document generated from the API; reviewed by the front-end developer before each feature is built.
- **Endpoint integration tests**: in-process host with a real test database; authentication, validation errors, not found, conflict, paging, and policy checks per endpoint.
- **Security API tests**: unauthenticated access denied; refresh token rotation and reuse detection; lockout; idle timeout; CORS origin rules; security headers present.
- **Manual or scripted API checks**: optional collection of requests for the tech lead; not a replacement for automated tests.

## UI Testing

- **Component tests**: Angular component tests for forms (validation messages, disabled submit, vital warnings and blocks).
- **End-to-end**: Playwright on Chromium and WebKit (Safari engine), per Q11. Firefox is not included.
- **Critical E2E journeys**: sign in and out; create patient; schedule appointment; start consultation; record vitals (warn and block); add medications; complete visit; print prescription; reprint; history search; export CSV and PDF.
- **Responsive checks**: desktop and tablet widths (768 px and up); consultation form touch targets.
- **Accessibility**: automated accessibility checks (axe-based) run locally on main screens; keyboard-only walkthrough of the consultation and calendar screens.
- **Usability**: timed consultation walkthrough with the doctor in UAT (target 2–3 minutes).

## Security Testing

- **Static analysis**: code analysis rules run in the local build; secret scanning run locally before each commit.
- **Dependency scanning**: NuGet and npm audit run locally at least weekly (see Risk S4).
- **Dynamic scan**: OWASP ZAP baseline scan against the staging environment in S11.
- **Manual review**: OWASP-based checklist for authentication, authorization, input handling, output encoding, cookies, headers, error handling and logging; review of refresh-token and lockout code by a second developer.
- **Authorization tests**: each endpoint tested for policy enforcement and patient ownership.
- **Penetration testing**: optional, before production, recommended in the brainstorm (Phase 1.5); not in the critical path. Product owner to decide.

## Performance and Load Testing

- **Dataset**: 10,000 synthetic patients, 100,000 visits, 40 appointments a day for a year.
- **Scenarios**: patient search; patient history load; calendar day and week; medicine autocomplete; prescription print; CSV and PDF export.
- **Targets**: page load under 2 seconds; search under 2 seconds; history under 3 seconds; export under 10 seconds; print dialog under 5 seconds; 95th percentile under 2 seconds; 99th percentile under 5 seconds.
- **Tooling**: a load-testing tool run against staging; results recorded in the hardening report.

## User Acceptance Testing

- **Who**: the doctor, with synthetic data only (Q20).
- **Duration**: 2 weeks in Sprint 12, plus weekly demos from Sprint 3.
- **Test scenarios**: derived from the BRD workflows 1–7 and the user stories; each scenario has pass or fail recorded.
- **Sign-off criteria**: all Must scenarios pass; no open critical or high defects; consultation completes within the 2–3 minute target in at least three timed runs; prescription print approved; export files approved.

---

# Deployment Strategy

## Development (current setup, Q9)

| Component | Where | How |
|-----------|-------|-----|
| Angular 19 | Developer workstation | Angular CLI development server on `http://localhost:4200` |
| ASP.NET Core Web API | Developer workstation | Kestrel or IIS Express; HTTPS with the development certificate |
| SQL Server | Developer workstation | LocalDB instance `(LocalDb)\MSSQLLocalDB`, Windows authentication |
| Configuration | Per developer | Development settings for LocalDB and CORS; secrets in user secrets |
| Data | Developer workstation | Synthetic data only; no TDE in LocalDB |

## Staging (optional; decided at the M7 gate)

No shared staging environment is set up while implementation runs locally. Where this plan says "staging", it means that environment if the team creates one at M7. Until then, the release candidate and UAT run in a local setup agreed at M7.

- If created: Windows server or App Service slot with the production-like configuration (IIS hosting, TLS, HSTS, TDE enabled on a SQL Server Standard instance or Azure SQL test database).
- Used for ZAP scans, load tests, UAT and the deployment rehearsal.
- Synthetic data only, same as development.

## Future Production (Q9)

| Component | Target | Notes |
|-----------|--------|-------|
| Angular 19 SPA | IIS (static hosting) | HTTPS only; HSTS; security headers; compressed, minified build |
| ASP.NET Core Web API | IIS or Azure App Service | Hosting decision in S11; reverse proxy settings verified; health check monitored |
| SQL Server | SQL Server Standard Edition or Azure SQL Database | TDE and encrypted backups; least-privilege logins; migration login separate from app login |
| Secrets | Azure Key Vault (App Service) or protected configuration (IIS) | Never in source control |
| Monitoring | Health endpoint plus application logs | Error alerting as a Phase 2 item (per brainstorm) unless the hosting platform provides it |

## Release Process

Deployment is manual. There is no CI/CD pipeline in Phase 1.

1. Build from the release branch. Run the build, unit and integration tests, and the dependency and secret scans locally; keep the output with the release notes.
2. Produce versioned artifacts for the API and the SPA, plus the migration scripts.
3. If a staging environment exists, deploy to it manually; run migrations on its database; run smoke tests.
4. Confirm a pre-deployment backup exists (go-live blocker, Q18).
5. Deploy to production in a maintenance window; run migrations; deploy API; deploy SPA; smoke tests.
6. Monitor for the first hour; hypercare daily for one week.

## Rollback

- Application rollback: redeploy the previous API and SPA artifacts.
- Schema rollback: restore from the pre-deployment backup, or apply a forward fix. Migration design favors additive changes so rollback is rarely needed.
- Without a tested backup, rollback of a failed migration is not possible. This is why the backup decision blocks go-live.

## Environment Parity Notes

- LocalDB and production SQL Server share SQL syntax for the planned features, but not all server features. Differences are recorded in the risk log (K1).
- The development CORS origin is removed from production configuration.
- Swagger is available in development and staging only.

---

# Assumptions

| # | Assumption | Basis | If wrong | Mitigation or validation |
|---|------------|-------|----------|--------------------------|
| A1 | Team: 1 tech lead (50%), 3 developers, 0.5 QA | Not specified in the request | Schedule extends or compresses by about 1.5× | Confirm at M0 (T1) |
| A2 | Doctor is the only user in Phase 1 | BRD: single user | Multi-user rework in Phase 2 | Roles and policies are data-driven (Q22) |
| A3 | Doctor is available for weekly demos and a 2-week UAT | Q20 | UAT slips; go-live slips | Book sessions at M0 (T2) |
| A4 | Development uses synthetic data only | Q9 | Real data at risk without TDE | Seeding tool; onboarding rule (S1) |
| A5 | Production hosting will be IIS or App Service with SQL Server Standard or Azure SQL | Q9 | Hosting change | Deployment package host-neutral; decision in S11 |
| A6 | Backups are not set up for now; decision needed before production | Q18 (conflicts with BRD) | Data loss at production go-live | Go-live blocker (M9, D1) |
| A7 | Browsers: latest two versions of Chrome, Edge and Safari | Q11 | Firefox users unsupported | E2E on Chromium and WebKit |
| A8 | Devices: desktop and tablet; phones not optimised | Q12 | Phone use attempted | Responsive checks at tablet width |
| A9 | English only; i18n-ready structure | Q14 | Translation later | Translation files from S1 |
| A10 | Accessibility: WCAG 2.1 AA best effort; not a release blocker | Q13 | Accessibility complaints | Automated checks run locally; findings tracked |
| A11 | Encryption: TLS 1.2 minimum, 1.3 preferred; TDE and encrypted backups in production | Q15, Q16 | Compliance gap | Verified at M9 |
| A12 | Audit covers data changes only | Q17 | Compliance expectations for access logs | Documented; login audit can be added later |
| A13 | Visits are editable; all edits audited with old and new values | Q2 | Audit volume higher than expected | Load test with audit rows (K4) |
| A14 | Patient fields: BRD fields plus blood group and chronic conditions | Q3 | More fields requested | Change control (T3) |
| A15 | Slots: 15 minutes fixed; one patient per slot | Q1; brainstorm | Slot length change | Slot length as a single configured constant in the Domain |
| A16 | Past appointments: date and time locked; status can be set once | Brainstorm acceptance criteria, interpreted | Doctor wants to edit past status | Confirm at S6 demo |
| A17 | Vital-sign ranges as proposed in Risk T6 | BRD and brainstorm | Clinical values differ | Seeded data; confirm at S7 |
| A18 | Password reset is an operator action (no email service in Phase 1) | Scope (no notifications) | Doctor cannot self-reset | Decide in S2; document the procedure |
| A19 | Access token 15 minutes; refresh token with idle timeout 30 minutes; refresh in HTTP-only cookie | Planning decision refining the brainstorm (1-hour access token, local storage) | Session behaviour unexpected | Security tests (S2); review in S11 |
| A20 | Full-text search is available on the chosen SQL Server editions; if not on LocalDB, fallback scan is acceptable | Q5; brainstorm | Keyword search slower | Spike in S1 (K2) |
| A21 | Angular 19 and .NET 10 remain usable for the build period | Brief; support status to verify | Forced upgrade mid-project | Check at S1 (K3) |
| A22 | Patient volume: up to 10,000 patients, 100,000 visits, 40 appointments a day; 1–3 concurrent users (design for 5) | BRD and brainstorm NFRs | Performance issues | Load test (S11) |
| A23 | Retention: keep indefinitely, soft delete only | Q8 | Legal retention rules differ | Confirm before production (S6) |
| A24 | Single clinic; no clinic identifier in data | Q22 | Multi-clinic demand | Phase 2 decision |
| A25 | Prescription printing uses the browser's print dialog; PDF comes from server-generated document | BRD; brainstorm | Print formatting differs by browser | Print tests in Chrome, Edge, Safari |
| A26 | Standard CSV export (UTF-8 with header row) and PDF export are sufficient | BRD export requirement | Spreadsheet import issues | Test with a spreadsheet tool in S10 |

---

# Success Criteria

| # | Criterion | Target | How measured | When |
|---|-----------|--------|--------------|------|
| SC1 | Consultation record completion time | 2–3 minutes per patient | Timed UAT runs (at least three) and hypercare observation | UAT; hypercare |
| SC2 | Patient search response | 95th percentile under 2 seconds; under 5 seconds at 99th | Load test and production logs | S4; S11; hypercare |
| SC3 | Full patient history load | Under 3 seconds | Load test with 100,000 visits | S11 |
| SC4 | Page load | Under 2 seconds (goal under 1 second) | Performance test in Chrome and Edge | S11 |
| SC5 | Prescription print dialog | Under 5 seconds from click | Timed test | S9; S11 |
| SC6 | Prescription correctness | Reprint matches original print; zero snapshot mismatches | Automated snapshot tests; doctor review | S9; UAT |
| SC7 | Export | Under 10 seconds for typical ranges; CSV and PDF open correctly | Timed test; spreadsheet and PDF viewer check | S10 |
| SC8 | Paper usage reduction | At least 80% reduction | Doctor's measured count of printed or filed pages vs pre-launch baseline | 1 month after go-live |
| SC9 | Usability and training | New user productive within 1 hour of training | Timed walkthrough with the doctor's first-week use | UAT; hypercare |
| SC10 | Data integrity | Zero data-loss defects; zero incorrect slot double-bookings | Defect log; database constraint tests | Every sprint; UAT |
| SC11 | Test coverage | 70% overall; 90% on consultation, prescription and authentication | Local coverage report from the test command | Every sprint |
| SC12 | Security | No open critical or high findings at go-live; scans clean | ZAP scan; dependency scan; review checklist | S11; M9 |
| SC13 | Accessibility | Automated checks with no critical violations on main screens | Automated accessibility checks run locally | S11 |
| SC14 | Availability | Target 99% (BRD), with the actual figure reported monthly | Health endpoint monitoring (realistic target agreed in O2) | Post go-live |
| SC15 | Schedule | Go-live by end of Week 24 (Week 26 with contingency) | Milestone tracker | Each gate |
| SC16 | Backup and recovery | Backup in place and restore tested before go-live | Restore test record | M9 |

---

# Recommended Implementation Order

Execute in this order. Do not start a step until the steps it depends on are complete (see the dependency section).

**Sprint 1 (Weeks 1–2): Foundation**
1. Create the solution with the four layers and test projects; set up the Angular workspace.
2. Set up the LocalDB instance and the migration runner; create the migration history table.
3. Create the base audit columns convention and the first migration (Users, master tables, ClinicProfile).
4. Set up logging, the error model, the health endpoint and Swagger.
5. Set up local build and test scripts: build, unit tests, dependency scan. Document the commands in the README.
6. Build the Angular shell (layout, routing, placeholder pages, error interceptor, notification components).
7. Spike: full-text search on LocalDB; record the result.
8. Spike: password hashing cost on the development machine; record the chosen work factor.
9. Create the synthetic data seeding tool (no real data).
10. **Gate**: empty application runs end to end; local build and tests pass.

**Sprint 2 (Weeks 3–4): Authentication and account (M1)**
11. Implement the audit writer and the unit of work; integration test for rollback.
12. Implement login, access token, refresh cookie with rotation and reuse detection, logout.
13. Implement lockout and the 30-minute idle timeout (client and server).
14. Build the login screen, auth guard, auth interceptor (single refresh), and route redirects.
15. Implement change own password (revokes refresh tokens).
16. Implement clinic profile read and update (API and screen).
17. Implement the operator password-reset command with audit entry.
18. **Gate M1**: sign-in, sign-out, lockout, idle timeout, audited profile change.

**Sprint 3–4 (Weeks 5–8): Patient management (M2)**
19. Patient table and repository; create, read, update, soft delete endpoints with validation.
20. Patient form (reactive) with age from DOB, phone normalization and duplicate warning.
21. Paged patient list and search by name and phone with indexes.
22. Recent patients (last 20 per user) and the patient profile page.
23. Blood group and chronic conditions on the patient form and profile.
24. **Gate M2**: patient tests and search time target met.

**Sprint 5–6 (Weeks 9–12): Appointments and dashboard (M3)**
25. Appointment table with unique slot index; create appointment endpoint with slot and patient checks.
26. Day and week calendar (15-minute slots) on the front end.
27. Reschedule (future only) and status transitions, within one transaction.
28. Past-appointment rules and their tests.
29. Dashboard: today's and upcoming appointments, recent patients (deferred), quick actions.
30. Appointment history on the patient profile.
31. **Gate M3**: double-booking prevented; dashboard accepted.

**Sprint 7–8 (Weeks 13–16): Consultation capture (M4)**
32. Visit table and start-visit endpoint from an appointment (InProgress).
33. Vitals table, vital-range configuration, warn and block rules.
34. Complaints and diagnosis tables and screens; draft save.
35. Medications table, medicine autocomplete from own history.
36. Complete-visit transaction: visit status, appointment status, child records, audit rows.
37. Unsaved-changes guard on the consultation form.
38. Edit completed visit with field-level audit and last-modified display.
39. **Gate M4**: consultation end to end; vital rules tested; edit audit verified.

**Sprint 9 (Weeks 17–18): Prescription and history (M5)**
40. Prescription table and generation from visit data plus clinic profile.
41. Print view (lightweight layout); record each print with its snapshot; reprint from snapshot.
42. Visit history list with date-range filter.
43. Keyword search within one patient's history (full-text or fallback, per the spike).
44. Visit detail view.
45. **Gate M5**: snapshot and reprint tests pass; doctor reviews the print layout.

**Sprint 10 (Weeks 19–20): Reporting (M6)**
46. Patient list CSV export (streaming).
47. Visit CSV and PDF export with date range; prescription PDF from snapshot.
48. Progress and asynchronous handling for large exports.
49. **Gate M6**: feature freeze; export time target met.

**Sprint 11 (Weeks 21–22): Hardening (M7)**
50. Load test on the 10,000-patient, 100,000-visit dataset; tune queries from execution plans.
51. Security review; OWASP ZAP baseline scan on staging; fix findings.
52. Accessibility pass with automated checks.
53. Backup and recovery decision implemented; restore test passed (D1).
54. Production hosting decision; deployment package; secret store; TLS and HSTS verified.
55. Runbook, release notes draft, UAT pack and seeded demo dataset.
56. **Gate M7**: release candidate on staging; no open critical or high findings.

**Sprint 12 (Weeks 23–24): UAT and go-live (M8–M10)**
57. Doctor UAT, two weeks, with daily triage and fixes.
58. Go-live checklist, including the backup gate; rollback rehearsal.
59. **Gate M9**: UAT sign-off by the doctor; readiness confirmed.
60. Production deployment (M10).

**Hypercare (4 weeks after go-live)**
61. Daily issue review in week one, then twice weekly; measure success criteria; handover (M11).

---

# Immediate Next Actions

| # | Action | Priority | Recommended Owner | Expected Outcome |
|---|--------|----------|-------------------|------------------|
| 1 | Confirm the team size and availability (A1) | High | Product owner | Schedule confirmed or re-baselined |
| 2 | Book the doctor's weekly demo slots and the 2-week UAT window (A3) | High | Product owner | Doctor availability secured |
| 3 | Decide the backup and recovery approach for production (Q18; D1) | High (blocker before M9) | Product owner with tech lead | Backup plan agreed, scheduled for S11 |
| 4 | Check Angular 19 support status and record the decision (K3) | High | Tech lead | Version decision recorded before S1 |
| 5 | Run the full-text search spike and password hashing spike (K2, A20) | High | Tech lead | Keyword search approach confirmed |
| 6 | Confirm the vital-sign ranges (T6, A17) | Medium | Doctor via product owner | Ranges confirmed before S7 |
| 7 | Confirm password reset approach (A18) | Medium | Product owner | Operator procedure agreed before S2 |
| 8 | Set up the synthetic data policy and seeding tool (A4, S1) | High | Tech lead | No real patient data in development |
| 9 | Hand this plan to the WorktreeAgent and ImplementationAgent for Sprint 1 setup | Medium | Tech lead | Sprint 1 starts on the agreed structure |
