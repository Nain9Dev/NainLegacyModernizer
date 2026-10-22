# Tasks: Migration Control Panel

All tasks closed on 2026-10-22; see `spec.md` → Session state.

- [x] **T01** `[A]` Core migration and parity model (`MigrationMode`, `MigrationRoute`, `CutoverPolicy`, summaries).
  - **Covers**: FR1, FR9
  - **Done when**: `CutoverPolicyTests` pass.
- [x] **T02** `[A]` `JsonParityComparer` with casing, aliases, ignored fields and decimal tolerance.
  - **Covers**: FR7
  - **Done when**: `JsonParityComparerTests` pass.
- [x] **T03** `[A]` Fault-tolerant `ShadowAuditor` and `InMemoryAuditStore`.
  - **Covers**: FR5, FR8, FR11, FR12
  - **Done when**: `ShadowAuditorTests` and `InMemoryAuditStoreTests` pass.
- [x] **T04** `[A]` Gateway options, registry and `StranglerProxy` with all four modes and catch-all.
  - **Covers**: FR1-FR6, FR8, FR10, FR14
  - **Done when**: `GatewayRoutingTests` pass against in-memory legacy and modern hosts.
- [x] **T05** `[A]` Control panel API and SSE stream.
  - **Covers**: FR10, FR11, FR13
  - **Done when**: `ControlPanelApiTests` pass.
- [x] **T06** `[A]` Static control panel (`wwwroot/_modernizer`).
  - **Covers**: FR12, FR13
  - **Done when**: the panel renders routes, live traffic, discrepancies and lets the operator switch modes.
- [x] **T07** `[A]` Demo host and runbook.
  - **Covers**: acceptance
  - **Done when**: one command starts legacy, modern and gateway and the panel is reachable.
