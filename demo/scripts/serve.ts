import { createServer } from "node:http";
import { readFile } from "node:fs/promises";
import { extname } from "node:path";

// Local preview of dist/ on 127.0.0.1 only. Not used in CI.
const types: Record<string, string> = {
  ".html": "text/html; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
};
const dist = new URL("../dist/", import.meta.url);
const port = Number(process.env["PORT"] ?? 4173);

createServer(async (req, res) => {
  const name = (req.url ?? "/").split("?")[0]?.replace(/^\/+/, "") || "index.html";
  if (name.includes("..")) {
    res.writeHead(400).end();
    return;
  }
  try {
    const body = await readFile(new URL(name, dist));
    res.writeHead(200, { "content-type": types[extname(name)] ?? "application/octet-stream" }).end(body);
  } catch {
    res.writeHead(404).end("not found");
  }
}).listen(port, "127.0.0.1", () => console.log(`http://127.0.0.1:${port}/`));
