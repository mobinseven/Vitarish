import { readdirSync, readFileSync, statSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const extensions = new Set([".ts", ".tsx"]);
let failed = false;

// Check 1: No direct /api/modules fetch in app source (must use apiFetch from @vitarish/api-client)
const appDirs = [
  resolve(__dirname, "../apps/private-practice-owner/src"),
  resolve(__dirname, "../apps/patients/src"),
  resolve(__dirname, "../apps/knowledge-qa-demo/src"),
];

const allowedAppPaths = new Set(["api/client/apiFetch.ts"]);

function walk(dir, prefix = "", callback) {
  for (const entry of readdirSync(dir)) {
    const fullPath = resolve(dir, entry);
    const relativePath = prefix ? `${prefix}/${entry}` : entry;
    if (statSync(fullPath).isDirectory()) {
      walk(fullPath, relativePath, callback);
      continue;
    }
    if (!entry.endsWith(".ts") && !entry.endsWith(".tsx")) continue;
    callback(fullPath, relativePath);
  }
}

for (const root of appDirs) {
  if (!statSync(root).isDirectory()) continue;
  walk(root, "", (fullPath, relativePath) => {
    if (allowedAppPaths.has(relativePath) || relativePath.startsWith("api/generated/")) return;
    const text = readFileSync(fullPath, "utf8");
    if (/\bfetch\s*\([^)]*['"`][^'"`]*\/api\/modules\//s.test(text)) {
      console.error(`Direct /api/modules fetch is forbidden in ${relativePath}; use @vitarish/api-client.`);
      failed = true;
    }
  });
}

// Check 2: No duplicate apiFetch implementations outside api-client
const sharedDir = resolve(__dirname, "../shared");
if (statSync(sharedDir).isDirectory()) {
  for (const entry of readdirSync(sharedDir)) {
    const pkgDir = resolve(sharedDir, entry);
    if (!statSync(pkgDir).isDirectory() || entry === "api-client") continue;
    const srcDir = resolve(pkgDir, "src");
    if (!statSync(srcDir).isDirectory()) continue;
    walk(srcDir, entry, (fullPath, relativePath) => {
      const text = readFileSync(fullPath, "utf8");
      const isOrvalMutatorAdapter = relativePath.endsWith("/api/client/apiFetch.ts")
        && text.includes('from "@vitarish/api-client"')
        && text.includes("return sharedApiFetch<T>(urlOrConfig, options)");
      if (isOrvalMutatorAdapter) return;
      if (/export\s+(async\s+)?function\s+apiFetch/.test(text)) {
        console.error(`Duplicate apiFetch implementation in shared/${relativePath}; use @vitarish/api-client.`);
        failed = true;
      }
    });
  }
}

if (failed) process.exit(1);
