# Verification Strategy & Quality Gates: NainLegacyModernizer

## 1. Testing pyramid

1. **Unit tests**
   - Monetary invariants (`DomainInvariantsTests`).
   - Parity comparison rules: casing, aliases, ignored fields, numeric strings, tolerance, arrays, type mismatch,
     non-JSON fallback (`JsonParityComparerTests`).
   - Auditor fault handling: exceptions, cancellation, status mismatch (`ShadowAuditorTests`).
   - Store eviction, summaries, subscriptions (`InMemoryAuditStoreTests`).
   - Cutover policy states (`CutoverPolicyTests`).
   - Route matching and runtime mode changes (`MigrationRouteRegistryTests`).
2. **Integration tests (`WebApplicationFactory`, no sockets)**
   - `GatewayFixture` boots legacy, modern and gateway as test servers and injects the gateway's upstream HTTP pipeline
     so YARP forwards into the other two servers in memory.
   - Every mode, the fallback, shadow replay (GET and POST), the intentional rounding bug, modern-down behaviour,
     mode changes from the API, overview, audits and the embedded panel (`GatewayRoutingTests`).
3. **Manual gate before a client demo**
   - `dotnet run --project src/NainLegacyModernizer.Demo`, open the panel, send traffic, promote a route, open a
     discrepancy, toggle theme and language.

## 2. Gate command

```bash
dotnet test --configuration Release
```

Warnings are errors (`TreatWarningsAsErrors`), analyzers at `latest-recommended`.
