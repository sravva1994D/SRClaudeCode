# Risk Decisions

**Audience**: tech lead and developers. Records accepted risks that CLAUDE.md or the plan would otherwise block.

## R1: Accept known npm advisories on Angular 19 and tooling (plan risk K3)

**Date**: 2026-11-11
**Requested by**: the developer on this sprint, after the Sprint 1 gate run (`scripts/verify.ps1`).
**Status**: accepted for Sprint 1 and Sprint 2. **Tech lead review pending.**

### Finding

`npm audit` on `src/frontend/patient-management-web` reports 56 advisories:

| Severity | Count | Where |
|----------|-------|-------|
| Critical | 4 | `piscina`, `tar`, `tinypool`, `vitest` (test runner, dev only) |
| High | 14 | 8 in runtime dependencies (`@angular/common`, `compiler`, `core`, `router`, and transitive packages); the rest in build and CLI tooling |
| Moderate | 36 | Mostly tooling |
| Low | 2 | Tooling |

Every fix npm offers is a major-version upgrade: Angular 22 for the framework and build tools, and vitest 5 for the test runner. Angular 19 is the version the product owner requested.

### Decision

Accept the current advisories for Sprint 1 and Sprint 2. Do not upgrade Angular during the sprint. Do not add new advisories.

### Compensating controls

- Nothing is deployed. Development runs on a single workstation against synthetic data only (CLAUDE.md §2 rule 1).
- The runtime advisories concern framework code paths that Sprint 1 does not expose: no authentication, no user-supplied HTML, no server-side rendering.
- `scripts/verify.ps1` compares every audit count with `scripts/frontend-audit-baseline.json`. The gate fails if any count rises, so new advisories still block a pull request.

### Limits

- This does not satisfy CLAUDE.md §10.8 for release. Critical and high findings must be fixed before the release candidate (Sprint 11, M7).
- Reassess at the M1 review (end of Sprint 2). If the team has not started the upgrade by then, escalate.

### Upgrade plan

Upgrade Angular to a supported major (22.x) and vitest to 5.x. Do this as its own change, in a separate branch, with the full test and build gates. Target: before M1 if capacity allows, otherwise no later than Sprint 11.

### Reverting this decision

Update `scripts/frontend-audit-baseline.json` only when a finding is actually fixed (lower the count). Raising a count requires a new entry in this file.
