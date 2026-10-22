# Requirements Specification: NainLegacyModernizer

Requirements follow the **EARS** pattern. Each one is traced to a test in [50-traceability.md](50-traceability.md).

## 1. Functional requirements

### Routing (strangler fig)

- **REQ-STR-01 Configuration-driven routes**: When the gateway starts, it shall load migration routes (path pattern, mode, canary percentage, field aliases, ignored fields, sample requests) from the `Modernizer` configuration section.
- **REQ-STR-02 Legacy mode**: When a request matches a route in `Legacy` mode, the gateway shall forward it to the legacy backend only.
- **REQ-STR-03 Modern mode**: When a request matches a route in `Modern` mode, the gateway shall forward it to the modern backend only.
- **REQ-STR-04 Canary mode**: When a request matches a route in `Canary` mode, the gateway shall forward the configured percentage of requests to the modern backend and the rest to legacy.
- **REQ-STR-05 Catch-all fallback**: When no route matches, the gateway shall forward the request to the legacy backend and record it under the `legacy-fallback` route id.
- **REQ-STR-06 Runtime mode switch**: When an operator changes a route mode through the control panel API, the new mode shall apply to the next request without a restart.
- **REQ-STR-07 Diagnostic headers**: When a proxied response is sent, the gateway shall add `X-Modernizer-Mode` and `X-Modernizer-Target` headers unless `EmitDiagnosticHeaders` is false.

### Shadow running (parity)

- **REQ-SHD-01 Shadow replay**: When a request matches a route in `Shadow` mode, the gateway shall serve the legacy response to the caller, replay the same method, path, headers and body against the modern backend and record a parity audit.
- **REQ-SHD-02 Tolerant JSON comparison**: When comparing bodies, the comparer shall ignore property-name casing, apply field aliases, skip ignored fields, treat numeric strings as numbers and honour the configured decimal tolerance; array length and status-code differences are reported as discrepancies.
- **REQ-SHD-03 Execution errors**: If a shadow call throws, times out or exceeds the capture limit, the audit shall carry verdict `ExecutionError` and the caller shall still receive the primary response.
- **REQ-SHD-04 Cutover readiness**: When a route has at least `MinimumSamples` audits, zero execution errors and a parity ratio at or above `MinimumParityRatio`, it shall be reported as `Ready`; a route already in `Modern` is `Completed`; a route in `Legacy` with no audits is `Collecting` with guidance.

### Control panel

- **REQ-CP-01 Overview**: When the panel requests `/_modernizer/api/overview`, the gateway shall return every route with mode, summary (samples, parity ratio, legacy and modern p50/p95 latency) and readiness, plus global totals.
- **REQ-CP-02 Live stream**: While a panel is connected to `/_modernizer/api/stream`, the gateway shall push each traffic sample and audit as a server-sent event.
- **REQ-CP-03 Audit inspection**: When the panel requests an audit by id, the gateway shall return both observations (status, latency, body or error) and the field-level discrepancies.
- **REQ-CP-04 Embedded panel**: When `/_modernizer` is requested, the gateway shall serve the control panel from resources embedded in its own assembly, with no build step or external asset.
- **REQ-CP-05 Demo traffic**: When the operator triggers demo traffic, the panel shall replay the configured sample requests through the gateway itself.

## 2. Non-functional requirements

- **NFR-01 Framework modernity**: Gateway, modern API and demo target .NET 10 with nullable and analyzers enforced (warnings are errors).
- **NFR-02 Determinism**: Financial comparisons use `decimal`; the bundled demo data is deterministic per id on both backends.
- **NFR-03 Zero external dependencies**: The whole suite runs in memory (`WebApplicationFactory`, no sockets); the demo runs with one command and no services.
- **NFR-04 Reusability**: Pointing the harness at a client's systems requires only the `Modernizer` configuration section.
- **NFR-05 Accessibility and themes**: The panel offers light and dark themes, English and Spanish, keyboard-reachable controls and respects `prefers-reduced-motion`.

## 3. Known limitations (documented, not defects)

- Mode changes and audits live in memory; a restart returns to the configured modes.
- The control panel has no authentication: deploy it behind the client's network boundary.
- Shadow replay of non-idempotent routes needs the client's explicit sign-off.
