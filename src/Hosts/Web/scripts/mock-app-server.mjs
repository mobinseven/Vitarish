#!/usr/bin/env node

import { spawn } from "node:child_process";

const app = process.argv[2];
const knownApps = new Set([
  "@vitarish/private-practice-owner",
  "@vitarish/patients",
  "@vitarish/platform-admin",
  "@vitarish/knowledge-qa-demo",
  "@vitarish/vitarish-landing",
]);

if (!knownApps.has(app)) {
  console.error("Usage: node scripts/mock-app-server.mjs <workspace> [--scenario ready|empty|loading|error]");
  process.exit(2);
}

const remainingArguments = process.argv.slice(3);
let scenario = process.env.NEXT_PUBLIC_VITARISH_MOCK_SCENARIO ?? "ready";
if (remainingArguments.length > 0) {
  if (remainingArguments.length !== 2 || remainingArguments[0] !== "--scenario") {
    console.error("Use --scenario ready|empty|loading|error.");
    process.exit(2);
  }
  scenario = remainingArguments[1];
}

if (!new Set(["ready", "empty", "loading", "error"]).has(scenario)) {
  console.error(`Unsupported mock scenario: ${scenario}`);
  process.exit(2);
}

const npmCommand = process.platform === "win32" ? "npm.cmd" : "npm";
const child = spawn(npmCommand, ["run", "dev", "--workspace", app], {
  env: {
    ...process.env,
    NEXT_PUBLIC_VITARISH_MOCK_API: "1",
    NEXT_PUBLIC_VITARISH_MOCK_SCENARIO: scenario,
    NEXT_TELEMETRY_DISABLED: "1",
  },
  shell: process.platform === "win32",
  stdio: "inherit",
});

for (const signal of ["SIGINT", "SIGTERM"]) {
  process.on(signal, () => {
    if (!child.killed) child.kill(signal);
  });
}

child.on("error", (error) => {
  console.error(`Unable to start ${app}: ${error.message}`);
  process.exit(1);
});

child.on("exit", (code, signal) => {
  if (signal) process.kill(process.pid, signal);
  else process.exit(code ?? 1);
});
