import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { compare } from "../src/parity.ts";
import { scenarios } from "../src/fixtures.ts";

const auditorTests = readFileSync(
  new URL("../../tests/NainLegacyModernizer.Tests/ShadowAuditorTests.cs", import.meta.url),
  "utf8",
);

function scenario(id: string) {
  const found = scenarios.find((s) => s.id === id);
  assert.ok(found, `scenario ${id}`);
  return found;
}

test("REQ-DEMO-08: identical outputs are Matched with no differences", () => {
  const s = scenario("ORD-001");
  const result = compare(s.legacy, s.modern);
  assert.equal(result.verdict, "Matched");
  assert.equal(result.differences.length, 0);
  assert.equal(result.compared, 3);
});

test("REQ-DEMO-08: a diverging total is a Discrepancy naming the field and both values", () => {
  const s = scenario("ORD-002");
  const result = compare(s.legacy, s.modern);
  assert.equal(result.verdict, "Discrepancy");
  assert.deepEqual(result.differences, [
    { field: "TotalAmount", legacyValue: "99.99", modernValue: "100.00" },
  ]);
});

test("REQ-DEMO-06: fixtures come from ShadowAuditorTests.cs", () => {
  for (const s of scenarios) {
    assert.ok(auditorTests.includes(`"${s.id}"`), `${s.id} id`);
    assert.ok(auditorTests.includes(`/api/orders/${s.id}`), `${s.id} path`);
    assert.ok(auditorTests.includes(`${s.legacy.Total}m`), `${s.id} legacy total`);
    assert.ok(auditorTests.includes(`${s.modern.totalAmount}m`), `${s.id} modern total`);
  }
});

test("REQ-DEMO-06: only the two repository scenarios exist", () => {
  assert.deepEqual(
    scenarios.map((s) => s.id),
    ["ORD-001", "ORD-002"],
  );
});
