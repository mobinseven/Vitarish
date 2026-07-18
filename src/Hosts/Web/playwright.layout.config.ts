import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./tests/layout",
  outputDir: "./artifacts/playwright-results",
  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 1 : 0,
  workers: process.env.CI ? 2 : undefined,
  reporter: [
    ["line"],
    ["html", { open: "never", outputFolder: "artifacts/playwright-report" }],
  ],
  expect: {
    timeout: 10_000,
    toHaveScreenshot: {
      animations: "disabled",
      caret: "hide",
      maxDiffPixelRatio: 0.005,
      scale: "css",
    },
  },
  snapshotPathTemplate: "{testDir}/{testFilePath}-snapshots/{arg}-{projectName}{ext}",
  use: {
    baseURL: "http://127.0.0.1:3000",
    browserName: "chromium",
    colorScheme: "light",
    reducedMotion: "reduce",
    screenshot: "only-on-failure",
    serviceWorkers: "block",
    trace: "retain-on-failure",
    video: "off",
    launchOptions: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH
      ? { executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH }
      : undefined,
  },
  projects: [{ name: "chromium", use: { browserName: "chromium" } }],
  webServer: [
    {
      command: "node scripts/layout-fixture-server.mjs",
      name: "layout-fixture-service",
      reuseExistingServer: !process.env.CI,
      timeout: 30_000,
      url: "http://127.0.0.1:4100/health",
    },
    {
      command:
        "node scripts/layout-app-server.mjs @vitarish/private-practice-general",
      name: "private-practice-general",
      reuseExistingServer: !process.env.CI,
      timeout: 300_000,
      url: "http://127.0.0.1:3000",
    },
    {
      command: "node scripts/layout-app-server.mjs @vitarish/knowledge-qa-demo",
      name: "knowledge-qa-demo",
      reuseExistingServer: !process.env.CI,
      timeout: 300_000,
      url: "http://127.0.0.1:3001",
    },
  ],
});
