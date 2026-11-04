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
| T-016 | A | Vertical diagram on phones, section navigation, steps linked to the modes, scroll reveal with a fallback, CSS minified at build time, README preview | `npm test` green, Lighthouse run on the built folder |
| T-017 | A | Shadow flow strip, print styles, hreflang links, JavaScript compaction at build time, parity report stylesheet aligned with the brand (renderer and samples) | `npm test` green; `dotnet test` pending (SampleReportTests compares the samples with the renderer) |
| T-018 | A | Adoption: why strip, cut-over rule, setup generator (settings, command, CI job), getting-started steps and limits; budget raised to 200 KB | `npm test` green, generator driven in a browser |
| T-019 | A | Dockerfile, compose.yaml, Compose output in the generator, runbook containers section and adoption checklist | `npm test` green |
| T-021 | A | Guided tour from the hero, rewrite-versus-route comparison, FAQ for decision makers | `npm test` green, tour driven in a browser with and without reduced motion |
| T-023 | A | `nain-parity --header` (C#, tests written, not compiled here) and the authentication option in the generator | `dotnet test` green on the owner's machine |
| T-024 | A | paths.txt output in the generator; gateway liveness endpoint `/_modernizer/health` (C#, test written, not compiled here) | `npm test` green; `dotnet test` on the owner's machine |
| T-025 | A | `nain-parity --lang es` (Spanish report wording) and the generator adds it on the Spanish page | CI green on the pull request |
| T-026 | A | `nain-parity --timeout`, CI actions pinned by commit, CI badges in the READMEs | CI green on the pull request |
| T-027 | A | nain-parity packed as a dotnet tool artifact; runbook covers the tool and the JSON summary | CI green, artifact attached to the run |
| T-028 | H | Decide whether CI publishes the gateway image to GitHub Container Registry (public package) | Owner decision |
| T-029 | A | Device matrix check (Android phones 320 to 412 px, landscape, Android tablets 712 and 800 px, laptops 1024 to 1366 px, desktops 1920 and 2560 px): no horizontal overflow, no orphan cards, top bar fits | Matrix script reports no element outside the viewport |
| T-030 | A | Control panel on the device matrix (stub API with five routes, traffic and audits): top bar, narrow widths | Matrix script reports no element outside the viewport |
| T-031 | A | Lighthouse gate in the Pages workflow (performance 90, accessibility 100, best practices 95, SEO 100) | Step green on the pull request |
| T-032 | A | Control panel Lighthouse: contrast, accessible names, no layout shift, Spanish markup defaults | Accessibility 100, CLS under 0.05 against the stub |
| T-020 | A | CI job builds the image and checks `/_modernizer/health` from the running container | Job green on the pull request |
| T-014 | H | Decide on the ecosystem bar (ADR `next-demo-ecosystem-bar`) | Done: owner chose the shared bar (2026-11-03) |
| T-022 | A | Load the shared ecosystem bar; layout unaffected when the script does not load | `npm test` green, both states rendered in a browser |
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
- Done and verified: T-016. `npm test` 31/31 green, `dist/` about 147 KB. Lighthouse 12 (mobile
  preset, Chromium, local server without compression): performance 99, accessibility 100, best
  practices 100, SEO 100; LCP 1.8 s, CLS 0, TBT 30 ms. The items it flags (text compression, cache
  headers) are served by GitHub Pages, not by the files.
- Done: T-017. `npm test` 32/32 green, `dist/` about 149 KB. The parity report stylesheet was changed
  only inside the `Style` constant of `ParityHtmlRenderer.cs` and copied verbatim into the two
  samples, so `SampleReportTests` should still match; this could not be run here (no .NET SDK).
  Report screenshots in `docs/images` regenerated from the samples.
- Done and verified: T-018. `npm test` 40/40 green, `dist/` about 172 KB (budget raised to 200 KB in
  REQ-DEMO-13 to make room for the generator; still no external asset). Playwright: generator edited,
  canary field appears with the mode, JSON parses with the expected route block, command and workflow
  switch, copy writes the clipboard, an invalid URL shows its error and disables copying.
- Done: T-019. `npm test` 41/41 green. The Compose output is checked against the committed
  `compose.yaml` example. The Dockerfile could not be built here (no Docker daemon): T-020 is the owner's
  first check together with `dotnet test`.
- Done and verified: T-021. `npm test` 43/43 green, `dist/` about 185 KB. Playwright: the tour steps
  Legacy, Shadow, Canary, Modern every 3.5 s with its caption, a manual mode click ends it, and with
  reduced motion the same button advances one step per activation.
- Done and verified: T-022. The bar was rendered in Playwright from a local copy of the site's script and
  data (online) and with the request blocked (offline): the top bar sits at 38 px or at 0 accordingly,
  no console errors either way. PR opened: Nain9Dev/NainLegacyModernizer#5.
- Next: T-020 and `dotnet test` (owner, first thing), T-012 if still pending; a `nainlegacymodernizer`
  profile in the site's `ecosystem.v1.json` so the bar's architecture drawer stops falling back to NainOrder.
