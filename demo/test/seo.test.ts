import { test } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";

// REQ-DEMO-31: search and AI-engine discoverability baseline of the public demo.
const demo = new URL("../", import.meta.url);
const dist = new URL("dist/", demo);
const read = (base: URL, rel: string) => readFileSync(new URL(rel, base), "utf8");

const HOST = "https://modernizer.naindev.com/";
const REPO = "https://github.com/Nain9Dev/NainLegacyModernizer";
const html = read(demo, "index.html");

function meta(attr: "name" | "property", key: string): string[] {
  const pattern = new RegExp(`<meta\\s+${attr}="${key}"\\s+content="([^"]*)"\\s*>`, "g");
  return [...html.matchAll(pattern)].map((m) => m[1] as string);
}

test("REQ-DEMO-31: exactly one canonical link equal to the host root", () => {
  const canonical = [...html.matchAll(/<link rel="canonical" href="([^"]*)">/g)];
  assert.equal(canonical.length, 1);
  assert.equal(canonical[0]?.[1], HOST);
});

test("REQ-DEMO-31: one description of 70 to 160 characters", () => {
  const descriptions = meta("name", "description");
  assert.equal(descriptions.length, 1);
  const length = (descriptions[0] as string).length;
  assert.ok(length >= 70 && length <= 160, `description length ${length}`);
});

test("REQ-DEMO-31: Open Graph and Twitter card metadata", () => {
  assert.deepEqual(meta("property", "og:type"), ["website"]);
  assert.deepEqual(meta("property", "og:site_name"), ["NainLegacyModernizer"]);
  assert.equal(meta("property", "og:title").length, 1);
  assert.deepEqual(meta("property", "og:description"), meta("name", "description"));
  assert.deepEqual(meta("property", "og:url"), [HOST]);
  const image = meta("property", "og:image");
  assert.equal(image.length, 1);
  assert.match(image[0] as string, /^https:\/\/modernizer\.naindev\.com\/[^"]+$/);
  assert.ok(existsSync(new URL((image[0] as string).slice(HOST.length), demo)), "og:image file ships with the demo");
  assert.ok(meta("property", "og:image:width").length === 1 && meta("property", "og:image:height").length === 1);
  assert.deepEqual(meta("name", "twitter:card"), ["summary_large_image"]);
  assert.deepEqual(meta("name", "twitter:title"), meta("property", "og:title"));
  assert.deepEqual(meta("name", "twitter:description"), meta("name", "description"));
  assert.deepEqual(meta("name", "twitter:image"), image);
});

test("REQ-DEMO-31: author link", () => {
  assert.match(html, /<link rel="author" href="https:\/\/www\.naindev\.com\/">/);
});

test("REQ-DEMO-31: JSON-LD parses and names the author", () => {
  const blocks = [...html.matchAll(/<script type="application\/ld\+json">([\s\S]*?)<\/script>/g)];
  assert.equal(blocks.length, 1);
  const raw = blocks[0]?.[1] as string;
  assert.ok(!raw.includes("<"), "less-than signs are escaped inside JSON-LD");
  const data = JSON.parse(raw) as Record<string, unknown>;
  assert.equal(data["@context"], "https://schema.org");
  assert.equal(data["@type"], "SoftwareApplication");
  assert.equal(data["url"], HOST);
  assert.equal(data["operatingSystem"], "Web");
  assert.equal(data["isAccessibleForFree"], true);
  assert.deepEqual(data["inLanguage"], ["es", "en"]);
  assert.ok(typeof data["name"] === "string" && typeof data["description"] === "string" && typeof data["applicationCategory"] === "string");
  assert.equal(data["sameAs"] && JSON.stringify(data["sameAs"]).includes(REPO), true);
  const author = data["author"] as Record<string, unknown>;
  assert.equal(author["@type"], "Person");
  assert.equal(author["@id"], "https://www.naindev.com/#person");
  assert.equal(author["url"], "https://www.naindev.com/");
});

for (const [label, base] of [["source", demo], ["dist", dist]] as const) {
  test(`REQ-DEMO-31: robots.txt, sitemap.xml and llms.txt in ${label}`, () => {
    const robots = read(base, "robots.txt");
    assert.match(robots, /^User-agent: \*\r?\nAllow: \/\r?\n/);
    assert.ok(!/Disallow/i.test(robots));
    assert.match(robots, new RegExp(`^Sitemap: ${HOST}sitemap\\.xml\\s*$`, "m"));

    const sitemap = read(base, "sitemap.xml");
    const locs = [...sitemap.matchAll(/<loc>([^<]*)<\/loc>/g)].map((m) => m[1]);
    assert.deepEqual(locs, [HOST]);

    const llms = read(base, "llms.txt");
    assert.match(llms, /^# NainLegacyModernizer\r?\n/);
    assert.match(llms, /^> .+$/m);
    assert.ok(llms.includes(REPO));
    assert.ok(llms.includes("https://www.naindev.com/"));

    // The sample parity reports are noindex and must stay out of every discovery file.
    for (const text of [sitemap, llms, robots]) assert.ok(!text.includes("samples/"), "no sample report URL");
  });
}
