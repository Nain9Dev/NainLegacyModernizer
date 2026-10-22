# Specification: Migration Control Panel

Status: Draft

## Objective

Turn the harness into a reusable, configuration-driven product: a YARP gateway that moves each route through
`Legacy -> Shadow -> Canary -> Modern`, audits parity between both backends on real HTTP traffic, and exposes a
visual control panel so a client can watch and drive the migration without reading code.

## Functional requirements (EARS)

- **FR1**: When the gateway starts, the system shall load the migration routes (path pattern, mode, canary
  percentage, sample requests) from the `Modernizer` configuration section instead of hard-coded YARP routes.
- **FR2**: When a request matches a route in `Legacy` mode, the system shall forward it to the legacy backend only.
- **FR3**: When a request matches a route in `Modern` mode, the system shall forward it to the modern backend only.
- **FR4**: When a request matches a route in `Canary` mode, the system shall forward the configured percentage of
  requests to the modern backend and the rest to the legacy backend.
- **FR5**: When a request matches a route in `Shadow` mode, the system shall serve the legacy response to the caller,
  replay the same request against the modern backend, compare both JSON bodies and record a parity audit.
- **FR6**: When no route matches, the system shall forward the request to the legacy backend (catch-all fallback).
- **FR7**: When comparing JSON bodies, the system shall ignore property-name casing, apply configured field aliases
  (legacy name -> modern name), ignore configured fields and tolerate a configured decimal difference.
- **FR8**: If one of the two shadow calls throws or times out, then the system shall record the audit with verdict
  `ExecutionError` and the caller shall still receive the primary response.
- **FR9**: When a route has at least the configured minimum of shadow samples and its parity ratio reaches the
  configured threshold with zero execution errors, the system shall mark it `Ready for cutover`.
- **FR10**: When an operator changes a route mode from the panel, the system shall apply the new mode to the next
  request without restarting the gateway.
- **FR11**: While the panel is open, the system shall stream traffic samples and audits to it in real time (SSE).
- **FR12**: When the panel loads, the system shall show, per route: mode, sample count, parity ratio, latency of
  legacy vs modern, readiness, and the latest discrepancies with a side-by-side field diff.
- **FR13**: When the operator triggers the demo traffic generator, the panel shall replay the route sample requests
  through the gateway so the dashboard fills with data without external tooling.
- **FR14**: When every request is answered, the gateway shall add `X-Modernizer-Mode` and `X-Modernizer-Target`
  response headers (can be disabled by configuration).

## Edge cases and error handling

- Request bodies are buffered so the shadow replay sends the exact same payload.
- Shadow replay never blocks the caller: the primary response is flushed first, the comparison runs afterwards.
- Response bodies larger than `MaxCapturedBodyBytes` are not compared; the audit is recorded as `ExecutionError`
  with a reason, never silently matched.
- Non-JSON bodies are compared byte-for-byte after trimming; a diff is reported as a single `$body` discrepancy.
- Mode changes are kept in memory. A restart returns to the configured mode (documented limitation).

## Out of scope

- Persisting audits or mode changes to a database.
- Authentication for the control panel (deploy behind the client's own network boundary).
- Request mirroring for non-idempotent routes in production without the client's explicit sign-off.

## Acceptance criteria

- `dotnet test` green; integration tests prove FR2-FR6, FR8, FR10 and FR14 through the gateway host.
- Unit tests prove FR7 and FR9.
- `dotnet run --project src/NainLegacyModernizer.Demo` opens a working panel on a single URL.

## Session state

- Last updated: 2026-10-22
- Done: T01-T07 implemented; 54 tests green; demo verified in the browser (traffic, promotion with confirmation,
  canary slider, audit drawer, light/dark, EN/ES). Requirements consolidated into `docs/01-requirements.md`;
  ADR-0001 and ADR-0002 drafted as Proposed.
- Verified: `dotnet test` (54/54), manual walkthrough on the demo host.
- Next: optional Playwright smoke test for the panel (REQ-CP-02/05); persistence of mode changes if a client asks.
- Blocked: owner approval of ADR-0001 and ADR-0002.
