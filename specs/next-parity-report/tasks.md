# Tasks: Parity report and CLI

| ID | Tag | Task | Done when |
| :--- | :--- | :--- | :--- |
| T-001 | A | Spec, plan | Reviewed |
| T-002 | A | Tests for comparer, report, renderer, runner, CLI options | `dotnet test` red |
| T-003 | A | Core models, comparer, runner | Logic tests green |
| T-004 | A | HTML renderer | Renderer tests green, opened in a browser |
| T-005 | A | CLI project and exit codes | CLI tests green, manual run against both services |
| T-006 | A | Sample report from the in-repo services, linked from the demo | File committed, link test green |
| T-007 | A | Docs: requirements, architecture, runbook, README usage | Updated in the same change |
| T-008 | A | gitleaks, build with zero warnings | Both clean |

## Blockers

None external. No account, key or domain is needed.

## Session state

Started 2026-10-20.

- Done and verified: T-001 to T-008. `dotnet test` 64/64, zero build warnings, demo `npm test` 19/19,
  gitleaks clean. Manual run of `nain-parity` over real HTTP against both in-repo services: exit code 1,
  report and JSON written.
- Found while testing: ignoring a legacy field did not ignore its mapped modern name. Fixed with a test.
- Known: Git Bash rewrites `--paths /a,/b`; documented in the README.
- 2026-10-27: merged with the migration control panel (`specs/next-migration-control-panel`). Both stand-ins now
  share `SampleData`, so the samples were regenerated (`UPDATE_SAMPLES=1`): not ready = `/api/orders/ORD-1`
  (stringified legacy total) and `/api/customers/CUST-501` (VAT rounding); ready = `catalog` and `pricing`.
  The screenshots in `docs/images/parity-*.webp` still show the previous samples and need a retake.
  `DiscrepancyDetail` values are now nullable (the shadow comparer leaves a missing side `null`); the renderer
  prints `<missing>` for them.
- Next (needs a decision): `--header` for authenticated APIs, a request body or POST mode behind an
  explicit opt-in, a `dotnet tool` release.
