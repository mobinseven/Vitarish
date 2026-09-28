import { defineConfig, devices } from "@playwright/test";
import { cspBrowserEnvironment } from "./scripts/csp-browser-environment.cjs";

const origins = JSON.parse(process.env[cspBrowserEnvironment.appOrigins] ?? "{}") as Record<string, string>;

export default defineConfig({
  timeout: 90_000,
  testDir: "./tests/csp-browser",
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  workers: 1,
  outputDir: "artifacts/csp-browser-e2e/test-results",
  reporter: [["list"], ["html", { outputFolder: "artifacts/csp-browser-e2e/report", open: "never" }]],
  use: {
    baseURL: origins["private-practice-owner"],
    ignoreHTTPSErrors: true,
    trace: "off",
    screenshot: "only-on-failure",
  },
  projects: [
    { name: "chromium", use: { ...devices["Desktop Chrome"], browserName: "chromium", launchOptions: { args: ["--use-fake-ui-for-media-stream", "--use-fake-device-for-media-stream"] } } },
    { name: "firefox", use: { ...devices["Desktop Firefox"], browserName: "firefox" } },
    { name: "webkit", use: { ...devices["Desktop Safari"], browserName: "webkit" } },
  ],
});
