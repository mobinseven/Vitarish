#!/usr/bin/env node
/**
 * Validates shared Web packages do not import forbidden dependencies.
 *
 * Enforced boundaries:
 *   - @vitarish/ui: no React Query, no apps, no generated clients, no backend, no feature packages
 *   - @vitarish/api-client: no React/UI, no localization, no generated clients, no apps, no backend
 *   - @vitarish/localization: no apps, no generated clients, no feature packages, no backend
 *   - @vitarish/knowledge: no apps, no generated clients, no backend, no app localization
 *   - Feature packages: no apps, no backend, no deep-importing other feature packages
 */

import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));

const appAndBackendForbiddenPatterns = [
  { pattern: /\.\.\/\.\.\/apps\//, reason: "must not import from app directories" },
  { pattern: /\.\.\/\.\.\/\.\.\/apps\//, reason: "must not import from app directories" },
  { pattern: /from\s+["']@\//, reason: "must not import through app aliases" },
  { pattern: /from\s+["'].*\/src\/Modules\//, reason: "must not import backend modules" },
  { pattern: /from\s+["'].*\/src\/Apps\//, reason: "must not import backend apps" },
  { pattern: /from\s+["'].*\/src\/Identity\//, reason: "must not import backend identity" },
  { pattern: /from\s+["'].*\/src\/Libraries\//, reason: "must not import backend libraries" },
];

// Packages that do NOT own generated clients must not import them
const noGeneratedClientPatterns = [
  { pattern: /from\s+["'].*\/api\/generated\//, reason: "must not import generated API clients" },
  { pattern: /from\s+["'].*\/api\/client\//, reason: "must not import API client code" },
];

const crossPackageForbiddenPatterns = [
  ...appAndBackendForbiddenPatterns,
  { pattern: /from\s+["'].*\/localization\/(messages|index)/, reason: "must not import localization catalogs from other packages" },
  { pattern: /from\s+["'].*\/LocalizationProvider/, reason: "must not import localization providers from other packages" },
];

// Packages that own generated clients can import their own, but not from other packages
const featurePackageForbiddenPatterns = [
  ...appAndBackendForbiddenPatterns,
];

// Shared packages must not import next/navigation (use onNavigate callbacks instead)
const noNextNavigationPatterns = [
  { pattern: /from\s+["']next\/navigation["']/, reason: "must not import next/navigation directly (use onNavigate callbacks)" },
  { pattern: /from\s+["']next\/router["']/, reason: "must not import next/router directly" },
];

// Sea must not depend on clinic. Clinic may consume SEA's public package API,
// but neither package may deep-import the other's source tree.
const clinicSeaBoundaryPatterns = [
  { pattern: /from\s+["']@vitarish\/clinic(?!\/api\/generated)/, reason: "sea must not import from clinic" },
  { pattern: /from\s+["'].*clinic\/src\/(?!api\/generated)/, reason: "must not deep-import from clinic (except generated API)" },
];

const seaClinicBoundaryPatterns = [
  { pattern: /from\s+["'].*sea\/src\//, reason: "must not deep-import from sea" },
];

const forbiddenPathBoundaries = [
  {
    name: "@vitarish/sea",
    path: resolve(__dirname, "../shared/sea/src/features/sea"),
    reason: "must not reintroduce the removed src/features/sea ownership wrapper",
  },
  {
    name: "@vitarish/clinic",
    path: resolve(__dirname, "../shared/clinic/src/features/clinic"),
    reason: "@vitarish/clinic must not reintroduce the removed src/features/clinic wrapper",
  },
];

const packageBoundaries = [
  {
    name: "@vitarish/ui",
    src: resolve(__dirname, "../shared/ui/src"),
    forbiddenPatterns: [
      ...crossPackageForbiddenPatterns,
      ...noGeneratedClientPatterns,
      ...noNextNavigationPatterns,
      { pattern: /from\s+["']@tanstack\/react-query/, reason: "must not depend on React Query" },
    ],
  },
  {
    name: "@vitarish/api-client",
    src: resolve(__dirname, "../shared/api-client/src"),
    forbiddenPatterns: [
      ...appAndBackendForbiddenPatterns,
      ...noNextNavigationPatterns,
      { pattern: /from\s+["']react/, reason: "must not import React" },
      { pattern: /from\s+["']@tanstack\/react-query/, reason: "must not depend on React Query" },
    ],
  },
  {
    name: "@vitarish/localization",
    src: resolve(__dirname, "../shared/localization/src"),
    forbiddenPatterns: [
      ...appAndBackendForbiddenPatterns,
    ],
  },
  {
    name: "@vitarish/knowledge",
    src: resolve(__dirname, "../shared/knowledge/src"),
    forbiddenPatterns: [
      ...crossPackageForbiddenPatterns,
      ...noGeneratedClientPatterns,
      ...noNextNavigationPatterns,
    ],
  },
  {
    name: "@vitarish/auth",
    src: resolve(__dirname, "../shared/auth/src"),
    forbiddenPatterns: [
      ...featurePackageForbiddenPatterns,
      ...noNextNavigationPatterns,
    ],
  },
  {
    name: "@vitarish/account",
    src: resolve(__dirname, "../shared/account/src"),
    forbiddenPatterns: [
      ...featurePackageForbiddenPatterns,
      ...noNextNavigationPatterns,
    ],
  },
  {
    name: "@vitarish/journey",
    src: resolve(__dirname, "../shared/journey/src"),
    forbiddenPatterns: [
      ...featurePackageForbiddenPatterns,
      ...noNextNavigationPatterns,
    ],
  },
  {
    name: "@vitarish/clinic",
    src: resolve(__dirname, "../shared/clinic/src"),
    forbiddenPatterns: [
      ...featurePackageForbiddenPatterns,
      ...noNextNavigationPatterns,
      ...seaClinicBoundaryPatterns,
    ],
  },
  {
    name: "@vitarish/intelligence",
    src: resolve(__dirname, "../shared/intelligence/src"),
    forbiddenPatterns: [
      ...featurePackageForbiddenPatterns,
      ...noNextNavigationPatterns,
    ],
  },
  {
    name: "@vitarish/communications",
    src: resolve(__dirname, "../shared/communications/src"),
    forbiddenPatterns: [
      ...featurePackageForbiddenPatterns,
      ...noNextNavigationPatterns,
    ],
  },
  {
    name: "@vitarish/sea",
    src: resolve(__dirname, "../shared/sea/src"),
    forbiddenPatterns: [
      ...featurePackageForbiddenPatterns,
      ...noNextNavigationPatterns,
      ...clinicSeaBoundaryPatterns,
    ],
  },
];

let failed = false;

for (const boundary of forbiddenPathBoundaries) {
  if (existsSync(boundary.path)) {
    console.error(`Forbidden path exists: ${boundary.path}: ${boundary.reason}`);
    failed = true;
  }
}


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

for (const boundary of forbiddenPathBoundaries) {
  try {
    if (statSync(boundary.path)) {
      console.error(`Forbidden path in ${boundary.name}: ${boundary.path} ${boundary.reason}`);
      failed = true;
    }
  } catch (error) {
    if (error?.code !== "ENOENT") {
      throw error;
    }
  }
}

if (failed) {
  console.error("\nShared Web package boundary validation failed.");
  process.exit(1);
} else {
  console.log("Shared Web package boundary validation passed.");
}
