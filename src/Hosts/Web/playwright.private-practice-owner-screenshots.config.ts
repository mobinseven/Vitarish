import { defineConfig, devices } from "@playwright/test";

/** Captures every private-practice-owner page against the mock API; run through scripts/private-practice-owner-screenshots.mjs. */
const desktop = { viewport: { width: 1440, height: 900 } };
const mobile = { ...devices["Pixel 7"], browserName: "chromium" as const };

export default defineConfig({
  testDir: "./tests/private-practice-owner-screenshots",
  outputDir: "./artifacts/playwright-results/private-practice-owner-screenshots",
  fullyParallel: true,
  workers: 2,
  timeout: 180_000,
  reporter: [["line"]],
  use: {
    baseURL: "http://127.0.0.1:3000",
    browserName: "chromium",
    contextOptions: { reducedMotion: "reduce" },
    serviceWorkers: "allow",
    video: "off",
    launchOptions: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH
      ? { executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH }
      : undefined,
  },
  projects: [
    { name: "desktop-light", use: { ...desktop, colorScheme: "light" } },
    { name: "desktop-dark", use: { ...desktop, colorScheme: "dark" } },
    { name: "mobile-light", use: { ...mobile, colorScheme: "light" } },
    { name: "mobile-dark", use: { ...mobile, colorScheme: "dark" } },
  ],
});
