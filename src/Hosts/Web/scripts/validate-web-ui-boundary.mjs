#!/usr/bin/env node
/**
 * Validates shared Web packages do not import forbidden dependencies.
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
 *
 * From shared/knowledge-web/src, block imports matching:
 *   - ../../apps/
 *   - app aliases
 *   - generated OpenAPI clients/DTOs
 *   - app apiFetch/client code
 *   - backend src/Modules/**, src/Apps/**, src/Identity/**, src/Libraries/**
 *   - app localization catalogs/providers
 */

import { readdirSync, readFileSync, statSync } from "node:fs";
import { resolve, relative, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));

const appAndBackendForbiddenPatterns = [
  { pattern: /\.\.\/\.\.\/apps\//, reason: "must not import from app directories" },
  { pattern: /\.\.\/\.\.\/\.\.\/apps\//, reason: "must not import from app directories" },
  { pattern: /from\s+["']@\//, reason: "must not import through app aliases" },
  { pattern: /from\s+["'].*\/api\/generated\//, reason: "must not import generated API clients" },
  { pattern: /from\s+["'].*\/api\/client\//, reason: "must not import API client code" },
  { pattern: /from\s+["'].*\/src\/Modules\//, reason: "must not import backend modules" },
  { pattern: /from\s+["'].*\/src\/Apps\//, reason: "must not import backend apps" },
  { pattern: /from\s+["'].*\/src\/Identity\//, reason: "must not import backend identity" },
  { pattern: /from\s+["'].*\/src\/Libraries\//, reason: "must not import backend libraries" },
  { pattern: /from\s+["'].*\/localization\/(messages|index)/, reason: "must not import localization catalogs" },
  { pattern: /from\s+["'].*\/LocalizationProvider/, reason: "must not import localization providers" },
];

const packageBoundaries = [
  {
    name: "@vitarish/web-ui",
    src: resolve(__dirname, "../shared/web-ui/src"),
    forbiddenPatterns: [
      ...appAndBackendForbiddenPatterns,
      { pattern: /from\s+["']@tanstack\/react-query/, reason: "must not depend on React Query" },
    ],
  },
  {
    name: "@vitarish/knowledge-web",
    src: resolve(__dirname, "../shared/knowledge-web/src"),
    forbiddenPatterns: appAndBackendForbiddenPatterns,
  },
];

let failed = false;

function walk(boundary, dir, prefix = "") {
  for (const entry of readdirSync(dir)) {
    const fullPath = resolve(dir, entry);
    const relativePath = prefix ? `${prefix}/${entry}` : entry;
    if (statSync(fullPath).isDirectory()) {
      walk(boundary, fullPath, relativePath);
      continue;
    }
    if (!entry.endsWith(".ts") && !entry.endsWith(".tsx")) continue;

    const text = readFileSync(fullPath, "utf8");
    for (const { pattern, reason } of boundary.forbiddenPatterns) {
      if (pattern.test(text)) {
        console.error(`Forbidden import in ${boundary.name}/${relativePath}: ${reason}`);
        failed = true;
      }
    }
  }
}

for (const boundary of packageBoundaries) {
  if (statSync(boundary.src).isDirectory()) {
    walk(boundary, boundary.src);
  }
}

if (failed) {
  console.error("\nShared Web package boundary validation failed.");
  process.exit(1);
} else {
  console.log("Shared Web package boundary validation passed.");
}
