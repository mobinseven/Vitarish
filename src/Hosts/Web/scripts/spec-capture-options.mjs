// Capture options the spec runner (scripts/run-specs.mjs) passes to the mock and E2E Playwright configs.

export const captureScreenshotsEnvironmentKey = "VITARISH_CAPTURE_SCREENSHOTS";
export const captureVideoEnvironmentKey = "VITARISH_CAPTURE_VIDEO";
export const captureColorSchemeEnvironmentKey = "VITARISH_CAPTURE_COLOR_SCHEME";
export const captureViewportEnvironmentKey = "VITARISH_CAPTURE_VIEWPORT";
export const mockScreenshotScenarioEnvironmentKey = "VITARISH_SCREENSHOT_SCENARIO";
export const mockScreenshotOutputEnvironmentKey = "VITARISH_SCREENSHOT_OUTPUT";
export const mockAppEnvironmentKey = "VITARISH_MOCK_APP";

export const captureColorSchemes = /** @type {const} */ (["light", "dark"]);
export const captureViewports = /** @type {const} */ (["desktop", "mobile"]);
export const mockScenarios = /** @type {const} */ (["ready", "empty", "loading", "error"]);

// Apps the mock specs capture. A mock spec belongs to the owner app unless another app lists it.
export const defaultMockApp = "private-practice-owner";
export const landingMockApp = "vitarish-landing";
export const mockApps = {
  [defaultMockApp]: { workspace: "@vitarish/private-practice-owner", url: "http://127.0.0.1:3000", specs: [] },
  [landingMockApp]: { workspace: "@vitarish/vitarish-landing", url: "http://127.0.0.1:3002", specs: ["landing.spec.ts"] },
};

/** The app a mock spec file captures. */
export function mockAppForSpec(file) {
  return Object.entries(mockApps).find(([, app]) => app.specs.includes(file))?.[0] ?? defaultMockApp;
}

/** Where the screenshots of one mock app are written, relative to the Web root. */
export function mockScreenshotRootFor(app) {
  return `artifacts/${app}-screenshots`;
}
