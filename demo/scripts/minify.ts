/** Drops comments and collapses whitespace outside string literals; selectors and values are left as written. */
export function minifyCss(css: string): string {
  const strings: string[] = [];
  const protectedCss = css.replace(/"[^"\n]*"|'[^'\n]*'/g, (literal) => {
    strings.push(literal);
    return `\u0000${strings.length - 1}\u0000`;
  });
  const compact = protectedCss
    .replace(/\/\*[\s\S]*?\*\//g, "")
    .replace(/\s+/g, " ")
    .replace(/\s*([{};:,>])\s*/g, "$1")
    .replace(/;}/g, "}")
    .trim();
  return compact.replace(/\u0000(\d+)\u0000/g, (_, index: string) => strings[Number(index)] ?? "");
}
