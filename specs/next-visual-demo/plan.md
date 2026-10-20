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
    i18n.ts             EN/ES strings
    contrast.ts         WCAG contrast helper (used by tests only)
    main.ts             DOM wiring, SVG animation loop
  scripts/build.ts      copies index.html and styles.css next to the compiled JS
  test/*.test.ts        node:test, run with native type stripping
  dist/                 build output (ignored by git), uploaded to Pages
```

## Decisions

- TypeScript is a build-time dev dependency only (ADR `next-demo-typescript-toolchain`, Proposed).
  Tests use `node:test` and Node's native type stripping, so no test framework is added.
- `erasableSyntaxOnly` keeps sources runnable by Node unchanged; relative imports use `.ts` and are
  rewritten to `.js` by `tsc`.
- Fonts: brandbook font stacks with system fallbacks. Urbanist and IBM Plex Mono are not shipped
  (no external fonts, weight budget).
- The brandbook logo is not used. The page uses a plain text title, so there is no logo to render
  with a gradient.
- The slider is an illustration. Requests are assigned with an integer Bresenham-style rule so the
  animation is reproducible and testable.
- Pages URL: the owner site has a custom domain, so the project page is served under
  `https://www.naindev.com/NainLegacyModernizer/`. All asset paths are relative.

## Risks

- Pages must be enabled with source "GitHub Actions" in repository settings (task `[H]`).
- Brandbook tokens are "All Rights Reserved"; the values are used for ecosystem integration by the
  same owner.
