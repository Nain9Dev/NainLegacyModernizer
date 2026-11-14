# Spec: Static visual demo

Status: Draft. Number assigned by the merge train (`next-visual-demo`).

## Goal

A static, backend-free page that lets a non-technical visitor understand in about 10 seconds what the
project does: traffic moves from a legacy monolith to a modern API one route at a time, and a shadow
run proves both answer the same before the cut-over.

## Scope

- One entry file (`demo/index.html`), CSS and TypeScript compiled to ES modules. No runtime dependencies.
- Hosted on GitHub Pages from this repository.
- Visible text in Spanish (default) and English. Everything else in the repository is English.

## Non-goals

- No claims about performance, customers, sectors or results. Nothing is measured by the demo.
- No live calls to the .NET services; the page never talks to a backend.
- No weighted traffic splitting claim: the gateway in this repository routes by path
  (`src/NainLegacyModernizer.Gateway/appsettings.json`). The slider is an illustration, and the page says so.

## Requirements (EARS)

### Strangler Fig diagram

- **REQ-DEMO-01** The page shall show four nodes: client, YARP gateway, modern API (.NET 10) and legacy monolith.
- **REQ-DEMO-02** When the visitor moves the "migrated traffic" slider (0 to 100), the page shall update
  the share of `/api/orders/*` requests drawn towards the modern API and the rest towards the legacy monolith.
- **REQ-DEMO-03** The `/api/invoices/*` route shall always be drawn towards the legacy monolith, because
  it is not carved out in the repository.
- **REQ-DEMO-04** While the slider is at 0 the `/api/orders/*` route shall be labelled legacy; while it is
  at 100 it shall be labelled migrated; in between it shall be labelled in transition. State shall never
  rely on colour alone (text label and pattern too).
- **REQ-DEMO-05** The assignment of request number *i* to a target shall be deterministic, and over 100
  consecutive requests exactly *p* shall go to the modern API for a slider value *p*.

### Shadow run panel

- **REQ-DEMO-06** The page shall show the legacy and the modern response side by side for a selected
  scenario, using only the synthetic fixtures from `tests/NainLegacyModernizer.Tests/ShadowAuditorTests.cs`
  (`ORD-001` equal at 100.00 EUR, `ORD-002` legacy 99.99 against modern 100.00).
- **REQ-DEMO-07** When a compared field differs, the page shall highlight it on both sides with a
  non-colour marker and list it with both values.
- **REQ-DEMO-08** The parity indicator shall read `Matched` when no compared field differs and
  `Discrepancy` otherwise (the `ParityVerdict` names in `NainLegacyModernizer.Core`). It shall show the
  count of compared and differing fields, and no percentage.

### Four-step walkthrough

- **REQ-DEMO-09** The page shall present four steps (carve-out, shadow run, compare, cut over), each with
  an SVG icon and at most one short sentence.

### Quality

- **REQ-DEMO-10** The demo shall be fully operable by keyboard with a visible focus indicator.
- **REQ-DEMO-11** While `prefers-reduced-motion: reduce` is set, the page shall not animate; the diagram
  shall update statically. Animation shall have a visible pause control otherwise.
- **REQ-DEMO-12** Text and UI component contrast shall meet WCAG 2.2 AA, verified by a test over the
  tokens in use.
- **REQ-DEMO-13** The published folder shall weigh less than 220 KB in total (210 KB until the install icons of REQ-INS-02 were added, 150 KB until the setup
  generator was added, 200 KB until the discovery files of REQ-DEMO-31 were added), with no external fonts, styles or images and no external script other than
  the shared NainDev ecosystem bar (REQ-DEMO-30). The brand typeface (Urbanist, SIL Open Font License) is self-hosted with its
  licence file.
- **REQ-DEMO-14** The CSS shall use the brandbook tokens (`Nain9Dev/brandbook`, `tokens.css` v1.6) and
  no gradients beyond the one-pixel blueprint grid.
- **REQ-DEMO-15** The page shall carry `lang` set to the active language and every Spanish string shall
  have an English counterpart.

### Language, theme and brand

- **REQ-DEMO-20** The page shall open in Spanish (es-ES, professional register, short sentences). A
  visible control shall switch to English; the choice shall persist for the visitor and a `lang=en` query
  value shall open the English version directly. No visible string shall exceed 140 characters.
- **REQ-DEMO-21** The page shall offer a light theme and a dark theme. The system preference applies
  by default, a visible control switches it, and the choice persists. Both themes shall meet REQ-DEMO-12.
