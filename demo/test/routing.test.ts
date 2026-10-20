import { test } from "node:test";
import assert from "node:assert/strict";
import { clampPercent, routeStatus, targetFor } from "../src/routing.ts";

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
