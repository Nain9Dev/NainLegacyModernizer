# ADR (number pending): Ecosystem bar on the static demo

- Status: Proposed
- Date: 2026-11-03

## Context

The other NainDev demos (NainPolicy, NainOrder and the rest of the catalogue) load the shared
`<nain-ecosystem-bar>` web component from `www.naindev.com` together with `tokens.css` and Google
Fonts. The static demo of this repository forbids external scripts, styles and fonts (REQ-DEMO-13) and
ships under 150 KB, so it does not load the bar today. Visitors therefore cannot jump to the other
projects from the demo.

## Decision

Keep the demo self-contained. The top bar and the footer carry the NainDev mark and links to
naindev.com, the repository and the brandbook, which covers the navigation need without a runtime
dependency.

## Alternatives

- Load `ecosystem-bar.js` from `www.naindev.com`: full catalogue navigation and the same chrome as the
  other demos, at the cost of a third-party request on every visit, a dependency on the site's deploy
  cadence and a change to REQ-DEMO-13 and its test.
- Vendor a copy of the bar into `demo/`: no runtime dependency, but the catalogue would drift from the
  canonical one and the published folder would grow by about 30 KB.

## Consequences

- No change to the Pages workflow or to the budget test.
- If the owner prefers the shared bar, REQ-DEMO-13 and `static-rules.test.ts` must allow that one
  script origin, and the bar's catalogue should list this demo (it does since ECO-024 on the site).
