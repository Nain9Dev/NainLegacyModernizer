import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { minifyCss } from "../scripts/minify.ts";

test("REQ-DEMO-13: CSS minification keeps every rule, string and custom property", () => {
  const source = readFileSync(new URL("../styles.css", import.meta.url), "utf8");
  const out = minifyCss(source);
  assert.ok(out.length < source.length * 0.9, "minified output is smaller");
  assert.ok(!out.includes("/*"), "no comments");
  for (const m of source.matchAll(/--[a-z0-9-]+:/g)) assert.ok(out.includes(m[0].replace(/\s+$/, "")), m[0]);
  for (const m of source.matchAll(/"[^"\n]*"/g)) assert.ok(out.includes(m[0]), m[0]);
  const rules = (text: string) => (text.match(/\{/g) ?? []).length;
  assert.equal(rules(out), rules(source));
  assert.ok(out.includes('content:"\\2713"'));
  assert.ok(out.includes("--nain-font-sans:\"Urbanist\",system-ui"));
});
