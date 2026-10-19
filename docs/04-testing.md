# Verification Strategy & Quality Gates: NainLegacyModernizer

## 1. Testing Pyramid

1. **Unit Tests**:
   - Monetary value calculations and tax rounding invariants.
   - Shadow comparison engine assertion logic (detecting missing fields, rounding diffs, schema changes).
2. **Integration Tests (`WebApplicationFactory`)**:
   - YARP gateway path-based forwarding: verifies `/api/orders` routes to `ModernApi`.
   - YARP fallback catch-all: verifies unmigrated routes (e.g., `/api/invoices`) hit `LegacyMonolith`.
3. **Architectural Fitness Functions**:
   - Domain layer purity: `NainLegacyModernizer.Core` has 0 references to YARP, HTTP, or serialization libraries.

## 2. Gate Verification Command

```bash
dotnet test --logger "console;verbosity=normal"
```
