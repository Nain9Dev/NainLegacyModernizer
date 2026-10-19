# Operational Runbook: NainLegacyModernizer

## 1. Prerequisites

- .NET 10 SDK (`dotnet --version` >= 10.0.100)

## 2. Standard Commands

```bash
# Restore dependencies
dotnet restore

# Build entire solution with zero warnings
dotnet build --configuration Release

# Execute test suite
dotnet test --configuration Release
```

## 3. Failure Mode Recovery

- **Routing Failure**: If a migrated route returns 404 through YARP, check `appsettings.json` in `NainLegacyModernizer.Gateway` to verify route prefix matching and cluster destinations.
- **Shadow Discrepancy**: If `ShadowAuditor` reports mismatches, inspect the generated discrepancy report payload for precision or casing differences.
