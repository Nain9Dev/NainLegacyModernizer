# Spec: Parity report and CLI

Status: Draft. Number assigned by the merge train (`next-parity-report`).

## Goal

Make the shadow run usable on any pair of HTTP APIs, not only on the in-repo demo services. A team
points the tool at its legacy system and its new implementation, and gets a visual pass/fail report
that says whether a route is ready to cut over. The same command works as a CI gate.

This builds on `REQ-SHD-01` (parity auditor) and the charter objective "Mathematical Parity
Verification". No new runtime dependency: the .NET base library only.

## Scope

- Pure report models in `Core`, comparison and rendering in `ShadowRunner`, a console tool
  `NainLegacyModernizer.ParityCli`.
- Read-only: the tool sends `GET` requests only.

## Non-goals (v1)

- No authentication headers, no request bodies, no write calls (they could change data on a live system).
- No latency or load measurements: one request per side is not a benchmark.
- No hosted service and no persistence of reports.

## Requirements (EARS)

### Comparison

- **REQ-PAR-01** When both responses are JSON, the comparer shall flatten them to field paths
  (`a.b`, `items[0].sku`) and report one `DiscrepancyDetail` per path whose values differ.
- **REQ-PAR-02** The comparer shall compare field names ignoring case, `_` and `-`
  (`OrderId`, `order_id` and `orderId` are the same field).
- **REQ-PAR-03** Where the user maps a legacy name to a modern name (`OrderID=id`), the comparer shall
  treat them as the same field.
- **REQ-PAR-04** Where the user lists a field to ignore, the comparer shall skip it on both sides.
- **REQ-PAR-05** When two numbers are equal as decimals (`450.5` and `450.50`), the comparer shall treat
  them as equal, and shall not use floating point (NFR-02).
- **REQ-PAR-06** If a field exists on one side only, the comparer shall report it with the value
  `<missing>` on the other side.
- **REQ-PAR-07** If the two HTTP status codes differ, the runner shall report a discrepancy on the
  pseudo field `$status`.
- **REQ-PAR-08** If a request fails, times out, or a body is not JSON, the runner shall record the route
  as `ExecutionError` with the reason, and shall continue with the next route.

### Report

- **REQ-PAR-09** The report shall count routes as matched, discrepant and failed.
- **REQ-PAR-10** The report shall be `ready to cut` only when it has at least one route and every route
  matched (fail closed).
- **REQ-PAR-11** The HTML renderer shall produce one self-contained file: no scripts, no external
  resources, every dynamic value HTML-escaped.
- **REQ-PAR-12** The HTML report shall lead with a verdict banner and a proportional bar of
  matched, discrepant and failed routes, then one card per route with the differing fields side by
  side. State shall not rely on colour alone, and text contrast shall meet WCAG 2.2 AA.

### CLI

- **REQ-PAR-13** The CLI shall accept `--legacy`, `--modern`, `--paths` (file with one path per line, or a
  comma separated list), and optional `--ignore`, `--map`, `--out` and `--json`.
- **REQ-PAR-14** The CLI shall exit with `0` when the report is ready to cut, `1` when it is not, and
  `2` on invalid arguments.
- **REQ-PAR-15** When `--out` is omitted, the CLI shall write `parity-report.html` in the current
  directory.

### Evidence

- **REQ-PAR-16** The repository shall include a sample report produced by running the CLI against the
  in-repo `LegacyMonolith` and `ModernApi`, and the demo shall link to it.

## Verification

xUnit tests in `tests/NainLegacyModernizer.Tests` for REQ-PAR-01 to 15, including an integration test
over `WebApplicationFactory` for both services. REQ-PAR-16 by regenerating the sample in CI and
diffing it.
