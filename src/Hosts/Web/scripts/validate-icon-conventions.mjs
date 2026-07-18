#!/usr/bin/env node
/**
 * Enforces the Web host's CSS-class icon convention.
 *
 * Interface icons use Font Awesome CSS classes. Raw SVG remains available only
 * to explicitly reviewed non-icon artwork such as a brand logo or illustration.
 */

import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(__dirname, "..");

function listPackageSourceRoots(groupName) {
  const groupRoot = resolve(webRoot, groupName);
  if (!existsSync(groupRoot)) return [];

  return readdirSync(groupRoot)
    .map((entry) => resolve(groupRoot, entry, "src"))
    .filter((sourceRoot) => existsSync(sourceRoot) && statSync(sourceRoot).isDirectory());
}

const sourceRoots = [
  ...listPackageSourceRoots("apps"),
  ...listPackageSourceRoots("shared"),
];

const svgArtworkAllowlist = new Map([
  [
    "apps/private-practice-general/src/features/landing/brand/index.tsx",
    "renders the product wordmark and brand mark, not interface icons",
  ],
]);

const svgIconImportPatterns = [
  {
    pattern: /from\s+["']lucide-react["']/,
    reason: "Lucide renders SVG icons; use Font Awesome CSS classes",
  },
  {
    pattern: /from\s+["']react-icons(?:\/[^"']+)?["']/,
    reason: "react-icons renders SVG icons; use Font Awesome CSS classes",
  },
  {
    pattern: /from\s+["']@heroicons\/[^"']+["']/,
    reason: "Heroicons renders SVG icons; use Font Awesome CSS classes",
  },
  {
    pattern: /from\s+["']@iconify\/react["']/,
    reason: "Iconify renders SVG icons; use Font Awesome CSS classes",
  },
];

let failed = false;

function validateFile(filePath) {
  if (!filePath.endsWith(".ts") && !filePath.endsWith(".tsx")) return;

  const source = readFileSync(filePath, "utf8");
  const relativePath = relative(webRoot, filePath).replaceAll("\\", "/");

  if (/<(?:svg|path)(?:\s|>)/.test(source) && !svgArtworkAllowlist.has(relativePath)) {
    console.error(
      `Inline SVG markup in ${relativePath}: interface icons must use Font Awesome CSS classes; allowlist only genuine non-icon artwork`,
    );
    failed = true;
  }

  for (const { pattern, reason } of svgIconImportPatterns) {
    if (pattern.test(source)) {
      console.error(`SVG icon import in ${relativePath}: ${reason}`);
      failed = true;
    }
  }
}

function walk(directory) {
  if (!existsSync(directory)) return;

  for (const entry of readdirSync(directory)) {
    const fullPath = resolve(directory, entry);
    if (statSync(fullPath).isDirectory()) {
      walk(fullPath);
    } else {
      validateFile(fullPath);
    }
  }
}

for (const root of sourceRoots) {
  walk(root);
}

for (const [relativePath, reason] of svgArtworkAllowlist) {
  const fullPath = resolve(webRoot, relativePath);
  if (!existsSync(fullPath)) {
    console.error(`Stale SVG artwork exception for ${relativePath}: ${reason}`);
    failed = true;
  }
}

if (failed) {
  console.error("\nWeb icon convention validation failed.");
  process.exit(1);
}

console.log("Web icon convention validation passed.");
