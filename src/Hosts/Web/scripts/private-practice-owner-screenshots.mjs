#!/usr/bin/env node

// Screenshots one app's mock specs (the private-practice-owner app by default) against the mock API, once per mock scenario.
// Usage: node scripts/private-practice-owner-screenshots.mjs [--app <name>] [--scenarios ready,empty,loading,error] [-- <playwright test args>]

import { execFileSync, spawn } from "node:child_process";
import { createRequire } from "node:module";
import { rm } from "node:fs/promises";
import path from "node:path";

import { layoutTestFixturesEnvironment } from "./layout-test-fixtures.mjs";
import {
  defaultMockApp,
  mockAppEnvironmentKey,
  mockApps,
  mockScenarios,
  mockScreenshotOutputEnvironmentKey,
  mockScreenshotRootFor,
  mockScreenshotScenarioEnvironmentKey,
} from "./spec-capture-options.mjs";

const playwrightCli = createRequire(import.meta.url).resolve("@playwright/test/cli");
const serverStartTimeoutMilliseconds = 300_000;
const usage = "Usage: node scripts/private-practice-owner-screenshots.mjs [--app <name>] [--scenarios ready,empty,loading,error] [-- <playwright test args>]";

const separator = process.argv.indexOf("--");
const ownArguments = separator === -1 ? process.argv.slice(2) : process.argv.slice(2, separator);
const playwrightArguments = separator === -1 ? [] : process.argv.slice(separator + 1);

let appName = defaultMockApp;
let scenarios = mockScenarios;
for (let index = 0; index < ownArguments.length; index += 2) {
  const [flag, value] = ownArguments.slice(index, index + 2);
  if (value === undefined) {
    console.error(usage);
    process.exit(2);
  }
  if (flag === "--app") appName = value;
  else if (flag === "--scenarios") scenarios = value.split(",");
  else {
    console.error(usage);
    process.exit(2);
  }
}
if (!Object.hasOwn(mockApps, appName)) {
  console.error(`Unsupported mock app: ${appName}. Use one of ${Object.keys(mockApps).join(", ")}.`);
  process.exit(2);
}
{
  const unknown = scenarios.filter((scenario) => !mockScenarios.includes(scenario));
  if (unknown.length > 0) {
    console.error(`Unsupported mock scenario: ${unknown.join(", ")}`);
    process.exit(2);
  }
}

const { workspace: app, url: appUrl } = mockApps[appName];
const outputRoot = path.resolve(mockScreenshotRootFor(appName));

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
      {
        [mockScreenshotScenarioEnvironmentKey]: scenario,
        [mockScreenshotOutputEnvironmentKey]: outputRoot,
        [mockAppEnvironmentKey]: appName,
      },
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
  if (child.exitCode === null) {
    const exited = new Promise((resolve) => child.once("exit", resolve));
    killTree(child.pid);
    await exited;
  }
  await releaseAppPort();
}

function killTree(pid) {
  if (isWindows) execFileSync("taskkill", ["/pid", String(pid), "/T", "/F"], { stdio: "ignore" });
  else process.kill(-pid, "SIGTERM");
}

// The port was free before this run started its server, so whatever still listens on it once the
// server's process tree is gone was started by this run: a Next worker can outlive the tree kill.
async function releaseAppPort() {
  for (let attempt = 0; attempt < 10 && await isServing(); attempt += 1) {
    if (attempt === 0) {
      for (const pid of listeningProcessIds(new URL(appUrl).port)) {
        try {
          killTree(pid);
        } catch {
          // The process may have exited on its own in the meantime.
        }
      }
    }
    await new Promise((resolve) => setTimeout(resolve, 1_000));
  }
  if (await isServing()) throw new Error(`The mock server is still answering at ${appUrl}; stop it before the next run.`);
}

function listeningProcessIds(port) {
  try {
    if (isWindows) {
      const listening = execFileSync("netstat", ["-ano", "-p", "TCP"], { encoding: "utf8" })
        .split(/\r?\n/)
        .map((line) => line.trim().split(/\s+/))
        .filter((columns) => columns[3] === "LISTENING" && columns[1]?.endsWith(`:${port}`));
      return [...new Set(listening.map((columns) => Number(columns[4])).filter((pid) => pid > 0))];
    }
    return execFileSync("lsof", ["-nP", `-iTCP:${port}`, "-sTCP:LISTEN", "-t"], { encoding: "utf8" })
      .split(/\s+/)
      .map(Number)
      .filter((pid) => pid > 0);
  } catch {
    return [];
  }
}
