import { test } from "node:test";
import assert from "node:assert/strict";
import { evaluate, floors } from "../scripts/lighthouse-gate.ts";

test("REQ-DEMO-12/13: the Lighthouse gate fails on any category under its floor", () => {
  const green = { categories: { performance: { score: 0.98 }, accessibility: { score: 1 }, "best-practices": { score: 1 }, seo: { score: 1 } } };
  assert.deepEqual(evaluate(green), []);
  const red = { categories: { performance: { score: 0.89 }, accessibility: { score: 0.99 }, "best-practices": { score: 1 }, seo: { score: null } } };
  assert.deepEqual(evaluate(red), ["performance: 89 is under 90", "accessibility: 99 is under 100", "seo: -1 is under 100"]);
  assert.equal(floors.accessibility, 100);
});
