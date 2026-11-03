# ADR (number pending): Ecosystem bar on the static demo

- Status: Accepted (owner decision, 2026-11-03)
- Date: 2026-11-03

## Context

The other NainDev demos (NainPolicy, NainOrder and the rest of the catalogue) load the shared
`<nain-ecosystem-bar>` web component from `www.naindev.com` together with `tokens.css` and Google
Fonts. The static demo of this repository forbids external scripts, styles and fonts (REQ-DEMO-13) and
ships under 150 KB, so it does not load the bar today. Visitors therefore cannot jump to the other
projects from the demo.

## Decision

Load the shared bar from `www.naindev.com` (`ecosystem-bar.js`, deferred), like the other NainDev
demos, with `current-project="NainLegacyModernizer"`. It is the only external resource of the page;
REQ-DEMO-13 and its test allow that one script origin and nothing else. The page lays out without
it: the custom element takes no room until the script defines it, and the top bar and the footer keep
their own links to naindev.com, the repository and the brandbook.

## Alternatives

- Keep the demo self-contained: no third-party request, but no catalogue navigation and a chrome that
  differs from the rest of the ecosystem.
- Vendor a copy of the bar into `demo/`: no runtime dependency, but the catalogue would drift from the
  canonical one and the published folder would grow by about 30 KB.

## Consequences

- A request to `www.naindev.com` on every visit; if it fails the page is unchanged.
- The bar's catalogue already lists this demo (site spec ECO-024). Its architecture drawer reads a
  profile from the site's `ecosystem.v1.json`; there is none for this project yet, so the drawer falls
  back to the NainOrder profile. Adding a `nainlegacymodernizer` profile is a change on the site
  repository.
