import { test } from "node:test";
import assert from "node:assert/strict";
import { strings } from "../src/i18n.ts";

test("REQ-DEMO-15: every English key has a non-empty Spanish counterpart and vice versa", () => {
  const en = Object.keys(strings.en).sort();
  const es = Object.keys(strings.es).sort();
  assert.deepEqual(es, en);
  for (const lang of ["en", "es"] as const) {
    for (const [key, value] of Object.entries(strings[lang])) {
      assert.ok(value.trim().length > 0, `${lang}.${key} is empty`);
    }
  }
});

test("REQ-DEMO-09: step sentences stay short", () => {
  for (const lang of ["en", "es"] as const) {
    for (const n of [1, 2, 3, 4]) {
      const text = (strings[lang] as Record<string, string>)[`step${n}Text`];
      assert.ok(text, `${lang}.step${n}Text`);
      assert.ok(text.length <= 80, `${lang}.step${n}Text is ${text.length} chars`);
    }
  }
});
