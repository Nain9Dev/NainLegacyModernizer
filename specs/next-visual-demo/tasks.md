# Tasks: Static visual demo

| ID | Tag | Task | Done when |
| :--- | :--- | :--- | :--- |
| T-001 | A | Spec, plan, ADR (Proposed) | Files reviewed |
| T-002 | A | Failing tests for routing, parity, i18n, contrast, budget, static rules | `npm test` red for the right reasons |
| T-003 | A | Implement `routing.ts`, `parity.ts`, `fixtures.ts`, `i18n.ts`, `contrast.ts` | Logic tests green |
| T-004 | A | Page, styles, SVG diagram, shadow panel, walkthrough | Static-rule tests green, checked in browser |
| T-005 | A | Pages workflow, zizmor and actionlint clean | Both tools report no findings |
| T-006 | A | README link | Link present |
| T-007 | A | 640x360 WebP card image under 30 KB | File exists, size checked |
| T-008 | A | gitleaks clean before proposing the commit | No leaks |
| T-009 | H | Enable GitHub Pages with source "GitHub Actions" in repository settings | Settings page shows the source |
| T-010 | H | Approve ADR (Proposed to Approved) | ADR status changed by the owner |
| T-011 | M | Push `feat/visual-demo`, open the PR, merge, confirm the published URL | URL answers 200 |

## Blockers

- T-009: needs repository admin rights. Without it the workflow's deploy job fails at
  `configure-pages`. Verify with `gh api repos/Nain9Dev/NainLegacyModernizer/pages`.

## Session state

Started 2026-10-20.

- Done and verified: T-001 to T-008. `npm test` 18/18 green, `npm run typecheck` clean, `dist/` is about
  32 KB, zizmor and actionlint clean on `pages.yml`, gitleaks clean, card image
  `docs/images/demo-card.webp` (640x360, 17 KB). Checked by hand in a browser: both languages, slider,
  scenarios, layout.
- Not verified: keyboard walk-through with a screen reader, animation at full frame rate (the preview
  pane throttled requestAnimationFrame), real phone widths.
- Next: T-009, T-010, T-011 (owner).
- Known, out of scope: `ci.yml` uses unpinned actions (zizmor reports them).
