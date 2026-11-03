import { test } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { join } from "node:path";
import { contrastRatio } from "../src/contrast.ts";

const demo = new URL("../", import.meta.url);
const root = new URL("../../", import.meta.url);
const read = (base: URL, rel: string) => readFileSync(new URL(rel, base), "utf8");

const html = read(demo, "index.html");
const css = read(demo, "styles.css");
const sources = readdirSync(new URL("src/", demo)).map((f) => ({
  name: f,
  text: read(demo, `src/${f}`),
}));

/** Hex custom properties declared inside the first block whose selector matches. */
function block(selector: RegExp): Map<string, string> {
  const start = css.search(selector);
  assert.ok(start >= 0, `block ${selector}`);
  const body = css.slice(css.indexOf("{", start) + 1, css.indexOf("}", start));
  const map = new Map<string, string>();
  for (const m of body.matchAll(/--([a-z-]+):\s*(#[0-9A-Fa-f]{6})\s*;/g)) {
    map.set(m[1] as string, m[2] as string);
  }
  return map;
}

const light = block(/^:root \{/m);
const dark = block(/^\[data-theme="dark"\] \{/m);
const darkAuto = block(/:root:not\(\[data-theme="light"\]\) \{/);

// [foreground, background, minimum ratio, what it is]
const semanticPairs: [string, string, number, string][] = [
  ["ink", "bg", 4.5, "body text on the canvas"],
  ["ink", "surface", 4.5, "text on panels"],
  ["ink-soft", "surface", 4.5, "secondary text on panels"],
  ["ink-soft", "bg", 4.5, "secondary text on the canvas"],
  ["heading", "surface", 4.5, "headings"],
  ["link", "surface", 4.5, "links"],
  ["legacy", "surface", 4.5, "legacy series label"],
  ["legacy", "bg", 4.5, "legacy series label on the canvas"],
  ["modern", "surface", 4.5, "modern series label"],
  ["modern", "bg", 4.5, "modern series label on the canvas"],
  ["bad", "surface", 4.5, "discrepancy label"],
  ["canary", "surface", 4.5, "canary chip"],
  ["shadow-mode", "surface", 4.5, "shadow replay label"],
  ["cta-ink", "cta", 4.5, "primary button text"],
  ["cta-ink", "cta-hover", 4.5, "primary button text on hover"],
  ["line-strong", "surface", 3, "control borders"],
  ["line-strong", "bg", 3, "control borders on the canvas"],
  ["focus", "bg", 3, "focus ring"],
];

test("REQ-DEMO-12: text and component contrast meets WCAG 2.2 AA in both themes", () => {
  for (const [name, tokens] of [["light", light], ["dark", dark]] as const) {
    for (const [fg, bg, min, what] of semanticPairs) {
      const a = tokens.get(fg);
      const b = tokens.get(bg);
      assert.ok(a && b, `${name}: token missing for ${what}: ${fg}/${bg}`);
      const ratio = contrastRatio(a, b);
      assert.ok(ratio >= min, `${name} ${what}: ${fg} on ${bg} is ${ratio.toFixed(2)}, needs ${min}`);
    }
  }
  // The deep blue band (hero, footer) is the same in both themes.
  for (const [fg, bg, min, what] of [
    ["band-ink", "band", 4.5, "hero text"],
    ["band-accent", "band", 4.5, "hero accent"],
    ["band-ink-soft", "band", 4.5, "hero secondary text"],
    ["band-accent", "band", 3, "focus ring on the band"],
  ] as const) {
    const ratio = contrastRatio(light.get(fg) as string, light.get(bg) as string);
    assert.ok(ratio >= min, `${what}: ${ratio.toFixed(2)}, needs ${min}`);
  }
});

test("REQ-DEMO-21: the dark theme chosen by the visitor equals the one chosen by the system", () => {
  assert.ok(dark.size > 10);
  assert.deepEqual([...darkAuto.entries()], [...dark.entries()]);
});

test("REQ-DEMO-14: brandbook tokens are present, no gradients, no external fetches", () => {
  for (const name of ["blue", "blue-deep", "blue-mid", "paper", "surface", "green", "green-ink", "red-ink", "brand-black", "dark-bg", "dark-elevated", "cyan"]) {
    assert.ok(light.has(`nain-${name}`), `token --nain-${name}`);
  }
  assert.equal(light.get("nain-blue"), "#0047AB");
  assert.equal(light.get("nain-blue-deep"), "#002B66");
  assert.equal(light.get("nain-dark-bg"), "#060D1A");
  // Outbound links (<a href>), canonical and Open Graph metadata are navigation or metadata, not fetches.
  const htmlWithoutLinks = html.replace(/<a\s[^>]*>/g, "<a>").replace(/<link rel="(canonical|alternate)"[^>]*>/g, "").replace(/<meta property="og:[^>]*>/g, "");
  for (const file of [{ name: "styles.css", text: css }, { name: "index.html", text: htmlWithoutLinks }, ...sources]) {
    assert.ok(!/gradient\(/i.test(file.text.replace(/linear-gradient\(to (right|bottom), var\(--[a-z-]+\) 1px, transparent 1px\)/g, "")), `${file.name} must not use gradients beyond the 1px blueprint grid`);
    // The SVG namespace identifier is not a network reference.
    const text = file.text.replaceAll("http://www.w3.org/2000/svg", "");
    assert.ok(!/https?:\/\//i.test(text), `${file.name} must not reference external URLs`);
    assert.ok(!/@import/.test(file.text), `${file.name} must not @import`);
  }
  // The typeface is self-hosted with its licence; nothing is fetched from a font service.
  assert.match(css, /src: url\(fonts\/urbanist-var\.woff2\)/);
  assert.ok(existsSync(new URL("fonts/urbanist-var.woff2", demo)));
  assert.ok(existsSync(new URL("fonts/OFL.txt", demo)));
});

test("REQ-DEMO-11: reduced motion and focus styles exist", () => {
  assert.match(css, /@media \(prefers-reduced-motion: reduce\)/);
  assert.match(css, /:focus-visible/);
  const main = sources.find((s) => s.name === "main.ts")?.text ?? "";
  assert.match(main, /prefers-reduced-motion: reduce/);
});

test("REQ-DEMO-10/15/20: Spanish document, viewport and a single module entry", () => {
  assert.match(html, /<html lang="es"/);
  assert.match(html, /<meta name="viewport"/);
  assert.match(html, /<meta name="color-scheme" content="dark light">/);
  assert.match(html, /<link rel="alternate" hreflang="es" href="https:\/\/modernizer\.naindev\.com\/">/);
  assert.match(html, /<link rel="alternate" hreflang="en" href="https:\/\/modernizer\.naindev\.com\/\?lang=en">/);
  assert.equal((html.match(/<script/g) ?? []).length, 1);
  assert.match(html, /<script type="module" src="main\.js"><\/script>/);
  assert.ok(!/<style/i.test(html), "no inline styles");
  assert.ok(!/\son[a-z]+=/i.test(html), "no inline event handlers");
  // Language and theme controls are real buttons with a pressed state.
  assert.match(html, /<button[^>]*data-lang="es"[^>]*aria-pressed="true"/);
  assert.match(html, /<button[^>]*data-lang="en"[^>]*aria-pressed="false"/);
  assert.match(html, /<button[^>]*id="theme"/);
  for (const mode of ["legacy", "shadow", "canary", "modern"]) {
    assert.ok(html.includes(`data-mode="${mode}"`), `mode control ${mode}`);
  }
});

test("REQ-DEMO-23: the page carries the author, the NainDev links and no generator tag", () => {
  assert.match(html, /<meta name="author" content="Aitor Nain Mendoza Vallejo">/);
  assert.ok(!/<meta name="generator"/i.test(html), "no generator tag");
  for (const href of ["https://www.naindev.com/", "https://github.com/Nain9Dev/NainLegacyModernizer", "https://brand.naindev.com/"]) {
    assert.ok(html.includes(`href="${href}"`), `link to ${href}`);
  }
  assert.ok((html.match(/class="brand-mark"/g) ?? []).length >= 2, "isotype in the top bar and the footer");
});

test("REQ-DEMO-13: the published folder stays under 150 KB", () => {
  const dist = new URL("dist/", demo);
  assert.ok(existsSync(dist), "run `npm run build` first");
  let total = 0;
  const walk = (dir: string) => {
    for (const entry of readdirSync(dir)) {
      const p = join(dir, entry);
      const s = statSync(p);
      if (s.isDirectory()) walk(p);
      else total += s.size;
    }
  };
  walk(dist.pathname.replace(/^\/([A-Za-z]:)/, "$1"));
  assert.ok(total < 150_000, `dist is ${total} bytes`);
  for (const file of ["index.html", "main.js", "styles.css", "fonts/urbanist-var.woff2", "card.webp"]) {
    assert.ok(existsSync(new URL(file, dist)), `dist/${file}`);
  }
});

test("REQ-DEMO-19: the demo links the generated sample reports and ships them", () => {
  for (const name of ["parity-report-not-ready.html", "parity-report-ready.html"]) {
    assert.ok(html.includes(`href="samples/${name}"`), `link to ${name}`);
    assert.ok(existsSync(new URL(`dist/samples/${name}`, demo)), `dist/samples/${name}`);
    const sample = read(demo, `samples/${name}`);
    assert.ok(!/<script/i.test(sample), `${name} has no script`);
    assert.ok(!/https?:\/\//i.test(sample), `${name} has no external reference`);
  }
});

test("REQ-DEMO-16: Pages workflow pins actions, limits permissions, deploys only from main", () => {
  const wf = read(root, ".github/workflows/pages.yml");
  assert.match(wf, /^permissions:\s*\n\s+contents: read/m);
  for (const m of wf.matchAll(/uses:\s*(\S+)/g)) {
    assert.match(m[1] as string, /@[0-9a-f]{40}$/, `unpinned action ${m[1]}`);
  }
  assert.match(wf, /actions\/deploy-pages@/);
  assert.match(wf, /if: github\.ref == 'refs\/heads\/main'/);
  assert.match(wf, /persist-credentials: false/);
});

test("REQ-DEMO-17: README links to the demo", () => {
  const readme = read(root, "README.md");
  assert.match(readme, /\[Live demo\]\(https:\/\/modernizer\.naindev\.com\/\)/);
});
