import { copyFileSync, mkdirSync } from "node:fs";

// tsc already wrote the JavaScript into dist/; add the static entry files next to it.
const demo = new URL("../", import.meta.url);
const dist = new URL("dist/", demo);
mkdirSync(dist, { recursive: true });
for (const file of ["index.html", "styles.css"]) {
  copyFileSync(new URL(file, demo), new URL(file, dist));
}
