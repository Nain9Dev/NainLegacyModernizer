# Spec: Installable demo

Status: Draft. Number assigned by the merge train (`next-pwa-install`).

## Goal

A visitor on a phone can install `https://modernizer.naindev.com/` from Android Chrome or iOS Safari and open
it like an app: its own icon and name on the home screen and a window with no browser bar. The shared contract,
the verified browser criteria and the approved names live in the portfolio spec
(`specs/next-pwa-install/spec.md` of `Nain9Dev/nain9dev.github.io`); this spec applies it to the demo.

## Scope

- The published site is the static demo (`demo/index.html`, built into `demo/dist` by `demo/scripts/build.ts`).
- The control panel served by the gateway (`src/NainLegacyModernizer.Gateway/Panel/`) is not the published
  site and is not touched.

## Non-goals

- No service worker, cache, offline page or install button (owner decision of 2026-11-13: installable only).
- No new brand mark: the icons are rendered from the existing product logo.
- No screenshots for the richer Android install dialog, no store package.

## Requirements (EARS)

- **REQ-INS-01** The site shall publish `manifest.webmanifest` at its root with `id`, `start_url` and `scope`
  equal to `./`, `display` `standalone`, `name` `NainLegacyModernizer`, `short_name` `Modernizer`, `lang` `es`,
  `dir` `ltr`, the page meta description as `description`, `theme_color` and `background_color` `#060D1A`
  (the page `theme-color`), and no `prefer_related_applications`.
- **REQ-INS-02** The manifest shall declare three PNG icons under `icons/`, each present at its declared size,
  one purpose per entry: `icon-192.png` and `icon-512.png` (`any`) and `icon-maskable-512.png` (`maskable`,
  opaque edge to edge, mark inside the centred circle of radius 40% of the width). `icons/apple-touch-icon.png`
  shall be 180 px and opaque.
- **REQ-INS-03** The page shall link the manifest and `icons/apple-touch-icon.png` and carry
  `mobile-web-app-capable` and `apple-mobile-web-app-capable` set to `yes`, `apple-mobile-web-app-title`
  equal to the `short_name`, and `apple-mobile-web-app-status-bar-style` set to `black` (the page is dark).
- **REQ-INS-04** When the demo is built, the build shall copy the manifest and the icons unchanged into
  `demo/dist`, which is the folder GitHub Pages publishes.
- **REQ-INS-05** The demo shall not register a service worker.

## Edge cases

- The demo is a single document with relative asset paths, so the manifest uses `./` and relative links.
- GitHub Pages serves no Content-Security-Policy header and the page declares none: a same-origin manifest is
  allowed without a policy change.
- The Pages host has no SPA fallback: an unknown path answers 404, so the manifest cannot be shadowed by
  `index.html`.
- Adding the icons took the published folder over 210 KB; REQ-DEMO-13 now allows 220 KB (approved by the
  owner on 2026-11-14). The icons are palette PNGs (32 colours) to keep the cost near 12.5 KB.

## Acceptance criteria

- `npm test` in `demo/` passes with `test/pwa-install.test.ts`.
- Chrome reports no installability error on `demo/dist` (DevTools protocol `Page.getInstallabilityErrors`).
- After the deploy, `https://modernizer.naindev.com/manifest.webmanifest` answers 200 and the owner installs
  the demo on an Android phone and an iPhone: icon uncropped, label `Modernizer`, no browser bar.

## Session state

- Last updated: 2026-11-14.
- Done: T-001 to T-004. Verified locally: tests, typecheck, icon and manifest checks, Chrome installability.
- Next: T-005 after the merge. Blocked: nothing; T-005 needs the owner's phones.
