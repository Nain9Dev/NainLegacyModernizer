# NainLegacyModernizer

> Executable zero-downtime modernization harness for legacy enterprise .NET applications (.NET Framework 4.x, ASP.NET MVC 5, WCF, EF 6) migrating to **.NET 10 Clean Architecture** using the **Strangler Fig Pattern** and **Shadow Running** verification.

[![Architecture](https://img.shields.io/badge/Architecture-Clean_Architecture-0047AB)](docs/02-architecture.md)
[![Target](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Reverse Proxy](https://img.shields.io/badge/YARP-v2.3-blue)](https://microsoft.github.io/reverse-proxy/)
[![Operating Cost](https://img.shields.io/badge/Cost-0.00_EUR-22c55e)](docs/00-charter.md)

---

## 1. Architectural Blueprint

```mermaid
flowchart TB
    classDef client fill:#0284c7,stroke:#0369a1,color:#fff;
    classDef proxy fill:#0047AB,stroke:#38bdf8,stroke-width:2px,color:#fff;
    classDef modern fill:#059669,stroke:#34d399,color:#fff;
    classDef legacy fill:#475569,stroke:#94a3b8,color:#fff;

    clientReq[Client Application / Web Consumer]:::client
    yarpProxy[YARP Gateway<br/>.NET 10 Reverse Proxy]:::proxy
    modernService[ModernApi (.NET 10)<br/>Carved-out /api/orders/*]:::modern
    legacyService[LegacyMonolith (Emulating .NET 4.8 / IIS)<br/>Catch-all unmigrated endpoints]:::legacy

    clientReq -->|Incoming HTTP| yarpProxy
    yarpProxy -->|Path: /api/orders/*| modernService
    yarpProxy -->|Catch-all fallback /*| legacyService
```

## 2. Key Capabilities

- **Strangler Fig Routing**: YARP reverse proxy directs migrated slices to .NET 10 while preserving legacy endpoints without downtime or breaking changes.
- **Shadow Running Auditor**: Asynchronous comparison engine executing dual writes across legacy and modern targets to assert 100.00% numerical and business rule parity before full cutover.
- **Spec-Driven Governance**: Documented under canonical `docs/` lifecycle phases (`00` to `10`).

## 3. Verification

```bash
# Build solution with zero warnings
dotnet build

# Run unit, integration, and routing tests
dotnet test
```

---

**Author**: Aitor Nain Mendoza Vallejo  
**Standards**: [NainDev Engineering Handbook](https://github.com/Nain9Dev/engineering-handbook)