- **REQ-DEMO-22** The page shall present the four route modes of the gateway (`Legacy`, `Shadow`,
  `Canary`, `Modern`) as selectable states that drive the diagram: `Shadow` draws the replay to the
  modern API, `Canary` sets the slider to a partial share, `Modern` to 100.
- **REQ-DEMO-23** The page shall carry the NainDev mark, the author's name and links to naindev.com,
  the repository and the brandbook, and no trace of any generator or assistant tool.
- **REQ-DEMO-24** The page shall show a migration board with the routes of the repository demo exactly
  as configured in the gateway's `appsettings.json` (id, pattern, mode, canary share), each on a
  four-stage track, with a readiness wording derived from the mode and the documented finding. No
  measured figure (samples, parity ratio, latency) shall be shown.
- **REQ-DEMO-25** On screens narrower than 640 px the diagram shall switch to a top-to-bottom layout
  that fits the viewport without horizontal scrolling; every node shall stay inside the view and each
  wire shall start and end on a node edge in both layouts.

### Adoption

- **REQ-DEMO-26** The page shall let a visitor enter the two base addresses, one route, the initial
  mode, the canary share and the ignored and renamed fields, and shall produce from them the
  `Modernizer` section of the gateway's `appsettings.json`, the `nain-parity` command, a GitHub
  Actions job and a Compose file (the same settings as environment variables), each copyable. Every generated property name shall exist in `ModernizerOptions.cs` and
  every CLI option in the CLI usage text; invalid input shall be flagged next to the field and shall
  disable copying. Nothing shall be sent anywhere.
- **REQ-DEMO-27** The board shall state the cut-over rule with the values configured in the demo
  (minimum samples, minimum parity ratio, zero execution errors), and the page shall list the known
  limits of the harness (HTTP and JSON only, GET-only report without authentication headers,
  in-memory audits, panel without login).
- **REQ-DEMO-28** A control in the hero shall play a guided tour that walks the orders route through
  the four modes in order with a one-sentence caption per step; any manual choice on the diagram shall
  end it. While `prefers-reduced-motion: reduce` is set the same control shall advance one step per
  activation instead of on a timer.
- **REQ-DEMO-29** The page shall contrast a full rewrite with route-by-route migration on downtime,
  rollback, evidence, delivery size and timing of risk, without figures, and answer the questions a
  decision maker asks first (cost and licence, prerequisites, impact on code and data, data leaving the
  network, failure of the new API in shadow, .NET Framework monoliths).
- **REQ-DEMO-30** The page shall load the shared NainDev ecosystem bar from `www.naindev.com`
  (deferred, naming this project) as its only external resource, and shall lay out correctly when that
  script does not load (ADR `next-demo-ecosystem-bar`, Accepted).

- **REQ-DEMO-31** The published site root shall carry the search and AI-engine discoverability baseline:
  one canonical link equal to `https://modernizer.naindev.com/`, one meta description of 70 to 160
  characters, Open Graph and Twitter card metadata whose URL equals the canonical, an author link,
  `SoftwareApplication` JSON-LD naming the author by `@id`, a `robots.txt` that allows everything and
  advertises the sitemap, a `sitemap.xml` that lists only the root, and an English `llms.txt`. The
  sample parity reports are `noindex` and shall appear in neither the sitemap nor `llms.txt`. The
  build shall publish the three root files.

- **REQ-DEMO-19** The shadow run panel shall link to the two sample parity reports (not ready, ready)
  generated by `SampleReportTests` from the in-repo services (`specs/next-parity-report`, REQ-PAR-16),
  in the wording of the active page language (English and Spanish files), and the build shall publish
  them with the demo.

### Delivery

- **REQ-DEMO-16** A GitHub Actions workflow shall build, test and publish `demo/dist` to GitHub Pages on
  push to `main`, and shall only build and test on pull requests.
- **REQ-DEMO-17** The README shall link to the demo.
- **REQ-DEMO-18** The demo shall be served at `https://modernizer.naindev.com/`, through GitHub Pages
  behind a Cloudflare-proxied subdomain, at no cost (ADR `next-demo-hosting-subdomain`, Proposed).

## Verification

Node tests in `demo/test/` (`npm test`) cover REQ-DEMO-05, 08, 12, 13, 14, 15, 20, 21, 22, 23, 24, 25, 26, 27, 28 and static
checks for 11 and 16. Browser check (Playwright script, both themes, both languages, phone and desktop
widths) for 01 to 04, 06, 07, 09 to 11.
