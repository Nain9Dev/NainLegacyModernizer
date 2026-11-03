import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { cutoverPolicy, demoRoutes, progressOf, readinessOf } from "../src/routes.ts";

const settings = JSON.parse(
  readFileSync(new URL("../../src/NainLegacyModernizer.Gateway/appsettings.json", import.meta.url), "utf8"),
) as { Modernizer: { Routes: { Id: string; PathPattern: string; Mode: string; CanaryPercent?: number }[] } };

test("REQ-DEMO-24: the board shows the gateway routes exactly as configured", () => {
  const configured = settings.Modernizer.Routes;
  assert.deepEqual(
    demoRoutes.map((r) => [r.id, r.pathPattern, r.mode, r.canaryPercent]),
    configured.map((r) => [r.Id, r.PathPattern, r.Mode.toLowerCase(), r.CanaryPercent ?? null]),
  );
});

test("REQ-DEMO-24: readiness follows the mode and the documented finding, with no numbers", () => {
  const byId = Object.fromEntries(demoRoutes.map((r) => [r.id, readinessOf(r)]));
  assert.deepEqual(byId, { invoices: "notStarted", orders: "verifying", customers: "blocked", catalog: "live", pricing: "done" });
  const progress = progressOf(demoRoutes);
  assert.equal(progress.total, 5);
  assert.equal(progress.done, 1);
  assert.deepEqual(progress.byMode, { legacy: 1, shadow: 2, canary: 1, modern: 1 });
});

test("REQ-DEMO-27: the cutover rule shown equals the configured policy", () => {
  const configured = (settings as unknown as { Modernizer: { Cutover: { MinimumSamples: number; MinimumParityRatio: number } } }).Modernizer.Cutover;
  assert.equal(cutoverPolicy.minimumSamples, configured.MinimumSamples);
  assert.equal(cutoverPolicy.minimumParityRatio, configured.MinimumParityRatio);
  assert.equal(cutoverPolicy.maximumErrors, 0);
});
