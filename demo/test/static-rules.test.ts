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

function tokens(): Map<string, string> {
  const map = new Map<string, string>();
  for (const m of css.matchAll(/--nain-([a-z-]+):\s*(#[0-9A-Fa-f]{6})\s*;/g)) {
    map.set(m[1] as string, m[2] as string);
  }
  return map;
}

test("REQ-DEMO-12: text and component contrast meets WCAG 2.2 AA", () => {
  const t = tokens();
  // [foreground, background, minimum ratio, what it is]
  const pairs: [string, string, number, string][] = [
    ["blue-deep", "paper", 4.5, "body text on paper"],
    ["blue-deep", "surface", 4.5, "text on cards"],
    ["blue-mid", "surface", 4.5, "secondary text on cards"],
    ["blue-mid", "paper", 4.5, "secondary text on paper"],
    ["blue", "surface", 4.5, "links and modern label"],
    ["green-ink", "surface", 4.5, "matched label"],
    ["red-ink", "surface", 4.5, "discrepancy label"],
    ["paper", "blue-deep", 4.5, "text on deep blue"],
    ["green", "blue-deep", 4.5, "accent on deep blue"],
    ["blue-mid", "surface", 3, "control borders"],
    ["blue", "paper", 3, "focus ring on paper"],
    ["green", "blue-deep", 3, "focus ring on deep blue"],
    ["brand-black", "surface", 4.5, "legacy label"],
  ];
  for (const [fg, bg, min, what] of pairs) {
    const a = t.get(fg);
    const b = t.get(bg);
    assert.ok(a && b, `token missing for ${what}: ${fg}/${bg}`);
    const ratio = contrastRatio(a, b);
    assert.ok(ratio >= min, `${what}: ${fg} on ${bg} is ${ratio.toFixed(2)}, needs ${min}`);
  }
});

test("REQ-DEMO-14: brandbook tokens are present, no gradients, no external fetches", () => {
  const t = tokens();
  for (const name of ["blue", "blue-deep", "blue-mid", "paper", "surface", "green", "green-ink", "red-ink", "brand-black"]) {
    assert.ok(t.has(name), `token --nain-${name}`);
  }
  assert.equal(t.get("blue"), "#0047AB");
  assert.equal(t.get("blue-deep"), "#002B66");
  // Plain outbound links (<a href>) are navigation, not fetches; everything else must be local.
  const htmlWithoutLinks = html.replace(/<a\s[^>]*>/g, "<a>");
  for (const file of [{ name: "styles.css", text: css }, { name: "index.html", text: htmlWithoutLinks }, ...sources]) {
    assert.ok(!/gradient/i.test(file.text), `${file.name} must not use gradients`);
    // The SVG namespace identifier is not a network reference.
    const text = file.text.replaceAll("http://www.w3.org/2000/svg", "");
    assert.ok(!/https?:\/\//i.test(text), `${file.name} must not reference external URLs`);
    assert.ok(!/@import/.test(file.text), `${file.name} must not @import`);
  }
});

test("REQ-DEMO-11: reduced motion and focus styles exist", () => {
  assert.match(css, /@media \(prefers-reduced-motion: reduce\)/);
  assert.match(css, /:focus-visible/);
  const main = sources.find((s) => s.name === "main.ts")?.text ?? "";
  assert.match(main, /prefers-reduced-motion: reduce/);
});

test("REQ-DEMO-10/15: document language, viewport and a single module entry", () => {
  assert.match(html, /<html lang="en"/);
  assert.match(html, /<meta name="viewport"/);
  assert.equal((html.match(/<script/g) ?? []).length, 1);
  assert.match(html, /<script type="module" src="main\.js"><\/script>/);
  assert.ok(!/<style/i.test(html), "no inline styles");
  assert.ok(!/\son[a-z]+=/i.test(html), "no inline event handlers");
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
  assert.ok(existsSync(new URL("index.html", dist)));
  assert.ok(existsSync(new URL("main.js", dist)));
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
