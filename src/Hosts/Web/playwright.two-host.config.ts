import { defineConfig, devices } from "@playwright/test";
import { resolve } from "node:path";
import {
  twoHostE2eArtifactDirectoryEnvironmentKey,
  twoHostE2eGrepEnvironmentKey,
} from "./scripts/two-host-e2e-artifacts.mjs";

const artifactDirectory = process.env[twoHostE2eArtifactDirectoryEnvironmentKey] ?? "artifacts/two-host-e2e";

export default defineConfig({
  timeout: 120_000,
  testDir: "./tests/two-host",
  globalSetup: "./tests/two-host/global-setup.ts",
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  grep: process.env[twoHostE2eGrepEnvironmentKey] ? new RegExp(process.env[twoHostE2eGrepEnvironmentKey]) : undefined,
  outputDir: resolve(artifactDirectory, "test-results"),
  reporter: [["list"], ["html", { outputFolder: resolve(artifactDirectory, "report"), open: "never" }]],
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3005",
    ...devices["Desktop Chrome"],
    ignoreHTTPSErrors: true,
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
});
