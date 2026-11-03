import { readFileSync } from "node:fs";

/**
 * Reads a Lighthouse JSON report and fails when a category falls under its floor.
 * Usage: node scripts/lighthouse-gate.ts lighthouse.json
 */
export const floors: Record<string, number> = {
  performance: 90,
  accessibility: 100,
  "best-practices": 95,
  seo: 100,
};

export function evaluate(report: { categories: Record<string, { score: number | null }> }): string[] {
  const failures: string[] = [];
  for (const [category, floor] of Object.entries(floors)) {
    const score = report.categories[category]?.score;
    const value = score === null || score === undefined ? -1 : Math.round(score * 100);
    if (value < floor) failures.push(`${category}: ${value} is under ${floor}`);
  }
  return failures;
}

const file = process.argv[2];
if (file) {
  const report = JSON.parse(readFileSync(file, "utf8")) as { categories: Record<string, { score: number | null }> };
  const scores = Object.entries(report.categories).map(([k, v]) => `${k} ${Math.round((v.score ?? 0) * 100)}`).join(", ");
  const failures = evaluate(report);
  console.log(`Lighthouse: ${scores}`);
  if (failures.length > 0) {
    console.error(failures.join("\n"));
    process.exit(1);
  }
}
