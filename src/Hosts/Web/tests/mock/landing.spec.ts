import { test } from "@playwright/test";
import path from "node:path";
import {
  landingMockApp,
  mockScreenshotOutputEnvironmentKey,
  mockScreenshotRootFor,
  mockScreenshotScenarioEnvironmentKey,
} from "../../scripts/spec-capture-options.mjs";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import { supportedLocales } from "../../shared/localization/src/locales";

const scenario = process.env[mockScreenshotScenarioEnvironmentKey] ?? "ready";
const outputRoot = process.env[mockScreenshotOutputEnvironmentKey] ?? mockScreenshotRootFor(landingMockApp);
const frontDoorPath = "/";
const settleDelayMilliseconds = 500;

test.describe.configure({ mode: "parallel" });

for (const locale of supportedLocales) {
  test(`${scenario} ${locale} front-door`, async ({ context, page }, testInfo) => {
    // The front door asks for no data, so every mock scenario renders the same page.
    test.skip(scenario !== "ready", "The front door renders the same in every mock scenario.");
    const origin = new URL(testInfo.project.use.baseURL ?? "").origin;
    await context.addCookies([{ name: localeCookieName, value: locale, url: origin }]);

    await page.goto(frontDoorPath, { waitUntil: "load" });
    await page.waitForFunction(() => navigator.serviceWorker.controller !== null);
    await page.waitForLoadState("networkidle");
    // The splash screen covers the page until the document has loaded; the screenshot is of the page behind it.
    await page.locator("[data-splash]").waitFor({ state: "detached" });
    // The mock server runs in development mode; keep the framework's development indicator out of the captures.
    await page.addStyleTag({ content: "nextjs-portal { display: none !important; }" });
    await page.waitForTimeout(settleDelayMilliseconds);

    await page.screenshot({
      fullPage: true,
      path: path.join(outputRoot, scenario, `${locale}-${testInfo.project.name}`, "front-door.png"),
    });
  });
}

for (const locale of supportedLocales) {
  test(`${scenario} ${locale} front-door-loading`, async ({ context, page }, testInfo) => {
    test.skip(scenario !== "ready", "The front door renders the same in every mock scenario.");
    const origin = new URL(testInfo.project.use.baseURL ?? "").origin;
    await context.addCookies([{ name: localeCookieName, value: locale, url: origin }]);
    // The captures run with reduced motion; the load screen's sweep only shows without it, so record it with motion on.
    await page.emulateMedia({ reducedMotion: "no-preference" });

    await page.goto(frontDoorPath, { waitUntil: "commit" });
    const loadScreen = page.locator("[data-splash]");
    await loadScreen.waitFor({ state: "visible" });
    await loadScreen.waitFor({ state: "detached" });
    await page.waitForTimeout(settleDelayMilliseconds);
  });
}
