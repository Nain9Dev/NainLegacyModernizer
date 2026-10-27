# Documentation map

Canonical documentation for NainLegacyModernizer. Feature work lives in `../specs/NNN-feature-name/` and consolidates here.

| Phase | Document | Status |
| :--- | :--- | :--- |
| Planning | [00-charter.md](00-charter.md) | Approved |
| Analysis | [01-requirements.md](01-requirements.md) | Active |
| Design | [02-architecture.md](02-architecture.md) | Active |
| Design | [30-decisions/](30-decisions/) | see each ADR |
| Testing | [04-testing.md](04-testing.md) | Active |
| Testing | [50-traceability.md](50-traceability.md) | Active |
| Deployment | [10-runbook.md](10-runbook.md) | Active |
| Maintenance | [90-changelog.md](90-changelog.md) | Active |

## How to work here

1. Any change starts in a specification, never in the code. New feature → `specs/NNN-feature-name/` with `spec.md` (EARS), `plan.md`, `tasks.md`.
2. Stable requirements get a `REQ-*` entry in `01-requirements.md`.
3. Significant decisions become ADRs under `30-decisions/`, status `Proposed`. Only the owner approves.
4. One task at a time, test first. A requirement with no test is not done; `50-traceability.md` maps each one.
5. Session handoff: update the `Session state` block of the active spec before ending a session.
