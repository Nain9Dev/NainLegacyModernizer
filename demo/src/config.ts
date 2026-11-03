import type { Mode } from "./routing.ts";

/**
 * Builds what a team pastes to point the harness at its own systems: the `Modernizer` section of
 * the gateway's appsettings.json, the `nain-parity` command and a CI step. Property names follow
 * src/NainLegacyModernizer.Gateway/ModernizerOptions.cs and the CLI usage text.
 */
export interface ClientSetup {
  legacy: string;
  modern: string;
  routePath: string;
  mode: Mode;
  canaryPercent: number;
  ignoredFields: string[];
  aliases: Record<string, string>;
  /** Both systems need an Authorization header; the token comes from the PARITY_TOKEN variable, never from the page. */
  authHeader: boolean;
  /** Wording of the HTML report: the page language, so a Spanish visitor gets a Spanish report. */
  reportLanguage: "en" | "es";
}

export const defaultSetup: ClientSetup = {
  legacy: "https://legacy.example.com",
  modern: "https://new.example.com",
  routePath: "/api/orders",
  mode: "shadow",
  canaryPercent: 10,
  ignoredFields: ["generatedAt"],
  aliases: { OrderID: "id" },
  authHeader: false,
  reportLanguage: "es",
};

export const TOKEN_VARIABLE = "PARITY_TOKEN";

export type SetupError = "legacy" | "modern" | "routePath" | "canaryPercent";

export function isBaseUrl(value: string): boolean {
  try {
    const url = new URL(value);
    return (url.protocol === "http:" || url.protocol === "https:") && url.host.length > 0;
  } catch {
    return false;
  }
}

export function validate(setup: ClientSetup): SetupError[] {
  const errors: SetupError[] = [];
  if (!isBaseUrl(setup.legacy)) errors.push("legacy");
  if (!isBaseUrl(setup.modern)) errors.push("modern");
  if (!/^\/[A-Za-z0-9_\-./]*$/.test(setup.routePath) || setup.routePath.includes("//")) errors.push("routePath");
  if (!Number.isInteger(setup.canaryPercent) || setup.canaryPercent < 1 || setup.canaryPercent > 99) errors.push("canaryPercent");
  return errors;
}

/** "/api/orders/" becomes "orders"; the last non-empty segment, lower case, safe characters only. */
export function routeIdFrom(routePath: string): string {
  const segments = routePath.split("/").filter((s) => s.length > 0);
  const last = segments[segments.length - 1] ?? "route";
  const id = last.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "");
  return id.length > 0 ? id : "route";
}

/** "/api/orders" becomes "/api/orders/{**rest}", the catch-all form the gateway matches. */
export function pathPatternFrom(routePath: string): string {
  const trimmed = routePath.replace(/\/+$/, "");
  return `${trimmed.length > 0 ? trimmed : ""}/{**rest}`;
}

const modeName: Record<Mode, string> = { legacy: "Legacy", shadow: "Shadow", canary: "Canary", modern: "Modern" };

export function buildSettings(setup: ClientSetup): Record<string, unknown> {
  const route: Record<string, unknown> = {
    Id: routeIdFrom(setup.routePath),
    PathPattern: pathPatternFrom(setup.routePath),
    Mode: modeName[setup.mode],
  };
  if (setup.mode === "canary") route["CanaryPercent"] = setup.canaryPercent;
  if (Object.keys(setup.aliases).length > 0) route["FieldAliases"] = { ...setup.aliases };
  if (setup.ignoredFields.length > 0) route["IgnoredFields"] = [...setup.ignoredFields];
  return {
    Modernizer: {
      LegacyBaseAddress: setup.legacy,
      ModernBaseAddress: setup.modern,
      Cutover: { MinimumSamples: 20, MinimumParityRatio: 0.995 },
      Routes: [route],
    },
  };
}

export function settingsJson(setup: ClientSetup): string {
  return JSON.stringify(buildSettings(setup), null, 2);
}

function shellQuote(value: string): string {
  return /^[A-Za-z0-9_\-./:=,@%+]+$/.test(value) ? value : `'${value.replace(/'/g, "'\\''")}'`;
}

