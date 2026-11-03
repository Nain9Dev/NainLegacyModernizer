import type { Mode } from "./routing.ts";

/**
 * The five routes of the in-repo demo, copied from
 * src/NainLegacyModernizer.Gateway/appsettings.json (id, pattern, mode, canary share).
 * `finding` names what the shadow audit is documented to catch; nothing here is measured.
 */
export type Finding = "none" | "rounding";

export interface DemoRoute {
  id: string;
  pathPattern: string;
  mode: Mode;
  canaryPercent: number | null;
  finding: Finding;
}

export const demoRoutes: readonly DemoRoute[] = [
  { id: "invoices", pathPattern: "/api/invoices/{**rest}", mode: "legacy", canaryPercent: null, finding: "none" },
  { id: "orders", pathPattern: "/api/orders/{**rest}", mode: "shadow", canaryPercent: 20, finding: "none" },
  { id: "customers", pathPattern: "/api/customers/{**rest}", mode: "shadow", canaryPercent: 10, finding: "rounding" },
  { id: "catalog", pathPattern: "/api/catalog/{**rest}", mode: "canary", canaryPercent: 30, finding: "none" },
  { id: "pricing", pathPattern: "/api/pricing/{**rest}", mode: "modern", canaryPercent: null, finding: "none" },
];

export type Readiness = "notStarted" | "verifying" | "blocked" | "live" | "done";

/** Readiness wording derived from the configured mode and the documented finding, never from numbers. */
export function readinessOf(route: DemoRoute): Readiness {
  if (route.mode === "modern") return "done";
  if (route.mode === "canary") return "live";
  if (route.mode === "shadow") return route.finding === "none" ? "verifying" : "blocked";
  return "notStarted";
}

export interface Progress {
  total: number;
  done: number;
  byMode: Record<Mode, number>;
}

export function progressOf(routes: readonly DemoRoute[]): Progress {
  const byMode: Record<Mode, number> = { legacy: 0, shadow: 0, canary: 0, modern: 0 };
  for (const r of routes) byMode[r.mode]++;
  return { total: routes.length, done: byMode.modern, byMode };
}

/** Readiness rule from the same appsettings.json: what "Ready" means on the board. */
export const cutoverPolicy = { minimumSamples: 20, minimumParityRatio: 0.995, maximumErrors: 0 } as const;
