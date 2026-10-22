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
- Gateway routing moved from static YARP configuration to per-request forwarding (ADR-0001).
- Legacy and modern demo backends expose five slices (`invoices`, `orders`, `customers`, `catalog`, `pricing`).

## 0.1.0

- Initial scaffold: YARP gateway, legacy and modern stand-ins, in-process shadow auditor, documentation skeleton.
