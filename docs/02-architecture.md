# System Architecture: NainLegacyModernizer

## 1. Container view

```mermaid
flowchart TB
    classDef client fill:#0284c7,stroke:#0369a1,color:#fff;
    classDef proxy fill:#0047AB,stroke:#38bdf8,stroke-width:2px,color:#fff;
    classDef modern fill:#0d9488,stroke:#2dd4bf,color:#fff;
    classDef legacy fill:#d97706,stroke:#fbbf24,color:#fff;
    classDef panel fill:#7c3aed,stroke:#a78bfa,color:#fff;

    caller[Client applications]:::client
    operator[Operator browser]:::client

    subgraph gateway [Gateway .NET 10]
        proxy[StranglerProxy<br/>target selection per route mode]:::proxy
        registry[MigrationRouteRegistry<br/>runtime modes]:::proxy
        mirror[Shadow mirror<br/>replay + ShadowParityComparer]:::proxy
        store[(InMemoryAuditStore<br/>traffic, audits, summaries)]:::proxy
        panel[Control panel<br/>embedded HTML/CSS/JS + /_modernizer/api]:::panel
    end

    legacy[Legacy backend<br/>monolith being strangled]:::legacy
    modern[Modern backend<br/>carved-out slices]:::modern

    caller -->|HTTP| proxy
    proxy --> registry
    proxy -->|Legacy / Shadow primary / Canary share| legacy
    proxy -->|Modern / Canary share| modern
    proxy -.->|Shadow replay| mirror --> modern
    mirror --> store
    proxy --> store
    operator --> panel
    panel -->|overview, SSE| store
    panel -->|PUT mode| registry
```

## 2. Request flow per mode

| Mode | Caller is served by | Modern backend | Audit recorded |
| :--- | :--- | :--- | :--- |
| `Legacy` | legacy | not called | traffic sample |
| `Shadow` | legacy | replayed after the response is flushed | traffic sample + parity audit |
| `Canary` | legacy or modern (configured %) | serves its share | traffic sample |
| `Modern` | modern | serves everything | traffic sample |

Unmatched paths behave as `Legacy` under the synthetic route id `legacy-fallback`.

## 3. Component responsibility matrix

| Project | Responsibility | Notes |
| :--- | :--- | :--- |
| `NainLegacyModernizer.Core` | Domain: migration modes and routes, cutover policy, parity model, parity report model, summaries, demo sample data | No references to HTTP or serialization |
| `NainLegacyModernizer.ShadowRunner` | `ShadowParityComparer`, fault-tolerant `ShadowAuditor`, `InMemoryAuditStore` with change notifications | Reusable outside the gateway |
| `NainLegacyModernizer.ShadowRunner` (parity report) | `JsonParityComparer`, `ParityRunner` (HTTP, `GET` only) and `ParityHtmlRenderer` for any pair of APIs | BCL only |
| `NainLegacyModernizer.Gateway` | YARP `IHttpForwarder` proxying, per-request target selection, shadow capture and replay, control panel API and embedded UI | Configured through the `Modernizer` section |
| `NainLegacyModernizer.ParityCli` | `nain-parity` console tool and CI gate (exit codes) | `PackAsTool` |
| `NainLegacyModernizer.LegacyMonolith` | Demo stand-in for the system being strangled (PascalCase, stringified decimals, slow) | Replace with the real system |
| `NainLegacyModernizer.ModernApi` | Demo stand-in for the rewritten slices (camelCase, typed domain, fast) | Replace with the real system |
| `NainLegacyModernizer.Demo` | Runs the three hosts in one process for demos | Not for production |
| `NainLegacyModernizer.Tests` | Unit tests plus in-memory gateway integration tests | No sockets |

Two comparers coexist on purpose: `ShadowParityComparer` serves the gateway's shadow replay (route aliases, ignored
fields, decimal tolerance, non-JSON fallback, typed `DiscrepancyKind`), while `JsonParityComparer` serves the
`nain-parity` CLI (name normalisation across `_`, `-` and case; non-JSON is an execution error). Unifying them is a
pending decision, not an accident of the merge.

## 4. Presentation boundary

The control panel (`Gateway/Panel`) only renders what `/_modernizer/api` returns and sends the operator's intent back
(`PUT /routes/{id}/mode`). Parity ratios, readiness and latency percentiles are computed server side (`Core` and
`ShadowRunner`), never in the browser.

## 5. Parity report flow

```mermaid
flowchart LR
    cli[ParityCli<br/>arguments, files, exit code] --> runner[ShadowRunner<br/>ParityRunner]
    runner --> cmp[JsonParityComparer]
    runner --> http[HttpClient x2<br/>GET only]
    cli --> html[ParityHtmlRenderer]
    runner --> core[Core<br/>ParityReport, RouteParityResult]
    html --> core
    cmp --> core
```

`Core` holds only the report models (no HTTP, no JSON). Comparison, HTTP and rendering live in `ShadowRunner`; the CLI does I/O and exit codes only.
