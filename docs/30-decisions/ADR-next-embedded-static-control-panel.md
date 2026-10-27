# ADR (number pending): Control panel as embedded static assets without a build step

Status: Proposed
Date: 2026-10-22

## Context

The product needs a visual, client-facing panel. The charter requires zero external dependencies and a demo that
runs with one command. The house default for front-ends is Vue 3 + Vite, which adds Node to the toolchain and CI.

## Decision

The panel is plain HTML, CSS and an ES module (`Gateway/Panel`), embedded in the gateway assembly and served from
`/_modernizer`. Any host that references the gateway gets the panel with no extra files. There is no bundler, no
package manager and no runtime download; fonts are the system stack.

Design constraints baked into the files: validated two-color series palette (legacy amber, modern teal) for light and
dark surfaces, status conveyed by icon + label never by color alone, text in text tokens, reduced-motion support,
English and Spanish strings in one dictionary.

## Alternatives considered

- **Vue 3 + Vite**: better once the panel grows (routing, auth, forms). Rejected for now because it would add a Node
  toolchain to a .NET-only repository and CI for a single page.
- **Blazor**: keeps the stack in .NET but adds a large runtime payload for a dashboard.

## Consequences

- Presentation logic stays small and reviewable; it only formats what the API returns.
- If the panel exceeds roughly one screen of behaviour, migrate it to Vue 3 under a new ADR.
