import { test } from "node:test";
import assert from "node:assert/strict";
import { MODES, MODE_PERCENT, clampPercent, modeFor, routeStatus, targetFor } from "../src/routing.ts";

test("REQ-DEMO-05: exactly p of 100 consecutive requests go to the modern API", () => {
  for (let p = 0; p <= 100; p++) {
    let modern = 0;
    for (let i = 0; i < 100; i++) {
      if (targetFor(i, p) === "modern") modern++;
    }
    assert.equal(modern, p, `slider ${p}`);
  }
});

test("REQ-DEMO-05: assignment is deterministic", () => {
  const a = Array.from({ length: 50 }, (_, i) => targetFor(i, 37));
  const b = Array.from({ length: 50 }, (_, i) => targetFor(i, 37));
  assert.deepEqual(a, b);
});

test("REQ-DEMO-05: the share stays within one request of p in any window of 100", () => {
  for (const p of [10, 33, 50, 80]) {
    for (let start = 0; start < 40; start++) {
      let modern = 0;
      for (let i = start; i < start + 100; i++) {
        if (targetFor(i, p) === "modern") modern++;
      }
      assert.equal(modern, p);
    }
  }
});

test("REQ-DEMO-04: route status at the ends and in between", () => {
  assert.equal(routeStatus(0), "legacy");
  assert.equal(routeStatus(1), "transition");
  assert.equal(routeStatus(99), "transition");
  assert.equal(routeStatus(100), "migrated");
});

test("clampPercent rounds and bounds the slider value", () => {
  assert.equal(clampPercent(-5), 0);
  assert.equal(clampPercent(140), 100);
  assert.equal(clampPercent(42.6), 43);
  assert.equal(clampPercent(Number.NaN), 0);
});

test("REQ-DEMO-22: the four gateway modes map to the slider and back", () => {
  assert.deepEqual(MODES, ["legacy", "shadow", "canary", "modern"]);
  assert.equal(MODE_PERCENT.legacy, 0);
  assert.equal(MODE_PERCENT.shadow, 0);
  assert.equal(MODE_PERCENT.modern, 100);
  assert.ok(MODE_PERCENT.canary > 0 && MODE_PERCENT.canary < 100);
  assert.equal(modeFor(0, false), "legacy");
  assert.equal(modeFor(0, true), "shadow");
  assert.equal(modeFor(25, true), "canary");
  assert.equal(modeFor(100, true), "modern");
  for (const mode of MODES) {
    assert.equal(modeFor(MODE_PERCENT[mode], mode === "shadow"), mode);
  }
});
