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

### Changed
- Static demo (`demo/`) redesigned: Spanish by default with English as the second option, light and
  dark themes following the system preference, NainDev top bar and footer, the four route modes as
  controls that drive the diagram (shadow replay drawn), a components section, a self-hosted brand
  typeface and an Open Graph card. Language and theme persist for the visitor. Tests cover the new
  requirements (REQ-DEMO-20 to 23) and the contrast of both themes. A migration board shows the five
  demo routes as configured in the gateway, each on its stage (REQ-DEMO-24). On phones the diagram
  turns vertical instead of scrolling sideways (REQ-DEMO-25); section navigation in the top bar; the
  four steps select the matching mode; panels rise into view; CSS is minified at build time.
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
