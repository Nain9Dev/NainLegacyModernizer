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

## 2. Non-Functional Requirements

- **NFR-01: Framework Modernity**: Modern services and gateway target `.NET 10` with C# 13 and strict nullable annotations.
- **NFR-02: Determinism**: Decimal financial calculations must match exact pennies with zero floating-point drift.
- **NFR-03: Zero External Dependencies**: Demonstrator runs standalone in memory via `WebApplicationFactory` for CI verification.
