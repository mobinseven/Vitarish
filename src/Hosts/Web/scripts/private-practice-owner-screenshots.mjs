#!/usr/bin/env node

// Screenshots every private-practice-owner page against the mock API, once per mock scenario.
// Usage: node scripts/private-practice-owner-screenshots.mjs [--scenarios ready,empty,loading,error] [-- <playwright test args>]

import { spawn } from "node:child_process";
import { createRequire } from "node:module";
import { rm } from "node:fs/promises";
import path from "node:path";

import { layoutTestFixturesEnvironment } from "./layout-test-fixtures.mjs";

const app = "@vitarish/private-practice-owner";
const appUrl = "http://127.0.0.1:3000";
const allScenarios = ["ready", "empty", "loading", "error"];
const outputRoot = path.resolve("artifacts/private-practice-owner-screenshots");
const playwrightCli = createRequire(import.meta.url).resolve("@playwright/test/cli");
const serverStartTimeoutMilliseconds = 300_000;
const usage = "Usage: node scripts/private-practice-owner-screenshots.mjs [--scenarios ready,empty,loading,error] [-- <playwright test args>]";

const separator = process.argv.indexOf("--");
const ownArguments = separator === -1 ? process.argv.slice(2) : process.argv.slice(2, separator);
const playwrightArguments = separator === -1 ? [] : process.argv.slice(separator + 1);

let scenarios = allScenarios;
if (ownArguments.length > 0) {
  if (ownArguments.length !== 2 || ownArguments[0] !== "--scenarios") {
    console.error(usage);
    process.exit(2);
  }
  scenarios = ownArguments[1].split(",");
  const unknown = scenarios.filter((scenario) => !allScenarios.includes(scenario));
  if (unknown.length > 0) {
    console.error(`Unsupported mock scenario: ${unknown.join(", ")}`);
    process.exit(2);
  }
}

if (await isServing()) {
  console.error(`${appUrl} is already in use. Stop that server first; each scenario needs its own mock server.`);
  process.exit(1);
}

const isWindows = process.platform === "win32";
let failed = false;

for (const scenario of scenarios) {
  await rm(path.join(outputRoot, scenario), { recursive: true, force: true });
  console.log(`\n== ${scenario}: starting mock server`);
  const server = spawn(process.execPath, ["scripts/mock-app-server.mjs", app, "--scenario", scenario], {
    detached: !isWindows,
    env: { ...process.env, NEXT_TELEMETRY_DISABLED: "1", ...layoutTestFixturesEnvironment },
    stdio: "ignore",
  });
  try {
    await waitForServer(server);
    console.log(`== ${scenario}: capturing`);
    const exitCode = await run(
      [playwrightCli, "test", "--config", "playwright.private-practice-owner-screenshots.config.ts", ...playwrightArguments],
      { VITARISH_SCREENSHOT_SCENARIO: scenario, VITARISH_SCREENSHOT_OUTPUT: outputRoot },
    );
    if (exitCode !== 0) failed = true;
  } finally {
    await stopTree(server);
  }
}

console.log(`\nScreenshots: ${outputRoot}`);
process.exit(failed ? 1 : 0);

async function isServing() {
  try {
    await fetch(appUrl, { signal: AbortSignal.timeout(2_000) });
    return true;
  } catch {
    return false;
  }
}

async function waitForServer(server) {
  const deadline = Date.now() + serverStartTimeoutMilliseconds;
  while (Date.now() < deadline) {
    if (server.exitCode !== null) throw new Error(`Mock server exited with code ${server.exitCode}.`);
    if (await isServing()) return;
    await new Promise((resolve) => setTimeout(resolve, 1_000));
  }
  throw new Error(`Mock server did not answer at ${appUrl} within ${serverStartTimeoutMilliseconds / 1000}s.`);
}

function run(args, env) {
  return new Promise((resolve, reject) => {
    const child = spawn(process.execPath, args, { env: { ...process.env, ...env }, stdio: "inherit" });
    child.on("error", reject);
    child.on("exit", (code) => resolve(code ?? 1));
  });
}

async function stopTree(child) {
  if (child.exitCode !== null) return;
  const exited = new Promise((resolve) => child.once("exit", resolve));
  if (isWindows) spawn("taskkill", ["/pid", String(child.pid), "/T", "/F"], { stdio: "ignore" });
  else process.kill(-child.pid, "SIGTERM");
  await exited;
}
