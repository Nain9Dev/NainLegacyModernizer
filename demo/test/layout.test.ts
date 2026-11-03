import { test } from "node:test";
import assert from "node:assert/strict";
import { inside, layoutFor, narrow, wide, type Layout } from "../src/layout.ts";

function onEdge(point: { x: number; y: number }, box: { x: number; y: number; width: number; height: number }): boolean {
  const onX = point.x === box.x || point.x === box.x + box.width;
  const onY = point.y === box.y || point.y === box.y + box.height;
  const withinX = point.x >= box.x && point.x <= box.x + box.width;
  const withinY = point.y >= box.y && point.y <= box.y + box.height;
  return (onX && withinY) || (onY && withinX);
}

for (const [name, layout] of [["wide", wide], ["narrow", narrow]] as [string, Layout][]) {
  test(`REQ-DEMO-01/25: ${name} layout keeps every node inside the view and wires on node edges`, () => {
    for (const box of Object.values(layout.nodes)) assert.ok(inside(box, layout), `${name}: node outside the view`);
    assert.ok(onEdge(layout.client, layout.nodes.client), `${name}: client exit`);
    assert.ok(onEdge(layout.dest.modern, layout.nodes.modern), `${name}: modern entry`);
    assert.ok(onEdge(layout.dest.legacy, layout.nodes.legacy), `${name}: legacy entry`);
    const g = layout.nodes.gateway;
    assert.deepEqual(layout.gateway, { x: g.x + g.width / 2, y: g.y + g.height / 2 }, `${name}: gateway centre`);
  });
}

test("REQ-DEMO-25: narrow screens get the top-to-bottom layout", () => {
  assert.equal(layoutFor(true), narrow);
  assert.equal(layoutFor(false), wide);
  assert.ok(narrow.height > narrow.width);
  assert.ok(wide.width > wide.height);
});
