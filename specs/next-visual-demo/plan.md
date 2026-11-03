# Plan: Static visual demo

## Layout

```
demo/
  index.html            entry point, inline-free (no inline scripts or styles)
  styles.css            brandbook tokens inlined as :root values (no external fetch)
  src/
    routing.ts          deterministic target assignment, route status
    parity.ts           field comparer and ParityVerdict (mirrors ShadowAuditor semantics)
    fixtures.ts         synthetic scenarios copied from ShadowAuditorTests.cs
    i18n.ts             ES/EN strings, Spanish default, language resolution
    prefs.ts            persisted language and theme, theme resolution
    contrast.ts         WCAG contrast helper (used by tests only)
    main.ts             DOM wiring, mode and theme controls, SVG animation loop
  fonts/                Urbanist variable subset (OFL) and its licence
  card.webp             Open Graph image (1280x640)
  scripts/build.ts      copies the entry files, fonts, card and sample reports next to the compiled JS
  test/*.test.ts        node:test, run with native type stripping
  dist/                 build output (ignored by git), uploaded to Pages
```

## Decisions

- TypeScript is a build-time dev dependency only (ADR `next-demo-typescript-toolchain`, Proposed).
  Tests use `node:test` and Node's native type stripping, so no test framework is added.
- `erasableSyntaxOnly` keeps sources runnable by Node unchanged; relative imports use `.ts` and are
  rewritten to `.js` by `tsc`.
- Fonts: Urbanist is self-hosted as the same variable subset the owner site ships (about 25 KB, SIL
  Open Font License, licence file next to it). The monospace stack is system-only.
- The NainDev isotype (three strokes, one colour) is drawn inline; it follows the theme.
- Themes: light (paper) and dark (blueprint) token sets; the deep blue band of the hero and footer is
  the same in both, as on naindev.com.
- Spanish first: `resolveLang` ignores the browser language and returns the stored choice, the
  `lang` query value or `es`.
- The slider is an illustration. Requests are assigned with an integer Bresenham-style rule so the
  animation is reproducible and testable.
- Pages URL: `https://modernizer.naindev.com/` (ADR `next-demo-hosting-subdomain`). All asset paths
  are relative.
- The ecosystem bar used by the other NainDev demos is not loaded: it is an external script and would
  break REQ-DEMO-13 (ADR `next-demo-ecosystem-bar`, Proposed).

## Risks

- Pages must be enabled with source "GitHub Actions" in repository settings (task `[H]`).
- Brandbook tokens are "All Rights Reserved"; the values are used for ecosystem integration by the
  same owner.
