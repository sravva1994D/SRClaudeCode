# Worktree and Branch Strategy: Patient Management System

**Date**: 2026-11-10
**Prepared by**: WorktreeAgent
**Repository**: https://github.com/sravva1994D/SRClaudeCode.git
**Inputs**: `documents/implementation-plan.md` (sprints S1–S13, milestones M0–M10, phases 1–6), `.claude/agents/WorktreeAgent.md`
**Status**: Proposed. Bootstrap steps in section 1 need explicit approval before they run.

---

## 1. Repository Assessment (Current State)

Discovered at runtime on 2026-11-10:

| Item | Value |
|------|-------|
| Local repository | `d:\Claude Code\ClaudePractical\ClaudeCodeSR\SRClaudeCode` |
| Remote | `origin` = https://github.com/sravva1994D/SRClaudeCode.git (fetch and push) |
| Remote state | Reachable, **no refs**: the GitHub repository is empty |
| Local branch | `main`, **no commits yet** |
| Git version | 2.55.0 (worktree support: yes) |
| Operating system | Windows 11 (Git Bash and PowerShell) |
| Line endings | `core.autocrlf = true` |
| Git identity | Shravan Ravva (set locally) |
| Active worktrees | 1 (the main working tree only) |
| Untracked content | `.claude/agents/` (8 agent files), `BRD/Doc_BRD.md`, `documents/` (3 planning documents), `Reference/AI Training.docx` |
| Pre-existing `documents/worktree-plan.md` | A generic template that assumes `develop` and `developer` branches. Replaced by this document. Copy kept outside the repository. |

**Consequence**: there is no `main` on the remote and no commit history to branch from. The first step is a one-time bootstrap (section 4), not a feature branch.

---

## 2. Branch Strategy

### 2.1 Branch Model

Two long-lived branches, plus short-lived branches for each type of change.

| Branch | Role | Protected | Who writes to it | How changes arrive |
|--------|------|-----------|------------------|--------------------|
| `main` | Production-ready code; each release is a tag on `main` | Yes | Nobody directly | Release PR from `release/*`, or hotfix PR from `hotfix/*` |
| `develop` | Integration branch; always buildable and tested | Yes | Nobody directly | Feature PRs (squash merge) |
| `release/vX.Y.Z` | Stabilization for UAT and go-live | Yes (from creation) | Nobody directly | Fix PRs targeting the release branch |
| `feature/*`, `fix/*`, `refactor/*`, `docs/*`, `chore/*`, `test/*`, `spike/*` | Work in progress | No | Owner of the branch only | Pushed to origin, then PR to `develop` |
| `hotfix/*` | Urgent production fixes | No (PR target only) | Owner of the branch only | PR to `main`, then back-merged to `develop` |

Why two long-lived branches: the team has three developers and a single release. `develop` gives a stable integration point during Sprints 3–11. `main` only ever holds what was released, which matters because the project has no backup plan yet (Q18): a bad release must be easy to identify and roll back to.

`develop` is the default base for all feature work. The GitHub default branch is set to `develop` after bootstrap, so new pull requests target it automatically.

### 2.2 Protected Branches (Discovered and Recorded)

These are the branches the team must never develop on directly:
- `main` (production)
- `develop` (integration)
- `release/*` (stabilization)

A `hotfix/*` branch is a working branch, but it may only receive a PR into `main`.

