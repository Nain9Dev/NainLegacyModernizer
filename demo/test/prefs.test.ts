import { test } from "node:test";
import assert from "node:assert/strict";
import { resolveTheme } from "../src/prefs.ts";

test("REQ-DEMO-21: a stored theme wins, then the system preference, then dark", () => {
  assert.equal(resolveTheme("light", false), "light");
  assert.equal(resolveTheme("dark", true), "dark");
  assert.equal(resolveTheme(null, true), "light");
  assert.equal(resolveTheme(null, false), "dark");
  assert.equal(resolveTheme("sepia", true), "light");
});
