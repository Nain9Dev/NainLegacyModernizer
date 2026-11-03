# Traceability: requirement → test

All tests live in `tests/NainLegacyModernizer.Tests`. Run `dotnet test`.

| Requirement | Test |
| :--- | :--- |
| REQ-STR-01 | `GatewayRoutingTests.Overview_ReportsRoutesSummariesAndReadiness` (5 configured routes loaded) |
| REQ-STR-02 | `GatewayRoutingTests.LegacyRoute_IsServedByMonolith_WithDiagnosticHeaders` |
| REQ-STR-03 | `GatewayRoutingTests.ModernRoute_IsServedByModernApi` |
| REQ-STR-04 | `GatewayRoutingTests.CanaryRoute_HonoursPercentage` |
| REQ-STR-05 | `GatewayRoutingTests.UnmatchedPath_FallsBackToLegacy` |
| REQ-STR-06 | `GatewayRoutingTests.ChangingMode_FromPanel_AppliesToNextRequest`, `MigrationRouteRegistryTests.SetMode_*` |
| REQ-STR-07 | `GatewayRoutingTests.LegacyRoute_IsServedByMonolith_WithDiagnosticHeaders` |
| REQ-SHD-01 | `GatewayRoutingTests.ShadowRoute_ServesLegacy_AndRecordsMatchedAudit`, `ShadowRoute_ReplaysPostBody_ToModern` |
| REQ-SHD-02 | `ShadowParityComparerTests.*`, `ShadowAuditorTests.AuditAsync_WhenStatusCodesDiffer_ReportsStatusDiscrepancy`, `GatewayRoutingTests.ShadowRoute_DetectsRoundingBug_AsDiscrepancy` |
| REQ-SHD-03 | `ShadowAuditorTests.AuditAsync_WhenModernThrows_*`, `AuditAsync_WhenCancelled_*`, `GatewayRoutingTests.ShadowRoute_WhenModernIsDown_*` |
| REQ-SHD-04 | `CutoverPolicyTests.*`, `GatewayRoutingTests.Overview_ReportsRoutesSummariesAndReadiness` |
| REQ-CP-01 | `GatewayRoutingTests.Overview_ReportsRoutesSummariesAndReadiness`, `InMemoryAuditStoreTests.GetSummary_*` |
| REQ-CP-02 | `InMemoryAuditStoreTests.SubscribeAsync_*` (store side; the SSE endpoint is exercised manually in the demo) |
| REQ-CP-03 | `GatewayRoutingTests.AuditsEndpoint_FiltersByVerdict_AndClears` |
| REQ-CP-04 | `GatewayRoutingTests.Panel_IsServedFromEmbeddedResources` |
| REQ-CP-05 | Manual: demo panel "Send 50 requests" (browser-only behaviour) |
| REQ-PAR (REQ-PAR-01 to REQ-PAR-16) | `ParityComparerTests.*`, `ParityRunnerTests.*`, `ParityHtmlRendererTests.*`, `ParityCliOptionsTests.*`, `SampleReportTests.*` (map in `specs/next-parity-report/spec.md`) |
| NFR-02 | `DomainInvariantsTests.Order_CalculatesTotalAmount_WithDeterministicDecimalPrecision` |
| NFR-03 | `GatewayFixture` (all hosts in memory, no sockets) |

Open gaps: REQ-CP-02 end-to-end over HTTP and REQ-CP-05 have no automated test yet (candidate for a Playwright smoke test).

## Static demo (`demo/`, run `npm test` in `demo/`)

| Requirement | Test |
| :--- | :--- |
| REQ-DEMO-05 | `routing.test.ts` |
| REQ-DEMO-06, 08 | `parity.test.ts` |
| REQ-DEMO-09, 15, 20 | `i18n.test.ts` |
| REQ-DEMO-21 | `prefs.test.ts`, `static-rules.test.ts` (both theme blocks equal) |
| REQ-DEMO-22 | `routing.test.ts` (modes), `static-rules.test.ts` (mode controls present) |
| REQ-DEMO-24 | `routes.test.ts` (routes equal `appsettings.json`, readiness wording) |
| REQ-DEMO-25 | `layout.test.ts` (both layouts), browser check at 390 px |
| REQ-DEMO-26 | `config.test.ts` (keys against `ModernizerOptions.cs`, options against the CLI usage, validation), browser check of the form |
| REQ-DEMO-27 | `routes.test.ts` (cut-over rule equals `appsettings.json`) |
| REQ-DEMO-13 (build) | `build.test.ts` (CSS minifier keeps rules and strings) |
| REQ-DEMO-10 to 14, 16, 17, 19, 23 | `static-rules.test.ts` |
| REQ-DEMO-01 to 04, 07 | Browser check (Playwright script, both themes and languages) |