export function parityCommand(setup: ClientSetup): string {
  const parts = [
    "dotnet run --project src/NainLegacyModernizer.ParityCli --",
    `--legacy ${shellQuote(setup.legacy)}`,
    `--modern ${shellQuote(setup.modern)}`,
    "--paths paths.txt",
  ];
  for (const [legacyName, modernName] of Object.entries(setup.aliases)) parts.push(`--map ${shellQuote(`${legacyName}=${modernName}`)}`);
  if (setup.ignoredFields.length > 0) parts.push(`--ignore ${shellQuote(setup.ignoredFields.join(","))}`);
  if (setup.authHeader) parts.push(`--header "Authorization: Bearer $${TOKEN_VARIABLE}"`);
  if (setup.reportLanguage === "es") parts.push("--lang es");
  parts.push("--out parity-report.html");
  return parts.join(" \\\n  ");
}

/** A GitHub Actions job that fails when the route is not ready (exit code 1). */
export function workflowYaml(setup: ClientSetup): string {
  return [
    "name: Parity gate",
    "on: [pull_request, workflow_dispatch]",
    "permissions:",
    "  contents: read",
    "jobs:",
    "  parity:",
    "    runs-on: ubuntu-latest",
    "    steps:",
    "      - uses: actions/checkout@v4",
    "      - uses: actions/setup-dotnet@v4",
    "        with:",
    '          dotnet-version: "10.0.x"',
    "      - name: Compare legacy and modern",
    ...(setup.authHeader ? ["        env:", `          ${TOKEN_VARIABLE}: \${{ secrets.${TOKEN_VARIABLE} }}`] : []),
    "        run: |",
    ...parityCommand(setup)
      .split("\n")
      .map((line, i) => (i === 0 ? `          ${line}` : `          ${line.trim()}`)),
    "      - uses: actions/upload-artifact@v4",
    "        if: always()",
    "        with:",
    "          name: parity-report",
    "          path: parity-report.html",
  ].join("\n");
}

/** Comma separated "a, b" to a clean list. */
export function parseList(value: string): string[] {
  return value.split(",").map((s) => s.trim()).filter((s) => s.length > 0);
}

/** "OrderID=id, Qty=quantity" to a map; entries without "=" are dropped. */
export function parseAliases(value: string): Record<string, string> {
  const map: Record<string, string> = {};
  for (const pair of parseList(value)) {
    const [legacyName, modernName] = pair.split("=").map((s) => s.trim());
    if (legacyName && modernName) map[legacyName] = modernName;
  }
  return map;
}

/** The same settings as ASP.NET Core environment variables: nested keys joined with "__", lists by index. */
export function flattenSettings(value: unknown, prefix = ""): [string, string][] {
  if (Array.isArray(value)) return value.flatMap((item, i) => flattenSettings(item, `${prefix}__${i}`));
  if (value && typeof value === "object") {
    return Object.entries(value).flatMap(([k, v]) => flattenSettings(v, prefix ? `${prefix}__${k}` : k));
  }
  return [[prefix, String(value)]];
}

function yamlValue(value: string): string {
  return /^[0-9.]+$/.test(value) || /[:#{}[\],&*?|<>=!%@`'"]/.test(value) ? `"${value.replace(/"/g, '\\"')}"` : value;
}

/** A Compose file that builds the repository's Dockerfile and configures the gateway by environment. */
export function composeYaml(setup: ClientSetup): string {
  const lines = [
    "services:",
    "  gateway:",
    "    build: .",
    "    ports:",
    '      - "8080:8080"',
    "    environment:",
  ];
  for (const [key, value] of flattenSettings(buildSettings(setup))) lines.push(`      ${key}: ${yamlValue(value)}`);
  return lines.join("\n");
}

/** The paths file the command reads: one request path per line, comments allowed. */
export function pathsFile(setup: ClientSetup): string {
  const base = setup.routePath.replace(/\/+$/, "");
  return [
    "# One GET path per line. Lines starting with # are ignored.",
    "# Use real identifiers from your data: the same request is sent to both systems.",
    `${base}/1001`,
    `${base}/1002`,
    `${base}?page=1`,
  ].join("\n");
}
