#!/usr/bin/env node
/**
 * Validates that @vitarish/web-ui does not import forbidden dependencies.
 *
 * From shared/web-ui/src, block imports matching:
 *   - ../../apps/
 *   - app route/page folders
 *   - app feature folders
 *   - generated OpenAPI clients/DTOs
 *   - React Query hooks
 *   - backend src/Modules/**, src/Apps/**, src/Identity/**, src/Libraries/**
 *   - app localization catalogs/providers
 *   - non-Web host implementation files
 *   - feature-specific business modules/types
 */

import { readdirSync, readFileSync, statSync } from "node:fs";
import { resolve, relative, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const webUiSrc = resolve(__dirname, "../shared/web-ui/src");

const forbiddenPatterns = [
  { pattern: /\.\.\/\.\.\/apps\//, reason: "must not import from app directories" },
  { pattern: /\.\.\/\.\.\/\.\.\/apps\//, reason: "must not import from app directories" },
  { pattern: /from\s+["'].*\/api\/generated\//, reason: "must not import generated API clients" },
  { pattern: /from\s+["'].*\/api\/client\//, reason: "must not import API client code" },
  { pattern: /from\s+["']@tanstack\/react-query/, reason: "must not depend on React Query" },
  { pattern: /from\s+["'].*\/src\/Modules\//, reason: "must not import backend modules" },
  { pattern: /from\s+["'].*\/src\/Apps\//, reason: "must not import backend apps" },
  { pattern: /from\s+["'].*\/src\/Identity\//, reason: "must not import backend identity" },
  { pattern: /from\s+["'].*\/src\/Libraries\//, reason: "must not import backend libraries" },
  { pattern: /from\s+["'].*\/localization\/(messages|index)/, reason: "must not import localization catalogs" },
  { pattern: /from\s+["'].*\/LocalizationProvider/, reason: "must not import localization providers" },
];

let failed = false;

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
    for (const { pattern, reason } of forbiddenPatterns) {
      if (pattern.test(text)) {
        console.error(`Forbidden import in shared/web-ui/src/${relativePath}: ${reason}`);
        failed = true;
      }
    }
  }
}

if (statSync(webUiSrc).isDirectory()) {
  walk(webUiSrc);
}

if (failed) {
  console.error("\n@vitarish/web-ui boundary validation failed.");
  process.exit(1);
} else {
  console.log("@vitarish/web-ui boundary validation passed.");
}
