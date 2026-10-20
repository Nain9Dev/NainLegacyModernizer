import type { LegacyOrder, ModernOrder } from "./fixtures.ts";

export type Verdict = "Matched" | "Discrepancy";

export interface Difference {
  field: string;
  legacyValue: string;
  modernValue: string;
}

export interface ParityResult {
  verdict: Verdict;
  compared: number;
  differences: Difference[];
}

/** Field pairs compared by the demo; names follow the shadow auditor's DiscrepancyDetail.Field. */
export const comparedFields = [
  { field: "Id", legacyName: "OrderId", modernName: "id", legacy: (l: LegacyOrder) => l.OrderId, modern: (m: ModernOrder) => m.id },
  { field: "TotalAmount", legacyName: "Total", modernName: "totalAmount", legacy: (l: LegacyOrder) => l.Total, modern: (m: ModernOrder) => m.totalAmount },
  { field: "Currency", legacyName: "Currency", modernName: "currency", legacy: (l: LegacyOrder) => l.Currency, modern: (m: ModernOrder) => m.currency },
] as const;

export function compare(legacy: LegacyOrder, modern: ModernOrder): ParityResult {
  const differences: Difference[] = [];
  for (const f of comparedFields) {
    const legacyValue = f.legacy(legacy);
    const modernValue = f.modern(modern);
    if (legacyValue !== modernValue) differences.push({ field: f.field, legacyValue, modernValue });
  }
  return {
    verdict: differences.length === 0 ? "Matched" : "Discrepancy",
    compared: comparedFields.length,
    differences,
  };
}
