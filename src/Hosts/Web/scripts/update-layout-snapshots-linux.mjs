#!/usr/bin/env node

import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptDirectory, "..");
const dockerArguments = [
  "run",
  "--rm",
  "--init",
  "--ipc=host",
  "--mount",
  `type=bind,source=${webRoot},target=/work`,
  "--mount",
  "type=volume,source=vitarish-layout-node-modules-1-61-1,target=/work/node_modules",
  "--workdir",
  "/work",
  "--env",
  "CI=1",
];

if (process.env.HEROUI_AUTH_TOKEN) {
  dockerArguments.push("--env", "HEROUI_AUTH_TOKEN");
}

dockerArguments.push(
  "mcr.microsoft.com/playwright:v1.61.1-noble",
  "/bin/bash",
  "-lc",
  "npm ci && npm run test:layout:visual -- --update-snapshots"
);

const result = spawnSync("docker", dockerArguments, { stdio: "inherit" });
if (result.error) {
  console.error(`Unable to start pinned Linux Playwright container: ${result.error.message}`);
  process.exit(1);
}
process.exit(result.status ?? 1);
