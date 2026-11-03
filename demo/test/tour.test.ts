import { test } from "node:test";
import assert from "node:assert/strict";
import { advance, finished, idle, modeAt, start, stop } from "../src/tour.ts";

test("REQ-DEMO-28: the tour walks the four modes in order and then finishes", () => {
  let state = start();
  const seen = [];
  while (state.running) {
    seen.push(modeAt(state));
    state = advance(state);
  }
  assert.deepEqual(seen, ["legacy", "shadow", "canary", "modern"]);
  assert.ok(finished(state));
  assert.equal(modeAt(state), null);
  assert.equal(advance(state), state);
});

test("REQ-DEMO-28: stopping returns to idle, which shows no step", () => {
  assert.deepEqual(stop(), idle);
  assert.equal(modeAt(idle), null);
  assert.ok(!finished(idle));
});
