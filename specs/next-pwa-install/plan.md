# Plan: Installable demo

## Layout

```
demo/
  manifest.webmanifest   static manifest, copied to the root of dist/ by the build
  icons/                 icon-192.png, icon-512.png, icon-maskable-512.png, apple-touch-icon.png
  index.html             manifest link, apple-touch-icon and the iOS and Android tags in the head
  scripts/build.ts       copies the manifest and the icons/ folder into dist/
  scripts/serve.ts       local preview now answers .webmanifest and .png with their media types
  test/pwa-install.test.ts
```

## Icons

Rendered from `src/NainLegacyModernizer.Gateway/Panel/logo.svg`, the product logo (the inline favicon of the
page draws the same mark on the same `#002B66` tile). The mark brings its own rounded tile, so `--bg` is the
tile colour and the maskable and Apple icons blend into it.

```
node make-install-icons.mjs --source src/NainLegacyModernizer.Gateway/Panel/logo.svg --out demo/icons --bg "#002B66" --maskable-scale 0.846 --apple-scale 1
```

The maskable mark is drawn at 84.6% of the width, the scale the other NainDev-marked sites use, so the family
looks the same on a home screen. The raw output is RGBA (about 30 KB for the four files); each file was then
re-encoded as a 32-colour palette PNG with `sharp`
(`png({ palette: true, colours: 32, compressionLevel: 9, effort: 10 })`), 12.5 KB in total.
The mark is flat colour, so the palette keeps the edges clean.

## Decisions

- **Static manifest at the demo root**, not generated: the build already copies a list of root files.
- **No service worker**: owner decision; Chrome and iOS install without one.
- **Status bar style `black`**: the page paints `#060D1A` first.
- **Size budget raised from 210 KB to 220 KB** (REQ-DEMO-13), approved by the owner on 2026-11-14: the
  published folder was 200.6 KB and is 214.1 KB with the icons, the manifest and the head tags. Dropping a
  size or the maskable icon would break the contract.

## Test strategy

`demo/test/pwa-install.test.ts` (run by `npm test`, which pages.yml runs after a build): manifest members and
names, icon files with their declared size read from the PNG header, head tags in `index.html`, files copied
unchanged into `dist/`, no service worker. Opacity and the safe zone are checked with `verify-install.mjs`
and Chrome with `installability.mjs` (manual, not part of CI).
