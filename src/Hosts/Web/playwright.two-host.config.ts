import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  timeout: 120_000,
  testDir: "./tests/two-host",
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  outputDir: "artifacts/two-host-e2e/test-results",
  reporter: [["list"], ["html", { outputFolder: "artifacts/two-host-e2e/report", open: "never" }]],
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3005",
    ...devices["Desktop Chrome"],
    ignoreHTTPSErrors: true,
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
});
