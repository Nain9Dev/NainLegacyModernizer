export type Target = "modern" | "legacy";
export type RouteStatus = "legacy" | "transition" | "migrated";

export function clampPercent(value: number): number {
  if (!Number.isFinite(value)) return 0;
  return Math.min(100, Math.max(0, Math.round(value)));
}

/**
 * Deterministic share: request `index` goes to the modern API when the running
 * total floor(i * p / 100) increases at that request, so any 100 consecutive
 * requests contain exactly `percent` modern ones.
 */
export function targetFor(index: number, percent: number): Target {
  const p = clampPercent(percent);
  const before = Math.floor((index * p) / 100);
  const after = Math.floor(((index + 1) * p) / 100);
  return after > before ? "modern" : "legacy";
}

export function routeStatus(percent: number): RouteStatus {
  const p = clampPercent(percent);
  if (p === 0) return "legacy";
  if (p === 100) return "migrated";
  return "transition";
}
