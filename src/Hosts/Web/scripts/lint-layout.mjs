#!/usr/bin/env node

import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import {
  createProductionLayoutPolicyConfig,
  lintLayoutPolicy,
} from "./layout-policy.mjs";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptDirectory, "..");

try {
  const result = lintLayoutPolicy(createProductionLayoutPolicyConfig(webRoot));

  if (result.diagnostics.length > 0) {
    for (const diagnostic of result.diagnostics) {
      console.error(
        `${diagnostic.path}:${diagnostic.line}:${diagnostic.column} ` +
          `[${diagnostic.rule}] ${diagnostic.reason} Preferred: ${diagnostic.preferred}`
      );
    }
    console.error(
      `\nLayout policy validation failed with ${result.diagnostics.length} violation(s).`
    );
    process.exitCode = 1;
  } else {
    console.log(
      `Layout policy validation passed (${result.scannedFiles} files, ` +
        `${result.suppressed.length} exceptions).`
    );
  }
} catch (error) {
  console.error(`Layout policy validation could not run: ${error.message}`);
  process.exitCode = 1;
}
