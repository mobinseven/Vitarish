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
    // The front door asks for no data until a visitor starts the conversation, so only the ready scenario has something to show.
    test.skip(scenario !== "ready", "The front door renders the same in every mock scenario.");
    const origin = new URL(testInfo.project.use.baseURL ?? "").origin;
    await context.addCookies([{ name: localeCookieName, value: locale, url: origin }]);

    await page.goto(frontDoorPath, { waitUntil: "load" });
    await page.waitForFunction(() => navigator.serviceWorker.controller !== null);
    await page.waitForLoadState("networkidle");
    // The mock server runs in development mode; keep the framework's development indicator out of the captures.
    await page.addStyleTag({ content: "nextjs-portal { display: none !important; }" });
    await page.waitForTimeout(settleDelayMilliseconds);

    await page.screenshot({
      fullPage: true,
      path: path.join(outputRoot, scenario, `${locale}-${testInfo.project.name}`, "front-door.png"),
    });
  });
}
