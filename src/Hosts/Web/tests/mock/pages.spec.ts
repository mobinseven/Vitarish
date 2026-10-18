import { test, type Page } from "@playwright/test";
import path from "node:path";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { mockScreenshotOutputEnvironmentKey, mockScreenshotScenarioEnvironmentKey } from "../../scripts/spec-capture-options.mjs";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import { supportedLocales } from "../../shared/localization/src/locales";
import { mockAnonymousSessionValue, mockSessionCookieName } from "../../shared/mock-runtime/src/mockSession";
import { mockFixtureIds as ids } from "../../shared/mock-runtime/src/mockStore";

type Session = "signed-in" | "signed-out";
type ScreenshotPage = { name: string; path: string; session: Session };

const signedOut = (name: string, pagePath: string): ScreenshotPage => ({ name, path: pagePath, session: "signed-out" });
const signedIn = (name: string, pagePath: string): ScreenshotPage => ({ name, path: pagePath, session: "signed-in" });
// Test-only fixture route; served when the fixture flag is on.
const colorLegendPath = "/color-legend";
const { Clinic } = Routes;
const { Appointments, Patients, Settings } = Clinic;

const pages: ScreenshotPage[] = [
  signedOut("login", Routes.Login.Index),
  signedOut("login-password", Routes.Login.Password),
  signedOut("login-phone", Routes.Login.Phone),
  signedOut("login-email", Routes.Login.Email),
  signedOut("login-passkey", Routes.Login.Passkey),
  signedOut("login-forgot-password", Routes.Login.ForgotPassword),
  signedIn("home", Routes.Home),
  signedIn("reservations", Appointments.Reservations.Index),
  signedIn("patients", Patients.Index),
  signedIn("patient", Patients.Detail(ids.patient)),
  signedIn("settings-audit", Settings.Audit),
  signedIn("sea-procedure-templates", Settings.ProcedureTemplates),
  signedIn("sea-procedure", Patients.Procedure(ids.patient, ids.seaProcedure)),
  signedIn("sea-procedure-event", Patients.ProcedureEvent(ids.patient, ids.seaProcedure, ids.seaEvent)),
  signedIn("color-legend", colorLegendPath),
];

const scenario = process.env[mockScreenshotScenarioEnvironmentKey] ?? "ready";
const outputRoot = process.env[mockScreenshotOutputEnvironmentKey] ?? "artifacts/private-practice-owner-screenshots";
const loadingCaptureDelayMilliseconds = 300;
const settleDelayMilliseconds = 500;
const networkIdleTimeoutMilliseconds = 15_000;

test.describe.configure({ mode: "parallel" });

for (const locale of supportedLocales) {
  for (const screenshotPage of pages) {
    test(`${scenario} ${screenshotPage.session} ${locale} ${screenshotPage.name}`, async ({ context, page }, testInfo) => {
      const origin = new URL(testInfo.project.use.baseURL ?? "").origin;
      await context.addCookies([
        { name: localeCookieName, value: locale, url: origin },
        ...(screenshotPage.session === "signed-out"
          ? [{ name: mockSessionCookieName, value: mockAnonymousSessionValue, url: origin }]
          : []),
      ]);

      await page.goto(screenshotPage.path, { waitUntil: "load" });
      await waitForMockedPage(page);
      // The mock server runs in development mode; keep the framework's development indicator out of the captures.
      await page.addStyleTag({ content: "nextjs-portal { display: none !important; }" });

      await page.screenshot({
        fullPage: true,
        path: path.join(
          outputRoot,
          scenario,
          `${locale}-${testInfo.project.name}`,
          `${screenshotPage.session}-${screenshotPage.name}.png`,
        ),
      });
    });
  }
}

async function waitForMockedPage(page: Page) {
  await page.waitForFunction(() => navigator.serviceWorker.controller !== null);
  if (scenario === "loading") {
    // The loading scenario delays every mocked response; capture before they resolve.
    await page.waitForTimeout(loadingCaptureDelayMilliseconds);
    return;
  }
  // Pages that poll or stream never go idle; capture them once the wait runs out.
  await page.waitForLoadState("networkidle", { timeout: networkIdleTimeoutMilliseconds }).catch(() => undefined);
  await page.waitForTimeout(settleDelayMilliseconds);
}
