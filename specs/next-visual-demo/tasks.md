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

| T-013 | A | Visual redesign: Spanish default, light and dark themes, NainDev chrome, mode controls, components section, Open Graph card | `npm test` green, browser check in both themes and languages |
| T-015 | A | Migration board from the gateway configuration, isotype draw-in, control panel Spanish-first with the brand mark | `npm test` green, panel rendered against a stub API |
| T-014 | H | Decide on the ecosystem bar (ADR `next-demo-ecosystem-bar`) | ADR status changed by the owner |
| T-012 | H | Set the Pages custom domain `modernizer.naindev.com` (`gh api -X PUT repos/Nain9Dev/NainLegacyModernizer/pages -f cname=modernizer.naindev.com`), then enable HTTPS enforcement once the certificate is issued | `curl -sI https://modernizer.naindev.com/` answers 200 |

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

Redesign session.

- Done and verified: T-013. `npm test` 25/25 green, `npm run typecheck` clean, `dist/` about 139 KB
  (font 25 KB, card 33 KB). Playwright check: no console errors; Spanish by default; English via the
  control and via `?lang=en`; theme and language survive a reload; the four modes drive the diagram
  (shadow replay drawn, ghost packet); discrepancy scenario marks the field on both sides; reduced
  motion hides the pause control and draws no packets; keyboard focus order starts at the skip link.
- Not verified: screen reader walk-through, real phone hardware (phone width checked in Playwright),
  the live URL after the Pages deploy.
- Done and verified: T-015. `npm test` 27/27 green, `dist/` about 145 KB. The board is checked against
  `appsettings.json` by `routes.test.ts`. The control panel was rendered in Playwright against a stub
  of `/_modernizer/api/overview`: Spanish by default, brand logo, no console errors. `dotnet test` was
  not run in this session (no .NET SDK available); the panel change is limited to its embedded
  `index.html`, `app.js` and `logo.svg`.
- Next: T-014 (owner), T-012 if still pending, `dotnet test` on a machine with the SDK.
