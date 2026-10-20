# ADR (number pending): TypeScript toolchain for the static demo

- Status: Proposed
- Date: 2026-10-20

## Context

The demo (`demo/`) must ship no runtime dependencies and weigh under 150 KB, but the owner wants it
written in TypeScript. Browsers need JavaScript, so something has to compile it.

## Decision

Add two build-time development dependencies, `typescript` and `@types/node` (types for the tests and
build scripts only), pinned in `demo/package-lock.json` and used by `npm run build`, `npm run typecheck`
and the Pages workflow. Nothing from `node_modules` reaches `demo/dist`.

Tests use `node:test` with Node's native type stripping, so no test runner, bundler or browser
driver is added.

## Alternatives

- Hand-written JavaScript with JSDoc types: no dependency, but loses strict type checking the owner asked for.
- Node type stripping at runtime in the browser: not available.
- esbuild/Vite: faster, but more dependencies for a six-module page.

## Consequences

- CI needs Node and `npm ci` for the demo only; the .NET build is untouched.
- Sources must stay erasable (`erasableSyntaxOnly`): no enums or parameter properties.
- Dependabot or equivalent should watch `demo/package.json`.
