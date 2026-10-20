# Spec: Static visual demo

Status: Draft. Number assigned by the merge train (`next-visual-demo`).

## Goal

A static, backend-free page that lets a non-technical visitor understand in about 10 seconds what the
project does: traffic moves from a legacy monolith to a modern API one route at a time, and a shadow
run proves both answer the same before the cut-over.

## Scope

- One entry file (`demo/index.html`), CSS and TypeScript compiled to ES modules. No runtime dependencies.
- Hosted on GitHub Pages from this repository.
- Visible text in English and Spanish. Everything else in the repository is English.

## Non-goals

- No claims about performance, customers, sectors or results. Nothing is measured by the demo.
- No live calls to the .NET services; the page never talks to a backend.
- No weighted traffic splitting claim: the gateway in this repository routes by path
  (`src/NainLegacyModernizer.Gateway/appsettings.json`). The slider is an illustration, and the page says so.

## Requirements (EARS)

### Strangler Fig diagram

- **REQ-DEMO-01** The page shall show four nodes: client, YARP gateway, modern API (.NET 10) and legacy monolith.
- **REQ-DEMO-02** When the visitor moves the "migrated traffic" slider (0 to 100), the page shall update
  the share of `/api/orders/*` requests drawn towards the modern API and the rest towards the legacy monolith.
- **REQ-DEMO-03** The `/api/invoices/*` route shall always be drawn towards the legacy monolith, because
  it is not carved out in the repository.
- **REQ-DEMO-04** While the slider is at 0 the `/api/orders/*` route shall be labelled legacy; while it is
  at 100 it shall be labelled migrated; in between it shall be labelled in transition. State shall never
  rely on colour alone (text label and pattern too).
- **REQ-DEMO-05** The assignment of request number *i* to a target shall be deterministic, and over 100
  consecutive requests exactly *p* shall go to the modern API for a slider value *p*.

### Shadow run panel

- **REQ-DEMO-06** The page shall show the legacy and the modern response side by side for a selected
  scenario, using only the synthetic fixtures from `tests/NainLegacyModernizer.Tests/ShadowAuditorTests.cs`
  (`ORD-001` equal at 100.00 EUR, `ORD-002` legacy 99.99 against modern 100.00).
- **REQ-DEMO-07** When a compared field differs, the page shall highlight it on both sides with a
  non-colour marker and list it with both values.
- **REQ-DEMO-08** The parity indicator shall read `Matched` when no compared field differs and
  `Discrepancy` otherwise (the `ParityVerdict` names in `NainLegacyModernizer.Core`). It shall show the
  count of compared and differing fields, and no percentage.

### Four-step walkthrough

- **REQ-DEMO-09** The page shall present four steps (carve-out, shadow run, compare, cut over), each with
  an SVG icon and at most one short sentence.

### Quality

- **REQ-DEMO-10** The demo shall be fully operable by keyboard with a visible focus indicator.
- **REQ-DEMO-11** While `prefers-reduced-motion: reduce` is set, the page shall not animate; the diagram
  shall update statically. Animation shall have a visible pause control otherwise.
- **REQ-DEMO-12** Text and UI component contrast shall meet WCAG 2.2 AA, verified by a test over the
  tokens in use.
- **REQ-DEMO-13** The published folder shall weigh less than 150 KB in total, with no external fonts,
  scripts, styles or images.
- **REQ-DEMO-14** The CSS shall use the brandbook tokens (`Nain9Dev/brandbook`, `tokens.css` v1.5) and
  no gradients.
- **REQ-DEMO-15** The page shall carry `lang` set to the active language and every English string shall
  have a Spanish counterpart.

### Delivery

- **REQ-DEMO-16** A GitHub Actions workflow shall build, test and publish `demo/dist` to GitHub Pages on
  push to `main`, and shall only build and test on pull requests.
- **REQ-DEMO-17** The README shall link to the demo.
- **REQ-DEMO-18** The demo shall be served at `https://modernizer.naindev.com/`, through GitHub Pages
  behind a Cloudflare-proxied subdomain, at no cost (ADR `next-demo-hosting-subdomain`, Proposed).

## Verification

Node tests in `demo/test/` (`npm test`) cover REQ-DEMO-05, 08, 12, 13, 14, 15 and static checks for 11
and 16. Browser check by hand for 01 to 04, 06, 07, 09 to 11.
