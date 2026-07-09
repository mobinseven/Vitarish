import { readdirSync, readFileSync, statSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const root = resolve(__dirname, "../apps/private-practice-general/src");
const allowed = new Set(["api/client/apiFetch.ts"]);
const extensions = new Set([".ts", ".tsx"]);
let failed = false;

function walk(dir, prefix = "") {
  for (const entry of readdirSync(dir)) {
    const fullPath = resolve(dir, entry);
    const relativePath = prefix ? `${prefix}/${entry}` : entry;
    if (statSync(fullPath).isDirectory()) {
      walk(fullPath, relativePath);
      continue;
    }

    const extension = entry.endsWith(".tsx") ? ".tsx" : entry.endsWith(".ts") ? ".ts" : "";
    if (!extensions.has(extension) || allowed.has(relativePath) || relativePath.startsWith("api/generated/")) {
      continue;
    }

    const text = readFileSync(fullPath, "utf8");
    if (/\bfetch\s*\([^)]*['"`][^'"`]*\/api\/modules\//s.test(text)) {
      console.error(`Direct /api/modules fetch is forbidden in src/${relativePath}; use src/api/client/apiFetch.ts.`);
      failed = true;
    }
  }
}

walk(root);
if (failed) process.exit(1);
