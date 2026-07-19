#!/usr/bin/env node

import { spawn } from "node:child_process";
import { cpSync, existsSync } from "node:fs";
import { join, resolve } from "node:path";

const workspace = process.argv[2];
const appConfigurations = new Map([
  ["@vitarish/private-practice-owner", { directory: "private-practice-owner", port: "3000" }],
  ["@vitarish/patients", { directory: "patients", port: "3001" }],
  ["@vitarish/knowledge-qa-demo", { directory: "knowledge-qa-demo", port: "3001" }],
]);
const appConfiguration = appConfigurations.get(workspace);
if (!appConfiguration) {
  console.error("Usage: node scripts/layout-app-server.mjs <workspace>");
  process.exit(2);
}

const npmCommand = process.platform === "win32" ? "npm.cmd" : "npm";
const environment = {
  ...process.env,
  NEXT_PUBLIC_API_BASE_URL: "http://127.0.0.1:4100",
  NEXT_TELEMETRY_DISABLED: "1",
  VITARISH_LAYOUT_TEST_FIXTURES: "1",
};

if (process.env.VITARISH_LAYOUT_SKIP_BUILD !== "1") {
  const buildExitCode = await run(["run", "build", "--workspace", workspace]);
  if (buildExitCode !== 0) process.exit(buildExitCode);
}

const appDirectory = resolve("apps", appConfiguration.directory);
const standaloneAppDirectory = join(
  appDirectory,
  ".next",
  "standalone",
  "apps",
  appConfiguration.directory
);
copyIfPresent(join(appDirectory, ".next", "static"), join(standaloneAppDirectory, ".next", "static"));
copyIfPresent(join(appDirectory, "public"), join(standaloneAppDirectory, "public"));

const server = spawn(process.execPath, [join(standaloneAppDirectory, "server.js")], {
  env: {
    ...environment,
    HOSTNAME: "127.0.0.1",
    PORT: appConfiguration.port,
  },
  stdio: "inherit",
});

for (const signal of ["SIGINT", "SIGTERM"]) {
  process.on(signal, () => {
    if (!server.killed) server.kill(signal);
  });
}

server.on("error", (error) => {
  console.error(`Unable to start ${workspace}: ${error.message}`);
  process.exit(1);
});

server.on("exit", (code, signal) => {
  if (signal) process.kill(process.pid, signal);
  else process.exit(code ?? 1);
});

function run(args) {
  return new Promise((resolveExitCode) => {
    const child = spawn(npmCommand, args, {
      env: environment,
      shell: process.platform === "win32",
      stdio: "inherit",
    });
    child.on("error", (error) => {
      console.error(`Unable to run npm for ${workspace}: ${error.message}`);
      resolveExitCode(1);
    });
    child.on("exit", (code) => resolveExitCode(code ?? 1));
  });
}

function copyIfPresent(source, destination) {
  if (existsSync(source)) {
    cpSync(source, destination, { force: true, recursive: true });
  }
}
