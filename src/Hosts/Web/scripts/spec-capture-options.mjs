// Capture options the spec runner (scripts/run-specs.mjs) passes to the mock and E2E Playwright configs.

export const captureScreenshotsEnvironmentKey = "VITARISH_CAPTURE_SCREENSHOTS";
export const captureVideoEnvironmentKey = "VITARISH_CAPTURE_VIDEO";
export const captureColorSchemeEnvironmentKey = "VITARISH_CAPTURE_COLOR_SCHEME";
export const captureViewportEnvironmentKey = "VITARISH_CAPTURE_VIEWPORT";
export const mockScreenshotScenarioEnvironmentKey = "VITARISH_SCREENSHOT_SCENARIO";
export const mockScreenshotOutputEnvironmentKey = "VITARISH_SCREENSHOT_OUTPUT";

export const captureColorSchemes = /** @type {const} */ (["light", "dark"]);
export const captureViewports = /** @type {const} */ (["desktop", "mobile"]);
export const mockScenarios = /** @type {const} */ (["ready", "empty", "loading", "error"]);
