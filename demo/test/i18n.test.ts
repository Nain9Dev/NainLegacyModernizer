import { test } from "node:test";
import assert from "node:assert/strict";
import { DEFAULT_LANG, resolveLang, strings } from "../src/i18n.ts";

test("REQ-DEMO-15: every Spanish key has a non-empty English counterpart and vice versa", () => {
  const en = Object.keys(strings.en).sort();
  const es = Object.keys(strings.es).sort();
  assert.deepEqual(en, es);
  for (const lang of ["es", "en"] as const) {
    for (const [key, value] of Object.entries(strings[lang])) {
      assert.ok(value.trim().length > 0, `${lang}.${key} is empty`);
    }
  }
});

test("REQ-DEMO-09: step sentences stay short", () => {
  for (const lang of ["es", "en"] as const) {
    for (const n of [1, 2, 3, 4]) {
      const text = (strings[lang] as Record<string, string>)[`step${n}Text`];
      assert.ok(text, `${lang}.step${n}Text`);
      assert.ok(text.length <= 80, `${lang}.step${n}Text is ${text.length} chars`);
    }
  }
});

test("REQ-DEMO-20: Spanish is the default; a stored choice or a lang query wins", () => {
  assert.equal(DEFAULT_LANG, "es");
  assert.equal(resolveLang(null, null), "es");
  assert.equal(resolveLang("en", null), "en");
  assert.equal(resolveLang("es", "en"), "en");
  assert.equal(resolveLang("fr", "de"), "es");
  assert.equal(resolveLang("", ""), "es");
});

test("REQ-DEMO-20: visible copy stays short (no paragraph over 140 characters)", () => {
  for (const lang of ["es", "en"] as const) {
    for (const [key, value] of Object.entries(strings[lang])) {
      if (key === "pageTitle" || key === "pageDescription" || key === "diagramLabel") continue;
      assert.ok(value.length <= 140, `${lang}.${key} is ${value.length} chars`);
    }
  }
});
