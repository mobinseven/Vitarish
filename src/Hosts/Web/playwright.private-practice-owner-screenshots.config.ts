import { defineConfig, devices } from "@playwright/test";
import {
  captureColorSchemes,
  captureVideoEnvironmentKey,
  captureViewports,
  mockScreenshotScenarioEnvironmentKey,
} from "./scripts/spec-capture-options.mjs";

/** Captures every private-practice-owner page against the mock API; run through scripts/private-practice-owner-screenshots.mjs. */
const viewportOptions = {
  desktop: { viewport: { width: 1440, height: 900 } },
  mobile: { ...devices["Pixel 7"], browserName: "chromium" as const },
};
// Each scenario runs in its own Playwright pass, which clears its output folder first.
const scenario = process.env[mockScreenshotScenarioEnvironmentKey] ?? "ready";

export default defineConfig({
  testDir: "./tests/mock",
  outputDir: `./artifacts/playwright-results/private-practice-owner-screenshots/${scenario}`,
  fullyParallel: true,
  workers: 2,
  timeout: 180_000,
  reporter: [["line"]],
  use: {
    baseURL: "http://127.0.0.1:3000",
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
