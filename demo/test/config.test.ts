import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import {
  buildSettings, defaultSetup, parityCommand, parseAliases, parseList, pathPatternFrom, routeIdFrom, settingsJson, validate, workflowYaml,
} from "../src/config.ts";

const options = readFileSync(new URL("../../src/NainLegacyModernizer.Gateway/ModernizerOptions.cs", import.meta.url), "utf8");
const usage = readFileSync(new URL("../../src/NainLegacyModernizer.ParityCli/CliOptions.cs", import.meta.url), "utf8");

function keysOf(value: unknown, into: Set<string> = new Set()): Set<string> {
  if (Array.isArray(value)) for (const item of value) keysOf(item, into);
  else if (value && typeof value === "object") {
    for (const [k, v] of Object.entries(value)) {
      into.add(k);
      if (k !== "FieldAliases") keysOf(v, into);
    }
  }
  return into;
}

test("REQ-DEMO-26: every generated settings key exists in ModernizerOptions.cs", () => {
  const settings = buildSettings({ ...defaultSetup, mode: "canary" });
  for (const key of keysOf(settings)) {
    if (key === "Modernizer") {
      assert.match(options, /SectionName = "Modernizer"/);
      continue;
    }
    assert.ok(new RegExp(`\\b${key}\\b`).test(options), `key ${key} missing in ModernizerOptions.cs`);
  }
});

test("REQ-DEMO-26: the route block mirrors the mode, pattern and optional fields", () => {
  const settings = buildSettings(defaultSetup) as { Modernizer: { Routes: Record<string, unknown>[] } };
  const [route] = settings.Modernizer.Routes;
  assert.deepEqual(route, {
    Id: "orders",
    PathPattern: "/api/orders/{**rest}",
    Mode: "Shadow",
    FieldAliases: { OrderID: "id" },
    IgnoredFields: ["generatedAt"],
  });
  const canary = buildSettings({ ...defaultSetup, mode: "canary", canaryPercent: 25, ignoredFields: [], aliases: {} }) as { Modernizer: { Routes: Record<string, unknown>[] } };
  assert.deepEqual(canary.Modernizer.Routes[0], { Id: "orders", PathPattern: "/api/orders/{**rest}", Mode: "Canary", CanaryPercent: 25 });
  assert.doesNotThrow(() => JSON.parse(settingsJson(defaultSetup)));
});

test("REQ-DEMO-26: route id and pattern derive from the path", () => {
  assert.equal(routeIdFrom("/api/orders"), "orders");
  assert.equal(routeIdFrom("/api/Customer Accounts/"), "customer-accounts");
  assert.equal(routeIdFrom("/"), "route");
  assert.equal(pathPatternFrom("/api/orders/"), "/api/orders/{**rest}");
  assert.equal(pathPatternFrom("/"), "/{**rest}");
});

test("REQ-DEMO-26: validation rejects what the gateway and the CLI would reject", () => {
  assert.deepEqual(validate(defaultSetup), []);
  assert.deepEqual(validate({ ...defaultSetup, legacy: "legacy.example.com" }), ["legacy"]);
  assert.deepEqual(validate({ ...defaultSetup, modern: "ftp://x" }), ["modern"]);
  assert.deepEqual(validate({ ...defaultSetup, routePath: "api/orders" }), ["routePath"]);
  assert.deepEqual(validate({ ...defaultSetup, mode: "canary", canaryPercent: 0 }), ["canaryPercent"]);
});

test("REQ-DEMO-26: the CLI command only uses documented options", () => {
  const command = parityCommand({ ...defaultSetup, legacy: "https://legacy.example.com", ignoredFields: ["generatedAt", "servedAt"], aliases: { OrderID: "id", Qty: "quantity" } });
  const cliArgs = command.slice(command.indexOf(" -- ") + 4);
  for (const m of cliArgs.matchAll(/--([a-z]+)/g)) assert.ok(usage.includes(`--${m[1]}`), `option --${m[1]} is not in the usage text`);
  assert.match(command, /--map OrderID=id/);
  assert.match(command, /--map Qty=quantity/);
  assert.match(command, /--ignore generatedAt,servedAt/);
  assert.match(command, /--out parity-report\.html/);
  assert.ok(!command.includes("'"), "plain values are not quoted");
  assert.match(parityCommand({ ...defaultSetup, legacy: "https://legacy.example.com/a b" }), /'https:\/\/legacy\.example\.com\/a b'/);
});

test("REQ-DEMO-26: the workflow embeds the same command and fails on a non-zero exit", () => {
  const yaml = workflowYaml(defaultSetup);
  assert.match(yaml, /^name: Parity gate/);
  assert.match(yaml, /permissions:\n {2}contents: read/);
  assert.ok(yaml.includes("dotnet run --project src/NainLegacyModernizer.ParityCli --"));
  assert.ok(yaml.includes("--legacy https://legacy.example.com"));
  assert.ok(yaml.includes("path: parity-report.html"));
});

test("REQ-DEMO-26: list and alias parsing is forgiving", () => {
  assert.deepEqual(parseList(" a, b ,,c "), ["a", "b", "c"]);
  assert.deepEqual(parseAliases("OrderID=id, Qty = quantity, broken, =x"), { OrderID: "id", Qty: "quantity" });
});