Branch names that look protected but are not used in this project: `master` (not created), `developer` (not used; the old template's `developer` branch is dropped to avoid two integration branches).

### 2.3 Release Cadence

| Release | Cut from | Cut at | Tag | Merged into |
|---------|----------|--------|-----|-------------|
| `v0.1.0` | `develop` | End of Sprint 2 (M1) | Optional, for demo builds | Tag only; no release branch |
| `v0.x.0` | `develop` | End of phases 2–5 (optional) | Tag per demo cycle | Tag only |
| `release/v1.0.0` | `develop` | Start of Sprint 11 (M7, release candidate) | `v1.0.0-rc.N` on this branch | `main` (merge commit), then back-merged to `develop` |
| `v1.0.0` | `main` | Go-live (M10) | `v1.0.0` on `main` | — |

Pre-release tags during the build are informal: they mark demo points, not supported releases.

---

## 3. Branch Naming Strategy

### 3.1 Format

```
<type>/<issue-number>-<module>-<short-slug>      (when a GitHub issue exists)
<type>/<module>-<short-slug>                     (when no issue exists)
```

Examples:
- `feature/42-auth-refresh-rotation`
- `feature/patients-duplicate-check`
- `fix/57-appointments-slot-timezone`
- `spike/search-fulltext-localdb`
- `release/v1.0.0`
- `hotfix/63-prescription-print-blank-footer`

### 3.2 Types

| Type | Use for | Replaces WorktreeAgent types |
|------|---------|------------------------------|
| `feature/` | New user-facing capability or endpoint | `enhancement/` (merged into feature) |
| `fix/` | Defect fix during development or UAT | `bugfix/` |
| `refactor/` | Structural change, no behaviour change | — |
| `test/` | Tests only (unit, integration, E2E) | — |
| `docs/` | Documentation only | — |
| `chore/` | Build, tooling, dependencies, repository setup | — |
| `spike/` | Time-boxed investigation; result is a document, code is throw-away | — |
| `release/` | Release stabilization (long-lived, see 2.1) | — |
| `hotfix/` | Production fix (see 2.1) | — |

### 3.3 Module Keys

Modules come from `implementation-plan.md` section "Module Breakdown". Use these keys in branch names so related work is easy to find.

| Key | Module |
|-----|--------|
| `platform` | Foundation: solution, logging, errors, health, local build and test scripts, migrations, audit writer |
| `auth` | Authentication and authorization |
| `admin` | Administration: clinic profile, own password, audit viewer |
| `patients` | Patient management, recent patients |
| `appointments` | Appointment management |
| `dashboard` | Dashboard |
| `consult` | Medical records: visit, vitals, complaints, diagnosis, medications |
| `rx` | Prescription generation, print, reprint |
| `history` | Visit history and keyword search |
| `export` | Reporting and exports |
| `ui` | Shared Angular components, theme, layout, notifications |
| `db` | Schema or migration-only work (when not owned by a module) |
| `ops` | Deployment, hosting, backup, runbook |

### 3.4 Rules

- Lowercase, hyphen-separated, ASCII only. Maximum 50 characters after the prefix.
- Slug states the outcome, not the task: `patients-duplicate-check`, not `patients-add-code`.
- Issue number is used when a GitHub issue exists. Create issues from the task list in the implementation plan; one issue per task or per small group of tasks.
- Never reuse a branch name after its PR is merged. Start a new branch, even for follow-up work.
- Never use a name that equals a protected branch (`main`, `develop`, `release/...`) for work in progress.
- Never put personal names, dates or sprint numbers in branch names. Sprint is tracked in the issue or milestone.

### 3.5 Invalid Names

| Name | Problem |
|------|---------|
| `Feature/PatientSearch` | Mixed case |
| `feature/patient_search` | Underscore |
| `feature/patient search` | Space |
| `feature/` | No description |
| `develop-patients` | Wrong prefix |
| `feature/sprint3-stuff` | Not an outcome |

---

## 4. One-Time Bootstrap

The remote is empty, so the usual "branch from develop" flow cannot start. The bootstrap uses one controlled exception, which requires explicit approval (WorktreeAgent constraint: no action on protected branches without approval).

### 4.1 Pre-Commit Repository Hygiene

Before the first commit, add these files on `main` (bootstrap is the only time the team commits to `main` directly):

| File | Purpose |
|------|---------|
| `.gitignore` | Excludes build output (`bin/`, `obj/`, `dist/`, `.angular/`, `node_modules/`), IDE folders (`.vs/`, `.idea/`), local settings (`.claude/settings.local.json`, `appsettings.*.local.json`), secrets (`secrets.json`, `.env`, `*.pfx`, `*.key`), and **database files** (`*.mdf`, `*.ldf`, `*.bak`, `*.dacpac` with data) |
| `.gitattributes` | `* text=auto` so line endings are normalized in the repository (works with `core.autocrlf = true`); binary types marked as binary (`*.docx`, `*.pdf`, `*.png`) |
| `README.md` | Project purpose, how to run locally, link to `documents/` |

### 4.2 Content Decisions

| Path | Commit? | Reason |
|------|---------|--------|
| `documents/*.md` | Yes | Planning documents are team artifacts; reviewed through PRs later |
| `BRD/Doc_BRD.md` | Yes | Source requirements |
| `.claude/agents/*.md` | Yes | Shared agent definitions used by the team |
| `.claude/settings.local.json` | No | Per-developer; excluded by `.gitignore` |
| `Reference/AI Training.docx` | **Decision needed** | Not part of the product. Recommend keeping it out of the repository (add `Reference/` to `.gitignore`) unless the team says it is needed |
| Any file with patient data | Never | Synthetic data only; the seeding tool generates data at runtime |

### 4.3 Bootstrap Sequence (run only after approval)

1. Create `.gitignore`, `.gitattributes`, `README.md` (section 4.1).
2. Stage the approved files only (never `git add .` without checking `git status` first).
3. Commit on `main` as the initial commit.
4. Push `main` to origin with upstream tracking.
5. Create `develop` from `main` and push it.
6. Set `develop` as the GitHub default branch.
7. Apply branch protection and rulesets (section 10.1).
8. Verify: `git ls-remote origin` shows `main` and `develop`; `git worktree list` shows only the main working tree.

After step 4, `main` is protected. Steps 5–7 happen immediately after so there is no window where unprotected work lands on `main`.

---

## 5. Worktree Strategy

### 5.1 Principles

- **One branch, one worktree.** A branch is checked out in at most one worktree at a time. Git enforces this; the rule is kept so developers do not work around it.
- **One task, one worktree.** A worktree is created for one issue or one agent task and removed when its PR is merged.
- **Worktrees are outside the main folder.** The main working tree at the repository root is used only for syncing and for release work. Feature work never happens there.
- **Main working tree stays on `develop`.** It is used to create worktrees and to run `git worktree` commands. It is not used for coding.

### 5.2 Location and Folder Naming

Worktrees live in a short folder outside the repository, to avoid Windows path-length problems with Angular and NuGet packages:

```
d:\wt\pms\                                       Worktree root (short path, keeps node_modules paths under 260 characters)
├── feature-auth-refresh-rotation\               Branch: feature/auth-refresh-rotation (or with issue number)
├── feature-42-patients-duplicate-check\         Branch: feature/42-patients-duplicate-check
├── fix-57-appointments-slot-timezone\           Branch: fix/57-appointments-slot-timezone
├── spike-search-fulltext-localdb\               Branch: spike/search-fulltext-localdb
└── release-v1-0-0\                              Branch: release/v1.0.0 (long-lived, locked)
```

Rules:
- Folder name = branch name with `/` replaced by `-`, and dots replaced by `-`.
- Folder names are unique. If two branches would produce the same folder name, rename the branch before creating the worktree.
- The worktree root `d:\wt\pms\` is not inside OneDrive or any synced folder, to avoid file-lock problems during builds.
- Enable long paths once per machine and set `core.longpaths true` in the repository (`git config core.longpaths true`), as a safety measure.

### 5.3 Worktree Limits

| Limit | Value | Reason |
|-------|-------|--------|
| Worktrees per developer | 2 at a time | One feature and one review or fix branch; more causes context loss |
| Total feature worktrees in the team | 4 at a time | Three developers plus one spare for an agent task |
| Release worktree | 1 (`release/v1.0.0`), from Sprint 11 onward | Used only for UAT and go-live fixes |
| Spike worktree | Counts as a feature worktree | Time-boxed; removed within 5 working days |

### 5.4 Standard Commands

These are the commands used by developers and agents. Run from the main working tree unless noted.

**Create a feature worktree (from the latest develop):**
```
git fetch --prune origin
git worktree add d:\wt\pms\feature-42-patients-duplicate-check -b feature/42-patients-duplicate-check origin/develop
```

**Open it:**
```
cd d:\wt\pms\feature-42-patients-duplicate-check
git status
git branch --show-current
```

**Check the mapping (1 branch = 1 worktree):**
```
git worktree list
```

**Publish the branch for the first time:**
```
git push -u origin feature/42-patients-duplicate-check
```

**Remove a worktree after its PR is merged (see section 9 for the full cleanup):**
```
git worktree remove d:\wt\pms\feature-42-patients-duplicate-check
```

### 5.5 Locking

- The release worktree (`release/v1.0.0`) is locked from creation to go-live, with the reason "Release stabilization. Do not remove." This stops accidental removal during UAT.
- Feature worktrees are not locked. Removing them is safe when the working tree is clean, and a lock would only add an unlock step.
- Unlock is done by the person who locked the worktree, after confirming the work is pushed.

---

## 6. Feature Development Approach

### 6.1 From Plan to Branch

Each task in `implementation-plan.md` (sprint gates and the ordered list in "Recommended Implementation Order") becomes one or more GitHub issues. Each issue gets one branch and one PR.

| Plan element | Git element |
|--------------|-------------|
| Milestone M1–M10 | GitHub milestone |
| Sprint S1–S13 | GitHub project iteration or label `sprint-N` |
| Ordered task (e.g., "Implement login, access token, refresh cookie") | GitHub issue |
| Issue | Branch `<type>/<issue>-<module>-<slug>` |
| Acceptance criteria | Issue checklist and PR checklist |

### 6.2 Sizing

- A PR should change no more than about 400 lines of production code, excluding generated files and tests. Larger work is split into a sequence of PRs, each of which keeps `develop` buildable.
- A branch should live no more than 5 working days. Longer tasks are split.
- Database changes are small and additive. A migration and the code that uses it go in the same PR, so `develop` always matches its schema.

### 6.3 Daily Flow

1. Start of day: `git fetch --prune origin` in the main working tree; check the open PRs assigned to you.
2. Work only inside your feature worktree.
3. Commit in small steps with the commit format below.
4. Push at least once a day. Unpushed work exists only on one machine, and the project has no backup (Q18), so a daily push is the safety net.
5. Before opening the PR, sync with `develop` (section 7.2) and run the local test set.
6. End of day: confirm `git status` is clean or the uncommitted work is committed to the feature branch.

### 6.4 Commit Format

Conventional commit prefixes, one line subject under 72 characters:

| Prefix | Use |
|--------|-----|
| `feat:` | New behaviour |
| `fix:` | Defect fix |
| `refactor:` | No behaviour change |
| `test:` | Tests only |
| `docs:` | Documentation only |
| `chore:` | Build, tooling, dependencies |
| `db:` | Migration script (always with the code that uses it) |

Example subject: `feat(patients): add duplicate warning on name and date of birth`

Commit body (optional) explains why, not what. Team policy for AI-agent commits: the Co-Authored-By trailer is added as set by the team's attribution rule.

### 6.5 Parallel Work Within a Sprint

Sprints in the plan have parallel work (for example, backend and front end for patient CRUD in S3). Use separate branches and worktrees:

| Sprint | Parallel branches (examples) | Merge order |
|--------|------------------------------|-------------|
| S1 | `chore/platform-solution-layout`, `ui/shell-layout`, `spike/search-fulltext-localdb`, `spike/password-hash-cost` | Solution layout first; shell and spikes after; spikes do not merge (results go into `documents/`) |
| S2 | `feature/auth-login-api`, `feature/auth-refresh-cookie`, `feature/ui-login-screen`, `feature/admin-clinic-profile` | API first; screen and refresh logic after the API PR is merged |
| S3–S4 | `feature/patients-api`, `feature/patients-ui-form`, `feature/patients-search` | API contract (OpenAPI) agreed first; UI branch builds against the contract and rebases after API merge |
| S7–S8 | `feature/consult-start-visit`, `feature/consult-vitals`, `feature/consult-complete-transaction`, `feature/consult-edit-audit` | Start visit first; vitals and complete-visit transaction in parallel; edit-audit last |
| S9 | `feature/rx-generate-snapshot`, `feature/rx-print-view`, `feature/history-search` | Snapshot before print view |

Rule: two branches that change the same file at the same time are coordinated in the issue. The later PR owns the conflict resolution.

### 6.6 Agent Tasks

AI agents (ImplementationAgent, VerificationAgent, CodeReviewAgent) follow the same rules:
- Each agent task gets its own issue, branch and worktree. No agent works in a developer's worktree.
- An agent never commits to a protected branch. Its PR is reviewed by a human before merge.
- An agent does not remove worktrees or delete branches. Cleanup is done by the person who owns the branch, after the PR is merged (section 9).

### 6.7 Stacked Changes

Avoid stacked PRs (a PR based on another open PR). When one is unavoidable:
- Only one level deep.
- The base PR must be reviewed and approved before the stacked one is reviewed.
- After the base PR is merged, retarget the stacked PR to `develop` and merge `develop` into the branch (do not rebase).

---

## 7. Sync and Conflict Approach

### 7.1 Principle

Keep feature branches up to date with `develop` by **merging `develop` into the feature branch**, not by rebasing. Merging needs no force push, and the squash merge at the end removes the merge commits from `develop`.

### 7.2 Sync Commands

```
git fetch origin
git merge origin/develop
```

Run before opening the PR, after each review round, and whenever `develop` changes a file the branch also changes.

### 7.3 Conflict Handling

- Resolve conflicts in the feature worktree, then run the full local test set before pushing.
- If a conflict touches a schema or migration file, the owner of the migration decides the resolution and notifies the other branch owner in the issue.
- If a conflict is too large to resolve in one sitting, stop, abort the merge (`git merge --abort`), and split the work.

### 7.4 Force Push Policy

- Force push is not used by default. This keeps the audit trail intact.
- If a rebase is ever needed (for example, a branch with a wrong base), it is done only on a feature branch owned by the person doing it, and it uses `--force-with-lease`, never `--force`. Each use needs explicit approval from the tech lead and is recorded in the PR.
- Force push to `main`, `develop`, or any `release/*` branch is blocked by branch protection (section 6.3) and is never done.

---

## 8. Merge Approach

### 8.1 Merge Flow

```
feature branch → push → PR to develop → local checks recorded in PR → review (1 approval) → squash merge → delete remote branch → cleanup worktree
```

No merge happens without an approved PR. No merge is automatic: the person who merges is the PR owner, after approval, and with the local checks in section 8.3 recorded as passed in the PR description.

### 8.2 Pull Request Rules

| Rule | Detail |
|------|--------|
| Target branch | `develop` for all features, fixes, refactors, docs, chores, tests, spikes. `release/*` only for release fixes. `main` only for release and hotfix PRs |
| Title | Same as the squash commit: `feat(patients): add duplicate warning on name and date of birth` |
| Description | PR template below |
| Size | Target 400 lines of production code (section 6.2) |
| Draft | Open as draft while work is in progress; mark ready for review when checks pass locally |
| Linked issue | "Closes #42" in the description, so the issue closes on merge |
| Reviewers | 1 approval from another team member. Database migration PRs also need the tech lead's approval |
| Self-review | Required before requesting review: read the full diff, remove debug code, check tests |

**PR description template:**

```
## Summary
One or two sentences: what changed and why.

## Changes
- Key change 1
- Key change 2

## Testing
- Unit tests: added / updated / not needed (reason)
- Integration tests: ...
- Manual check: steps performed

## Impact
- Database migration: No / Yes (migration name)
- API contract change: No / Yes (endpoint)
- Breaking change: No / Yes
- Patient data touched: No / Yes (synthetic data only in development)

Closes #42
```

### 8.3 Local Quality Checks (No CI)

Phase 1 has no CI pipeline, so GitHub cannot block a merge on these checks. The PR author runs them locally and records the results in the PR description (Testing section). The reviewer checks that record before approving.

| Check | How it is run locally | Blocks PR? |
|-------|-----------------------|------------|
| Build (API and Angular) | `dotnet build` and `npm run build` | Yes |
| Unit tests | `dotnet test` and `npm test` | Yes |
| Integration tests (API and repositories) | `dotnet test` against the test database on LocalDB | Yes |
| Front-end component tests | `npm test` | Yes |
| Dependency vulnerability scan (NuGet and npm) | `dotnet list package --vulnerable` and `npm audit`; critical and high findings must be fixed or explained | Yes, if critical or high |
| Secret scan | Review `git diff --staged` before committing; no keys, passwords or connection strings | Yes |
| Lint and format | `dotnet format --verify-no-changes` and the Angular lint and format scripts | Yes |
| Accessibility (main screens) | Automated axe-based check run locally from S7; advisory until S11 | No until S11 |

End-to-end tests run locally before each release merge, and the result is recorded in the release PR. They are not required for every PR.

The team adds a CI pipeline only when the decision in the brainstorm review (item 20) is revisited. Until then, the PR template and reviewer check are the only enforcement.

### 8.4 Merge Method

| Target | Method | Reason |
|--------|--------|--------|
| `develop` | Squash merge | One commit per feature keeps `develop` readable and easy to revert |
| `release/*` fixes | Squash merge | Same reason |
| `main` (from release or hotfix) | Merge commit | Keeps the release branch history visible on `main` |
| Back-merge `main` → `develop` | Merge commit (PR or direct merge via PR) | Records that the release went out |

If GitHub rulesets can restrict allowed merge methods per target branch on the team's plan, enable that. If not, the rule is enforced by the merging developer and by reviewers.

### 8.5 Release Merge (`release/v1.0.0` → `main`)

1. All release fixes are merged into `release/v1.0.0` by PR.
2. End-to-end tests and the UAT sign-off checklist pass on the release branch.
3. Open PR from `release/v1.0.0` to `main`. Approval from the product owner (go-live decision) and the tech lead.
4. Merge with a merge commit.
5. Tag `v1.0.0` on `main`.
6. Open a PR from `main` to `develop` to back-merge, and merge it with a merge commit.
7. Remove the release worktree (after unlock, see 5.5) and delete the release branch only after the tag exists and the back-merge is merged.

### 8.6 Hotfix Flow (after go-live)

1. Branch `hotfix/<issue>-<module>-<slug>` from `main`.
2. Fix, test, push. PR to `main`. Approval from the tech lead.
3. Merge with a merge commit; tag `v1.0.x`.
4. Back-merge `main` into `develop` by PR, and into any open `release/*` branch.
5. Clean up as in section 9.

Backup requirement: no hotfix is deployed to production until the backup gate in the implementation plan (Q18 blocker) is closed.

---

## 9. Cleanup Approach

### 9.1 When Cleanup Happens

Cleanup is done for every branch and worktree within one working day of its PR being merged. Stale items are found in the weekly audit (9.5).

### 9.2 Per-Branch Cleanup Checklist

Owner of the branch runs these steps:

1. Confirm the PR state is `MERGED`:
   ```
   gh pr view <number> --json state,mergedAt
   ```
2. Confirm the worktree has no uncommitted work:
   ```
   git -C d:\wt\pms\<folder> status
   ```
   Expected: "nothing to commit, working tree clean". If not, stop and decide what to do with the changes. Never discard them silently.
3. Confirm the worktree is not locked (`git worktree list`).
4. Remove the worktree from the main working tree:
   ```
   git worktree remove d:\wt\pms\<folder>
   ```
5. Delete the local branch. Squash merges do not make the branch an ancestor of `develop`, so `-d` refuses. Use `-D` only after step 1 confirms the PR is merged:
   ```
   git branch -D feature/42-patients-duplicate-check
   ```
6. Prune remote-tracking references:
   ```
   git fetch --prune origin
   ```
7. Confirm:
   ```
   git worktree list
   git branch -vv
   ```

**Approval note**: `git branch -D` is a force operation under the WorktreeAgent rules. It is pre-approved for this one case: the PR is confirmed `MERGED` in step 1 and the worktree was clean in step 2. Any other use of `-D` (unmerged branch, abandoned work) needs explicit approval with the list of commits that will be lost.

### 9.3 Remote Branch Cleanup

- GitHub setting "Automatically delete head branches" is enabled, so the remote feature branch is deleted when its PR is merged.
- If a remote branch remains after merge, delete it on GitHub or with `git push origin --delete <branch>` after confirming the PR is merged.

### 9.4 Abandoned Work

When a PR is closed without merge (feature dropped, approach replaced):

1. Confirm with the tech lead whether any part should be kept. Keep the branch if yes (move the useful part to a new branch).
2. If no: the PR is closed, the worktree is cleaned up as in 9.2, and the remote branch is deleted.
3. Deleting a branch does not delete its commits immediately: they stay in the developer's local reflog for a period set by Git (90 days by default) until garbage collection. Before deleting, record the branch name and last commit hash in the issue comments, so the work can be recovered with `git branch <name> <hash>` while that period lasts.

### 9.5 Weekly Audit

Run every Friday by the tech lead (about 10 minutes):

```
git worktree list
git worktree prune --dry-run
git fetch --prune origin
git branch --merged origin/develop
git for-each-ref --sort=committerdate refs/heads/ --format="%(committerdate:short) %(refname:short)"
```

Actions:

| Finding | Action |
|---------|--------|
| Worktree with no commit for 10 working days | Ask the owner: continue, or close the PR and clean up |
| Local branch merged into `develop` with no worktree | Delete with `git branch -d` (merge commit case) or `-D` after PR check as in 9.2 |
| Worktree folder missing but still listed | `git worktree prune` (safe: removes metadata only) |
| Worktree folder present but not listed | Do not delete the folder by hand. Run `git worktree repair` first, then decide |
| Locked worktree older than the release | Ask the owner for the reason before any action |

### 9.6 Repairing a Broken Worktree

Use the safest option first:

1. `git worktree repair` (restores links after a moved folder).
2. `git worktree prune` (removes metadata for a folder that no longer exists).
3. If the folder still has work and cannot be opened: copy the folder to a backup location, confirm the copy is complete, and only then remove it with `git worktree remove --force`. Record the action in the issue.

The project's `.gitignore` excludes database files, so a broken worktree never holds patient data in version control. The folder copy may hold local `appsettings` secrets; keep it on the developer machine only.

### 9.7 Cleanup Record

Each cleanup is recorded in the closing comment of the issue with:
- Worktree removed (folder name)
- Branch deleted (name and last commit hash)
- Remote branch confirmed deleted
- Verification output (`git worktree list` shows only active worktrees)

---

## 10. Repository Protection Configuration

### 10.1 Branch Protection and Rulesets

| Setting | `main` | `develop` | `release/*` |
|---------|--------|-----------|-------------|
| Require pull request before merging | Yes | Yes | Yes |
| Required approvals | 1 (product owner or tech lead for release) | 1 | 1 |
| Require status checks | No (no CI pipeline; see 8.3) | No | No |
| Require branch to be up to date before merge | Not applicable (no required checks) | Not applicable | Not applicable |
| Block force pushes | Yes | Yes | Yes |
| Block deletions | Yes | Yes | Yes |
| Include administrators | Yes | Yes | Yes |
| Require linear history | No (merge commits allowed for release) | No | No |
| Restrict who can push | Nobody directly | Nobody directly | Nobody directly |

"Include administrators" means the repository owner also goes through a PR. It is the main control against accidental commits to protected branches.

Team has one repository owner (the GitHub account above) and three developers. Developers are granted "Write" access to the repository so they can push feature branches; protection rules stop direct pushes to protected branches.

### 10.2 Repository Settings

| Setting | Value |
|---------|-------|
| Default branch | `develop` (after bootstrap) |
| Automatically delete head branches | Enabled |
| Allow squash merging | Enabled (used for `develop`) |
| Allow merge commits | Enabled (used for `main` and back-merges) |
| Allow rebase merging | Disabled (not used; avoids rewritten history on `develop`) |
| Allow force pushes | Disabled for all branches |
| Issues | Enabled (feature tracking) |
| Projects or milestones | Enabled (sprints and milestones M0–M10) |

### 10.3 Access

- Repository owner: admin.
- Three developers: write (push feature branches, open PRs).
- Product owner (doctor or stakeholder): read, plus review on release PRs if added as a collaborator.
- AI agents run under a developer's account; they have no separate credentials. Their PRs are visible as the developer's PRs, with the agent noted in the PR description.

---

## 11. Team Guidelines

1. Work only in a worktree under `d:\wt\pms\`. Never edit files in the main working tree for feature work.
2. Never commit to `main`, `develop` or `release/*` directly.
3. One issue, one branch, one worktree, one PR.
4. Push at least once a day.
5. Keep PRs under about 400 lines of production code.
6. Merge `develop` into the feature branch to sync. Do not rebase shared branches.
7. Squash-merge into `develop`; merge-commit into `main`.
8. Delete the worktree and branch within one working day of merge.
9. Never discard uncommitted work. If unsure, ask in the issue first.
10. Keep patient data and database files out of Git. Synthetic data only.

---

## 12. Implementation Sequence for This Strategy

| Step | Action | Owner | Gate |
|------|--------|-------|------|
| 1 | Approve this strategy | Tech lead, product owner | Approval recorded in the issue "Repository setup" |
| 2 | Bootstrap repository (section 4.3) | Tech lead | Remote shows `main` and `develop` |
| 3 | Apply branch protection and repository settings (10.1, 10.2) | Repository owner | Test push to `main` is rejected |
| 4 | Create GitHub milestones M0–M10 and sprint labels | Tech lead | Milestones match the implementation plan |
| 5 | Create issues for Sprint 1 tasks (plan steps 1–10) | Tech lead | Each issue has acceptance criteria |
| 6 | Create the worktree root `d:\wt\pms\` and enable long paths | Each developer | `git config core.longpaths true` set |
| 7 | Start Sprint 1 with the first feature worktree | Developers | First PR merged to `develop` |
| 8 | Run the first weekly audit (9.5) | Tech lead | Audit report with no findings |

---

## 13. Items Needing Decision

| # | Item | Recommendation | Who decides |
|---|------|----------------|-------------|
| W1 | Approve the one-time bootstrap commit on `main` | Yes; it is the only way to start with an empty remote | Tech lead |
| W2 | Whether `Reference/AI Training.docx` is part of the repository | Exclude it (add `Reference/` to `.gitignore`) | Product owner |
| W3 | GitHub plan features (rulesets and per-branch merge method restriction) | Use them if the plan supports them; otherwise enforce by convention | Repository owner |
| W4 | Whether to use GitHub Issues and Milestones for tasks | Yes, so the implementation plan maps to tracked work | Tech lead |
| W5 | Whether developers have separate GitHub accounts or share one | Separate accounts, so PR reviews and authorship are traceable | Repository owner |
| W6 | Whether a `develop` branch is created before Sprint 1 starts | Yes, from the bootstrap | Tech lead |

---

## 14. Document Control

| Item | Value |
|------|-------|
| Version | 1.0 |
| Date | 2026-11-10 |
| Owner | Tech lead |
| Review | At M1 (end of Sprint 2) and at the release candidate (M7) |
| Related documents | `documents/implementation-plan.md`, `.claude/agents/WorktreeAgent.md` |
