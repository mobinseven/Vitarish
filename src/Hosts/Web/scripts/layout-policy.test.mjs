import { existsSync, readdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";
import assert from "node:assert/strict";

import {
  createProductionLayoutPolicyConfig,
  lintLayoutPolicy,
  validateLayoutExceptions,
} from "./layout-policy.mjs";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const fixturesRoot = resolve(scriptDirectory, "__fixtures__/layout-policy");

test("valid fixture follows shared landmark, viewport, container, and stylesheet ownership", () => {
  const result = lintLayoutPolicy(createFixtureConfig("valid"));

  assert.deepEqual(result.diagnostics, []);
  assert.equal(result.suppressed.length, 0);
});

test("invalid fixture reports every policy rule with actionable source locations", () => {
  const result = lintLayoutPolicy(createFixtureConfig("invalid"));
  const ruleIds = new Set(result.diagnostics.map((diagnostic) => diagnostic.rule));

  assert.deepEqual(
    ruleIds,
    new Set([
      "raw-main",
      "raw-viewport",
      "direct-applayout",
      "legacy-outer-container",
      "layout-css-import",
      "hand-rolled-surface",
    ])
  );
  assert.equal(
    result.diagnostics.filter((diagnostic) => diagnostic.rule === "hand-rolled-surface").length,
    2,
    "surface policy must detect border and background surfaces, including template and variant classes"
  );
  assert.equal(
    result.diagnostics.filter(
      (diagnostic) => diagnostic.rule === "legacy-outer-container"
    ).length,
    2,
    "class policy must inspect both plain strings and template literals with substitutions"
  );

  for (const diagnostic of result.diagnostics) {
    assert.ok(diagnostic.line >= 1);
    assert.ok(diagnostic.column >= 1);
    assert.ok(diagnostic.reason.length > 0);
    assert.ok(diagnostic.preferred.length > 0);
  }
});

test("future exceptions require exact ownership, tracking, and expiry metadata", () => {
  const problems = validateLayoutExceptions(
    [
      {
        rule: "raw-main",
        path: "app/*.tsx",
        owner: "",
        reason: "Temporary migration",
        trackingIssue: "VIT-123",
        expiresOn: "2025-01-01",
      },
    ],
    new Date("2026-07-18T00:00:00Z")
  );

  assert.ok(problems.some((problem) => problem.includes("owner")));
  assert.ok(problems.some((problem) => problem.includes("exact normalized path")));
  assert.ok(problems.some((problem) => problem.includes("expired")));
});

test("production layout policy scans clinic, SEA, and every private-practice source", () => {
  const webRoot = resolve(scriptDirectory, "..");
  const config = createProductionLayoutPolicyConfig(webRoot);

  assert.ok(config.includeRoots.includes("shared/clinic/src"));
  assert.ok(config.includeRoots.includes("shared/sea/src"));
  assert.ok(
    !config.excludePrefixes.some((prefix) =>
      prefix.startsWith("apps/private-practice-owner/src/app/")
    )
  );

  const result = lintLayoutPolicy(config);
  assert.deepEqual(result.diagnostics, []);
});

test("surface policy scans every Web app and shared package source root", () => {
  const webRoot = resolve(scriptDirectory, "..");
  const config = createProductionLayoutPolicyConfig(webRoot);
  const packageRoots = ["apps", "shared"].flatMap((group) =>
    readdirSync(resolve(webRoot, group), { withFileTypes: true })
      .filter((entry) => entry.isDirectory() && existsSync(resolve(webRoot, group, entry.name, "src")))
      .map((entry) => `${group}/${entry.name}/src`)
  );

  assert.deepEqual([...config.surfaceIncludeRoots].sort(), packageRoots.sort());
});

function createFixtureConfig(name) {
  return {
    rootDir: resolve(fixturesRoot, name),
    includeRoots: ["app"],
    surfaceIncludeRoots: ["app"],
    excludePrefixes: [],
    owners: {},
    expectedGlobalStylesheets: ["app/globals.css"],
    now: new Date("2026-07-18T00:00:00Z"),
  };
}
