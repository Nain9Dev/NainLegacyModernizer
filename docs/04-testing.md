# Verification Strategy & Quality Gates: NainLegacyModernizer

## 1. Testing pyramid

1. **Unit tests**
   - Monetary invariants (`DomainInvariantsTests`).
   - Parity comparison rules: casing, aliases, ignored fields, numeric strings, tolerance, arrays, type mismatch,
     non-JSON fallback (`ShadowParityComparerTests`).
   - Parity report and CLI: comparer normalisation, runner, HTML renderer, CLI options, sample reports
     (`ParityComparerTests`, `ParityRunnerTests`, `ParityHtmlRendererTests`, `ParityCliOptionsTests`, `SampleReportTests`).
   - Auditor fault handling: exceptions, cancellation, status mismatch (`ShadowAuditorTests`).
   - Store eviction, summaries, subscriptions (`InMemoryAuditStoreTests`).
   - Cutover policy states (`CutoverPolicyTests`).
   - Route matching and runtime mode changes (`MigrationRouteRegistryTests`).
2. **Integration tests (`WebApplicationFactory`, no sockets)**
   - `GatewayFixture` boots legacy, modern and gateway as test servers and injects the gateway's upstream HTTP pipeline
     so YARP forwards into the other two servers in memory.
   - Every mode, the fallback, shadow replay (GET and POST), the intentional rounding bug, modern-down behaviour,
     mode changes from the API, overview, audits and the embedded panel (`GatewayRoutingTests`).
3. **Static demo (`demo/`, Node 24, `npm test`)**
   - Routing share, parity fixtures, Spanish-first language resolution, theme resolution, modes, diagram layouts,
     migration board against `appsettings.json`, setup generator against `ModernizerOptions.cs`, the CLI usage and
     `compose.yaml`, guided tour, contrast of both themes, static rules (no external asset but the ecosystem bar,
     budget, workflow pinning) and the build-time minifiers.
   - Browser checks with Playwright are run by hand in both themes and languages at desktop and phone widths.
4. **Container (`ci.yml`, job "Container image")**
   - Builds the gateway `Dockerfile` and waits for `GET /_modernizer/health` from the running container.
5. **Manual gate before a client demo**
   - `dotnet run --project src/NainLegacyModernizer.Demo`, open the panel, send traffic, promote a route, open a
     discrepancy, toggle theme and language.

## 2. Gate command

```bash
dotnet test --configuration Release
```

```bash
cd demo && npm ci && npm run typecheck && npm test
```

Warnings are errors (`TreatWarningsAsErrors`), analyzers at `latest-recommended`.
