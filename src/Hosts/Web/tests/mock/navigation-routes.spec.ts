import { expect, test, type Page } from "@playwright/test";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { mockScreenshotScenarioEnvironmentKey } from "../../scripts/spec-capture-options.mjs";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import { defaultLocale } from "../../shared/localization/src/locales";
import { mockFixtureIds as ids } from "../../shared/mock-runtime/src/mockStore";

/**
 * Every address of the clinic app the sidebar leads to or a page links to. A route added to `Routes.Clinic` belongs
 * here, so each page is checked for the invariants below: it renders without a page error or a failed API call, has
 * one h1, lights exactly one sidebar destination, and does not scroll sideways.
 */
const { Appointments, CheckIn, Patients, Reminders, Settings, Surgeries } = Routes.Clinic;

const clinicRoutes: { name: string; path: string; allowedApiFailures?: readonly RegExp[] }[] = [
  { name: "today", path: Routes.Home },
  { name: "check-in", path: CheckIn },
  { name: "patients", path: Patients.Index },
  { name: "patient-new", path: Patients.New },
  { name: "patient-overview", path: Patients.Tab(ids.patient, "overview") },
  { name: "patient-visits", path: Patients.Tab(ids.patient, "visits") },
  { name: "patient-surgeries", path: Patients.Tab(ids.patient, "surgeries") },
  { name: "patient-procedures", path: Patients.Tab(ids.patient, "procedures") },
  { name: "patient-prescriptions", path: Patients.Tab(ids.patient, "prescriptions") },
  { name: "patient-edit", path: Patients.Edit(ids.patient) },
  { name: "procedure", path: Patients.Procedure(ids.patient, ids.seaProcedure) },
  { name: "procedure-event", path: Patients.ProcedureEvent(ids.patient, ids.seaProcedure, ids.seaEvent) },
  { name: "schedule", path: Appointments.Reservations.Index },
  { name: "visit", path: Appointments.Reservations.Detail(ids.reservation) },
  { name: "surgeries", path: Surgeries.Index },
  { name: "surgery-new", path: Surgeries.New },
  { name: "surgery", path: Surgeries.Detail(ids.surgery) },
  { name: "surgery-edit", path: Surgeries.Edit(ids.surgery) },
  { name: "reminders", path: Reminders },
  { name: "settings", path: Settings.Index },
  { name: "settings-users", path: Settings.Users },
  { name: "settings-audit", path: Settings.Audit },
  { name: "settings-insurance", path: Settings.Insurance },
  { name: "settings-statistics", path: Settings.Statistics },
  { name: "settings-appointments", path: Settings.Appointments },
  { name: "settings-shifts", path: Settings.Shifts.Index },
  { name: "settings-shift-new", path: Settings.Shifts.New },
  { name: "settings-shift", path: Settings.Shifts.Detail(ids.shift) },
  { name: "settings-shift-edit", path: Settings.Shifts.Edit(ids.shift) },
  { name: "settings-shift-occurrence", path: Settings.Shifts.Occurrence(ids.shift, ids.shiftOccurrenceDate) },
  { name: "settings-services", path: Settings.Services.Index },
  { name: "settings-service", path: Settings.Services.Detail(ids.service) },
  { name: "settings-service-types", path: Settings.Services.Types.Index },
  { name: "settings-service-type", path: Settings.Services.Types.Detail(ids.serviceType) },
  { name: "settings-service-providers", path: Settings.Services.Providers.Index },
  { name: "settings-service-provider", path: Settings.Services.Providers.Detail(ids.serviceProvider) },
  { name: "settings-procedure-templates", path: Settings.ProcedureTemplates },
  { name: "settings-prescription-templates", path: Settings.PrescriptionTemplates },
  {
    name: "settings-surgery-types",
    path: Settings.SurgeryTypes,
    // The practice has no surgery settings until they are first saved, so the server answers not found.
    allowedApiFailures: [/\/api\/modules\/clinic\/surgeries\/settings(?:\?|$)/],
  },
];

const scenario = process.env[mockScreenshotScenarioEnvironmentKey] ?? "ready";
const networkIdleTimeoutMilliseconds = 15_000;
const settleDelayMilliseconds = 500;

// The first request to a route can stall while the dev server compiles it; a retry gets the compiled page.
test.describe.configure({ mode: "parallel", retries: 2 });

// Layout and structure do not differ by language, so one locale is enough; the viewport projects still run.
const locale = defaultLocale;
for (const route of clinicRoutes) {
  test(`${scenario} ${locale} navigation ${route.name}`, async ({ context, page }, testInfo) => {
    // Only the ready scenario has the records these routes read; the others show their own states elsewhere.
    test.skip(scenario !== "ready", "The route invariants are checked against the seeded practice.");

    const origin = new URL(testInfo.project.use.baseURL ?? "").origin;
    await context.addCookies([{ name: localeCookieName, value: locale, url: origin }]);

    const pageErrors: string[] = [];
    const failedApiCalls: string[] = [];
    page.on("pageerror", (error) => pageErrors.push(error.message));
    page.on("response", (response) => {
      const url = response.url();
      const allowed = route.allowedApiFailures?.some((pattern) => pattern.test(url)) ?? false;
      if (url.includes("/api/") && response.status() >= 400 && !allowed) {
        failedApiCalls.push(`${response.status()} ${new URL(url).pathname}`);
      }
    });

    await page.goto(route.path, { waitUntil: "domcontentloaded" });
    await settle(page);

    expect(pageErrors, "page errors").toEqual([]);
    expect(failedApiCalls, "failed API calls").toEqual([]);
    await expect(page.locator("h1"), "one h1").toHaveCount(1);
    await expect(page.locator('.sidebar__menu-item[data-current="true"]'), "one lit sidebar destination").toHaveCount(1);
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
    expect(overflow, "horizontal overflow in pixels").toBeLessThanOrEqual(0);
  });
}

async function settle(page: Page) {
  await page.waitForFunction(() => navigator.serviceWorker.controller !== null);
  // Pages that poll or stream never go idle; check them once the wait runs out.
  await page.waitForLoadState("networkidle", { timeout: networkIdleTimeoutMilliseconds }).catch(() => undefined);
  await page.waitForTimeout(settleDelayMilliseconds);
}
