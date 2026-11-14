# Changelog

## Unreleased

### Added
- Migration control panel served by the gateway at `/_modernizer`: progress ring, parity and latency KPIs, live
  traffic feed, migration board with one-click mode changes, audit inspector with side-by-side diff. Light and dark
  themes, English and Spanish.
- Configuration-driven migration routes with four modes (`Legacy`, `Shadow`, `Canary`, `Modern`) and runtime switching.
- Real shadow running over HTTP: buffered request replay, response capture, tolerant JSON comparison (casing, aliases,
  ignored fields, numeric strings, decimal tolerance), execution-error handling.
- Cutover readiness policy (minimum samples, parity threshold, zero errors).
- In-memory audit store with per-route summaries, percentiles and server-sent events.
- Demo host that runs legacy, modern and gateway in one process; deterministic demo data shared by both backends,
  including an intentional VAT rounding difference caught by the shadow audit.
- Integration tests that boot the gateway against in-memory legacy and modern servers.
- Installable demo (REQ-INS-01 to 05): web app manifest, install icons and iOS tags so the demo installs from
  Android Chrome and iOS Safari. No service worker. The published-size budget (REQ-DEMO-13) is now 220 KB.

### Changed
- Static demo discoverability baseline (REQ-DEMO-31): `SoftwareApplication` JSON-LD, Twitter card tags, author
  link, `robots.txt`, `sitemap.xml` (root only; the sample reports stay `noindex`) and `llms.txt`, all published
  by the build. `og:site_name` is now the product name and the size budget is 210 KB.
- Static demo (`demo/`) redesigned: Spanish by default with English as the second option, light and
  dark themes following the system preference, NainDev top bar and footer, the four route modes as
  controls that drive the diagram (shadow replay drawn), a components section, a self-hosted brand
  typeface and an Open Graph card. Language and theme persist for the visitor. Tests cover the new
  requirements (REQ-DEMO-20 to 23) and the contrast of both themes. A migration board shows the five
  demo routes as configured in the gateway, each on its stage (REQ-DEMO-24). On phones the diagram
  turns vertical instead of scrolling sideways (REQ-DEMO-25); section navigation in the top bar; the
  four steps select the matching mode; panels rise into view; CSS is minified at build time.
- Static demo: the stored theme is applied before the first paint (no flash for visitors who chose the other
  theme); print view of the stage list fixed; English and light-theme layouts checked on the same device matrix.
- Static demo, layout: verified on a device matrix from 320 px phones to 2560 px desktops, including Android
  tablets and landscape phones; narrow-width overflows, tablet orphan cards and the 1024 px top bar fixed.
- Static demo, adoption: a "why" strip under the hero, the cut-over rule with the configured values,
  a setup generator that writes the `Modernizer` section, the `nain-parity` command and a GitHub
  Actions job from the visitor's addresses and route (validated against `ModernizerOptions.cs` and
  the CLI usage by tests), getting-started steps and the known limits (REQ-DEMO-26, 27). The size
  budget is now 200 KB. The generator also writes a Compose file. A guided tour plays the four modes
  with a caption each (REQ-DEMO-28); a rewrite-versus-route comparison and a FAQ for decision makers
  (REQ-DEMO-29). The shared NainDev ecosystem bar is loaded from www.naindev.com as the only external
  resource; the page lays out without it (REQ-DEMO-30, ADR accepted by the owner).
- `Dockerfile` (multi-stage, official .NET 10 images, port 8080) and `compose.yaml` example configured by
  environment variables; runbook gains a containers section and an adoption checklist. Not yet built
  with a Docker daemon.
- CI builds the gateway image and smoke-tests its liveness endpoint from the running container; `nain-parity`
  is packed as a dotnet tool artifact on every run. The runbook documents the tool and the `--json` summary.
- Gateway liveness endpoint `GET /_modernizer/health` for orchestrators (REQ-CP-06).
- `nain-parity --timeout <seconds>` bounds each request (REQ-PAR-19). CI actions pinned by commit.
- `nain-parity --lang es` writes the report in Spanish (REQ-PAR-18); the demo generator adds it when the page is in
  Spanish, and the demo links Spanish sample reports when the page is in Spanish.
- `nain-parity --header "Name: value"` (repeatable) sends a header to both systems, so authenticated APIs can
  be compared; malformed values are rejected (REQ-PAR-17).
- Parity report (`nain-parity` and the sample reports): stylesheet aligned with the brandbook v1.6
  tokens, dark mode by system preference, print-safe. Markup and wording unchanged; the committed
  samples carry the same stylesheet.
- The Pages workflow runs Lighthouse against the built demo and fails under performance 90, accessibility 100,
  best practices 95 or SEO 100.
- Control panel audited with Lighthouse against a stub API: accessibility 100 (secondary text contrast,
  accessible names of the language and export buttons), no layout shift at load (the embedded markup now
  ships the Spanish strings, tiles and counters reserve their height), document title per language.
- Control panel layout checked on the same device matrix with stub data: the top bar wraps instead of
  overflowing below 1200 px, nothing exceeds the viewport on 320 px phones, stage counters in two columns there.
- Control panel: Spanish by default (the visitor's choice persists) and the NainDev isotype as its logo,
  replacing the previous gradient mark.
- Gateway routing moved from static YARP configuration to per-request forwarding (ADR: per-request routing over
  YARP's forwarder, number pending).
- Legacy and modern demo backends expose five slices (`invoices`, `orders`, `customers`, `catalog`, `pricing`).
- The shadow replay comparer is now `ShadowParityComparer`; `JsonParityComparer` keeps serving the `nain-parity`
  CLI. Both coexist until a decision unifies them (see `docs/02-architecture.md`).
- The sample parity reports are regenerated from the shared demo data: the not-ready report shows the stringified
  legacy total and the VAT rounding difference on `CUST-501`; the ready report covers `catalog` and `pricing`.

## 0.1.0

- Initial scaffold: YARP gateway, legacy and modern stand-ins, in-process shadow auditor, documentation skeleton.
