import { test } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";

// REQ-INS-01 to REQ-INS-05: the demo installs on phones (manifest, icons, head tags, published files).
// Opacity and the maskable safe zone are checked by the icon tooling, not here: this test pins the
// manifest members, the icon files with their declared sizes and the head tags.
const demo = new URL("../", import.meta.url);
const dist = new URL("dist/", demo);
const read = (base: URL, rel: string) => readFileSync(new URL(rel, base), "utf8");
const html = read(demo, "index.html");

const manifest = JSON.parse(read(demo, "manifest.webmanifest")) as Record<string, unknown> & {
  icons: { src: string; sizes: string; type: string; purpose: string }[];
};

function pngSize(base: URL, rel: string): string {
  const bytes = readFileSync(new URL(rel, base));
  assert.equal(bytes.subarray(1, 4).toString("latin1"), "PNG", `${rel} is a PNG file`);
  // IHDR starts at byte 16: width then height, big endian.
  return `${bytes.readUInt32BE(16)}x${bytes.readUInt32BE(20)}`;
}

function metaContent(name: string): string[] {
  const pattern = new RegExp(`<meta\\s+name="${name}"\\s+content="([^"]*)"\\s*>`, "g");
  return [...html.matchAll(pattern)].map((m) => m[1] as string);
}

test("REQ-INS-01: the manifest names the app and opens it standalone", () => {
  assert.equal(manifest["id"], "./");
  assert.equal(manifest["start_url"], "./");
  assert.equal(manifest["scope"], "./");
  assert.equal(manifest["display"], "standalone");
  assert.equal(manifest["name"], "NainLegacyModernizer");
  assert.equal(manifest["short_name"], "Modernizer");
  assert.equal(manifest["lang"], "es");
  assert.equal(manifest["dir"], "ltr");
  assert.equal(manifest["theme_color"], "#060D1A");
  assert.equal(manifest["background_color"], "#060D1A");
  assert.ok(!("prefer_related_applications" in manifest), "a related native app would cancel the browser install");
  assert.equal(manifest["description"], metaContent("description")[0], "description reused from the page");
});

test("REQ-INS-02: three icons, one purpose each, present at the declared size", () => {
  assert.deepEqual(
    manifest.icons.map(({ src, sizes, type, purpose }) => ({ src, sizes, type, purpose })),
    [
      { src: "./icons/icon-192.png", sizes: "192x192", type: "image/png", purpose: "any" },
      { src: "./icons/icon-512.png", sizes: "512x512", type: "image/png", purpose: "any" },
      { src: "./icons/icon-maskable-512.png", sizes: "512x512", type: "image/png", purpose: "maskable" },
    ],
  );
  for (const icon of manifest.icons) assert.equal(pngSize(demo, icon.src), icon.sizes, icon.src);
  assert.equal(pngSize(demo, "icons/apple-touch-icon.png"), "180x180");
});

test("REQ-INS-03: the page links the manifest and carries the iOS and Android tags", () => {
  assert.equal([...html.matchAll(/<link rel="manifest" href="manifest\.webmanifest">/g)].length, 1);
  assert.equal([...html.matchAll(/<link rel="apple-touch-icon" href="icons\/apple-touch-icon\.png">/g)].length, 1);
  assert.deepEqual(metaContent("mobile-web-app-capable"), ["yes"]);
  assert.deepEqual(metaContent("apple-mobile-web-app-capable"), ["yes"]);
  assert.deepEqual(metaContent("apple-mobile-web-app-title"), [manifest["short_name"]]);
  assert.deepEqual(metaContent("apple-mobile-web-app-status-bar-style"), ["black"]);
  assert.deepEqual(metaContent("theme-color"), [manifest["theme_color"]]);
});

test("REQ-INS-04: the build publishes the manifest and the icons unchanged", () => {
  assert.ok(existsSync(dist), "run `npm run build` first");
  assert.equal(read(dist, "manifest.webmanifest"), read(demo, "manifest.webmanifest"));
  for (const file of [...manifest.icons.map((i) => i.src), "icons/apple-touch-icon.png"]) {
    assert.ok(readFileSync(new URL(file, dist)).equals(readFileSync(new URL(file, demo))), `dist/${file}`);
  }
});

test("REQ-INS-05: the demo registers no service worker", () => {
  assert.ok(!/serviceWorker/.test(html), "index.html");
  assert.ok(!existsSync(new URL("sw.js", demo)), "no service worker file");
});
