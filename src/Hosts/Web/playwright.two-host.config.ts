import { defineConfig, devices } from "@playwright/test";
import { resolve } from "node:path";
import {
  twoHostE2eArtifactDirectoryEnvironmentKey,
  twoHostE2eGrepEnvironmentKey,
} from "./scripts/two-host-e2e-artifacts.mjs";
import {
  captureColorSchemeEnvironmentKey,
  captureScreenshotsEnvironmentKey,
  captureVideoEnvironmentKey,
  captureViewportEnvironmentKey,
} from "./scripts/spec-capture-options.mjs";

const artifactDirectory = process.env[twoHostE2eArtifactDirectoryEnvironmentKey] ?? "artifacts/two-host-e2e";
// The spec runner may ask for a theme, a mobile viewport, a screenshot of every test and video; unset, the suite keeps its defaults.
const colorScheme = process.env[captureColorSchemeEnvironmentKey] as "light" | "dark" | undefined;
const device = process.env[captureViewportEnvironmentKey] === "mobile" ? devices["Pixel 7"] : devices["Desktop Chrome"];
const screenshots = Boolean(process.env[captureScreenshotsEnvironmentKey]);
const video = Boolean(process.env[captureVideoEnvironmentKey]);

export default defineConfig({
  timeout: 120_000,
  testDir: "./tests/e2e",
  globalSetup: "./tests/e2e/global-setup.ts",
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  grep: process.env[twoHostE2eGrepEnvironmentKey] ? new RegExp(process.env[twoHostE2eGrepEnvironmentKey]) : undefined,
  outputDir: resolve(artifactDirectory, "test-results"),
  reporter: [["list"], ["html", { outputFolder: resolve(artifactDirectory, "report"), open: "never" }]],
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3005",
    ...device,
    ...(colorScheme ? { colorScheme } : {}),
    ignoreHTTPSErrors: true,
    trace: "retain-on-failure",
    screenshot: screenshots ? "on" : "only-on-failure",
    video: video ? "on" : "off",
  },
});
