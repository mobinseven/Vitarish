import { defineConfig, devices } from "@playwright/test";
import {
  captureColorSchemes,
  captureVideoEnvironmentKey,
  captureViewports,
  defaultMockApp,
  mockAppEnvironmentKey,
  mockApps,
  mockScreenshotScenarioEnvironmentKey,
} from "./scripts/spec-capture-options.mjs";

/**
 * Captures the mock specs of one app against the mock API; run through scripts/private-practice-owner-screenshots.mjs,
 * which names the app. Without it, the owner app is captured.
 */
const viewportOptions = {
  desktop: { viewport: { width: 1440, height: 900 } },
  mobile: { ...devices["Pixel 7"], browserName: "chromium" as const },
};
// Each scenario runs in its own Playwright pass, which clears its output folder first.
const scenario = process.env[mockScreenshotScenarioEnvironmentKey] ?? "ready";
const app = (process.env[mockAppEnvironmentKey] ?? defaultMockApp) as keyof typeof mockApps;
// The owner app takes every spec that no other app lists.
const otherAppSpecs = Object.entries(mockApps).flatMap(([name, { specs }]) => (name === app ? [] : specs));
const appSpecs: string[] = mockApps[app].specs;

export default defineConfig({
  testDir: "./tests/mock",
  ...(appSpecs.length > 0 ? { testMatch: appSpecs } : { testIgnore: otherAppSpecs }),
  outputDir: `./artifacts/playwright-results/${app}-screenshots/${scenario}`,
  fullyParallel: true,
  workers: 2,
  timeout: 180_000,
  reporter: [["line"]],
  use: {
    baseURL: mockApps[app].url,
    browserName: "chromium",
    contextOptions: { reducedMotion: "reduce" },
    serviceWorkers: "allow",
    video: process.env[captureVideoEnvironmentKey] ? "on" : "off",
    launchOptions: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH
      ? { executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH }
      : undefined,
  },
  projects: captureViewports.flatMap((viewport) =>
    captureColorSchemes.map((colorScheme) => ({
      name: `${viewport}-${colorScheme}`,
      use: { ...viewportOptions[viewport], colorScheme },
    })),
  ),
});
