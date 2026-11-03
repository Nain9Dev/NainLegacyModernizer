export type Target = "modern" | "legacy";
export type RouteStatus = "legacy" | "transition" | "migrated";
/** The four migration modes of a gateway route (MigrationMode in NainLegacyModernizer.Core). */
export type Mode = "legacy" | "shadow" | "canary" | "modern";

export const MODES: readonly Mode[] = ["legacy", "shadow", "canary", "modern"];

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

/** The canary share a mode button selects. Shadow serves everything from legacy, like Legacy. */
export const MODE_PERCENT: Record<Mode, number> = { legacy: 0, shadow: 0, canary: 25, modern: 100 };

/** The mode shown for a slider value: shadow only when the replay is on and nothing is migrated. */
export function modeFor(percent: number, shadow: boolean): Mode {
  const status = routeStatus(percent);
  if (status === "migrated") return "modern";
  if (status === "transition") return "canary";
  return shadow ? "shadow" : "legacy";
}
