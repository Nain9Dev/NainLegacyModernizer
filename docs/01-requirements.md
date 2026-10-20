# Requirements Specification: NainLegacyModernizer

This document defines the functional and non-functional requirements for the modernization harness using the **EARS** (Easy Approach to Requirements Syntax) pattern.

## 1. Functional Requirements

### REQ-STR-01: Reverse Proxy Route Carve-Out (Strangler Fig)
- **Statement**: When an incoming HTTP request targets `/api/orders/*`, the YARP Gateway shall forward the request directly to the `.NET 10 ModernApi` destination cluster.
- **Acceptance Criteria**:
  - `GET /api/orders/{id}` returns HTTP 200 with standard camelCase JSON from ModernApi.
  - HTTP headers are preserved without leakage of internal gateway topology.

### REQ-STR-02: Catch-All Legacy Monolith Fallback
- **Statement**: When an incoming HTTP request targets any route other than migrated vertical slices (e.g., `/api/invoices/*`), the YARP Gateway shall forward the request to the `LegacyMonolith` destination cluster.
- **Acceptance Criteria**:
  - `GET /api/invoices/{id}` returns HTTP 200 from the simulated legacy monolith.
  - Legacy response formats (PascalCase / XML compatibility) are preserved for unmigrated clients.

### REQ-SHD-01: Shadow Running Parity Auditor
- **Statement**: While operating in shadow audit mode, the `ShadowAuditor` shall execute equivalent requests against both Legacy and Modern pipelines, compare the computed domain aggregates, and record any discrepancy.
- **Acceptance Criteria**:
  - Identical outputs yield `ParityVerdict.Matched` with 0 mismatches.
  - Value divergence (e.g., rounding differences or field omissions) yields `ParityVerdict.Discrepancy` with detailed field-level diffs.

### REQ-PAR: Parity Report and CLI

- **Statement**: When a team runs `nain-parity` against a legacy and a modern base URL, the tool shall send the same `GET` requests to both, compare the JSON responses, and write a self-contained HTML report that states whether every route is ready to cut over.
- **Acceptance Criteria**:
  - Field names match ignoring case, `_` and `-`; `--map` aligns renamed fields; `--ignore` skips fields on both sides.
  - Numbers compare as decimals (NFR-02); a field present on one side only is a discrepancy.
  - A request failure or a non-JSON body is an `ExecutionError` for that route and the run continues.
  - An empty report is never ready (fail closed). Exit codes: `0` ready, `1` not ready, `2` invalid arguments.
  - Detailed requirements `REQ-PAR-01` to `REQ-PAR-16`: `specs/next-parity-report/spec.md`.

## 2. Non-Functional Requirements

- **NFR-01: Framework Modernity**: Modern services and gateway target `.NET 10` with C# 13 and strict nullable annotations.
- **NFR-02: Determinism**: Decimal financial calculations must match exact pennies with zero floating-point drift.
- **NFR-03: Zero External Dependencies**: Demonstrator runs standalone in memory via `WebApplicationFactory` for CI verification.
