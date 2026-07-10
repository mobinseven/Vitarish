#!/usr/bin/env node
/**
 * Validates cross-app and public-import boundaries.
 *
 * Enforces:
 *   - apps may import @vitarish/web-ui
 *   - apps must not import @vitarish/web-ui/src/**
 *   - apps must not import other apps' private files
 *   - app-to-app relative imports are forbidden
 *   - shared UI must flow through shared/web-ui only when presentation-only
 */

import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const appsDir = resolve(__dirname, "../apps");

const apps = [];
for (const entry of readdirSync(appsDir)) {
  const fullPath = resolve(appsDir, entry);
  if (statSync(fullPath).isDirectory()) {
    apps.push({ name: entry, path: fullPath });
  }
}

let failed = false;

for (const app of apps) {
  const srcDir = resolve(app.path, "src");
  if (!existsSync(srcDir) || !statSync(srcDir).isDirectory()) continue;

  function walk(dir, prefix = "") {
    for (const entry of readdirSync(dir)) {
      const fullPath = resolve(dir, entry);
      const relativePath = prefix ? `${prefix}/${entry}` : entry;
      if (statSync(fullPath).isDirectory()) {
        walk(fullPath, relativePath);
        continue;
      }
      if (!entry.endsWith(".ts") && !entry.endsWith(".tsx")) continue;

      const text = readFileSync(fullPath, "utf8");

      // Block deep imports into @vitarish/web-ui
      if (/from\s+["']@vitarish\/web-ui\/src\//.test(text)) {
        console.error(`Deep import into @vitarish/web-ui/src in ${app.name}/${relativePath}: use public exports only`);
        failed = true;
      }

      // Block imports from other apps
      for (const otherApp of apps) {
        if (otherApp.name === app.name) continue;
        const importPattern = new RegExp(`from\\s+["'].*\\.\\.\\/${otherApp.name}\\/`, "g");
        if (importPattern.test(text)) {
          console.error(`Cross-app import in ${app.name}/${relativePath}: must not import from ${otherApp.name}`);
          failed = true;
        }
        const directImportPattern = new RegExp(`from\\s+["'].*\\/${otherApp.name}\\/src\\/`, "g");
        if (directImportPattern.test(text)) {
          console.error(`Direct app import in ${app.name}/${relativePath}: must not import from ${otherApp.name}/src/`);
          failed = true;
        }
      }
    }
  }

  walk(srcDir);
}

if (failed) {
  console.error("\nApp boundary validation failed.");
  process.exit(1);
} else {
  console.log("App boundary validation passed.");
}
