import { expect, test, type Page } from "@playwright/test";
import path from "node:path";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { enMessages } from "../../apps/private-practice-owner/src/localization/messages/en";
import { faMessages } from "../../apps/private-practice-owner/src/localization/messages/fa";
import { mockScreenshotOutputEnvironmentKey, mockScreenshotScenarioEnvironmentKey } from "../../scripts/spec-capture-options.mjs";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import { supportedLocales, type SupportedLocale } from "../../shared/localization/src/locales";

// Walks the self-service clinic setup wizard, invalid handoff recovery, and dashboard logo prompt against the mock API.
// Only the ready scenario answers onboarding mutations; the other scenarios are covered by pages.spec.ts.
const scenario = process.env[mockScreenshotScenarioEnvironmentKey] ?? "ready";
const outputRoot = process.env[mockScreenshotOutputEnvironmentKey] ?? "artifacts/private-practice-owner-screenshots";
const settleDelayMilliseconds = 500;
const networkIdleTimeoutMilliseconds = 15_000;
// The development server compiles a page on its first visit.
const pageCompileTimeoutMilliseconds = 60_000;
const ownerMessages = { en: enMessages, fa: faMessages };

const clinicNames: Record<SupportedLocale, string> = {
  en: "Rezaei Clinic",
  fa: "مطب رضایی",
};
const takenSubdomain = "rezaei-taken";
const availableSubdomain = "rezaei";
const clinicDescriptions: Record<SupportedLocale, string> = {
  en: "Dermatology and skin care consultations by appointment.",
  fa: "مشاوره و درمان تخصصی پوست و مو با تعیین وقت قبلی.",
};
const provinces: Record<SupportedLocale, string> = {
  en: "Tehran",
  fa: "تهران",
};
const cities: Record<SupportedLocale, string> = {
  en: "Tehran",
  fa: "تهران",
};
const streets: Record<SupportedLocale, string> = {
  en: "Valiasr Street, No. 42",
  fa: "خیابان ولیعصر، پلاک ۴۲",
};
const phoneNumber = "021 1234 5678";

test.describe.configure({ mode: "parallel" });

for (const locale of supportedLocales) {
  const journey = ownerMessages[locale].journey;
  const setup = journey.setup;

  test(`${scenario} clinic-setup-flow ${locale}`, async ({ context, page }, testInfo) => {
    test.skip(scenario !== "ready", "Only the ready scenario answers clinic setup requests.");
    test.setTimeout(11 * 180_000);
    const origin = new URL(testInfo.project.use.baseURL ?? "").origin;
    await context.addCookies([{ name: localeCookieName, value: locale, url: origin }]);
    let step = 0;
    const capture = async (name: string) => {
      await settle(page);
      step += 1;
      await page.screenshot({
        fullPage: true,
        path: path.join(outputRoot, scenario, `${locale}-${testInfo.project.name}`, "mock-clinic-setup-flow", `${String(step).padStart(2, "0")}-${name}.png`),
      });
    };

    // A required field's accessible name ends with its asterisk.
    const nameField = page.getByRole("textbox", { name: `${setup.name}*`, exact: true });
    const addressField = page.getByRole("textbox", { name: `${setup.address}*`, exact: true });

    await test.step("setup empty", async () => {
      await open(page, Routes.Setup);
      await expect(page.getByRole("heading", { name: setup.title })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
      await capture("setup-empty");
    });

    await test.step("setup address taken", async () => {
      await nameField.fill(clinicNames[locale]);
      await addressField.fill(takenSubdomain);
      await expect(page.getByText(setup.availability.Taken)).toBeVisible();
      await capture("setup-address-taken");
    });

    await test.step("setup address available", async () => {
      await addressField.fill(availableSubdomain);
      await expect(page.getByText(setup.availability.Available)).toBeVisible();
      await capture("setup-address-available");
    });

    await test.step("setup profile", async () => {
      await page.getByRole("button", { name: setup.next }).click();
      await expect(page.getByText(setup.specialty, { exact: true })).toBeVisible();
      await capture("setup-profile");
    });

    await test.step("setup specialty chosen", async () => {
      await page.getByRole("button", { name: setup.specialty }).click();
      await page.getByRole("option", { name: setup.specialties.dermatology }).click();
      await page.getByRole("textbox", { name: setup.clinicDescription, exact: true }).fill(clinicDescriptions[locale]);
      await capture("setup-specialty-chosen");
    });

    await test.step("setup details open", async () => {
      await page.getByRole("button", { name: setup.details.title }).click();
      await page.getByRole("textbox", { name: setup.details.province, exact: true }).fill(provinces[locale]);
      await page.getByRole("textbox", { name: setup.details.city, exact: true }).fill(cities[locale]);
      await page.getByRole("textbox", { name: setup.details.street, exact: true }).fill(streets[locale]);
      await page.getByRole("textbox", { name: setup.details.phone, exact: true }).first().fill(phoneNumber);
      await page.getByRole("heading", { name: setup.details.addressTitle }).scrollIntoViewIfNeeded();
      await capture("setup-details-open");
    });

    await test.step("setup details hours", async () => {
      await page.getByRole("button", { name: setup.details.dayStateLabel.replace("{day}", setup.days.Saturday) }).click();
      await page.getByRole("option", { name: setup.details.dayState.open, exact: true }).click();
      await page.getByRole("button", { name: setup.details.dayStateLabel.replace("{day}", setup.days.Friday) }).click();
      await page.getByRole("option", { name: setup.details.dayState.closed, exact: true }).click();
      await page.getByRole("heading", { name: setup.details.hoursTitle }).scrollIntoViewIfNeeded();
      await capture("setup-details-hours");
    });

    await test.step("setup ready", async () => {
      await page.getByRole("button", { name: setup.create }).click();
      await expect(page.getByRole("heading", { name: setup.ready.title })).toBeVisible();
      await capture("setup-ready");
    });

    await test.step("handoff failed", async () => {
      await open(page, Routes.Handoff);
      await expect(page.getByRole("heading", { name: journey.handoff.failedTitle })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
      await capture("handoff-failed");
    });

    const logoPromptHeading = page.getByRole("heading", { name: journey.logoPrompt.title });

    await test.step("home logo prompt", async () => {
      await open(page, Routes.Home);
      await expect(logoPromptHeading).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
      await logoPromptHeading.scrollIntoViewIfNeeded();
      await capture("home-logo-prompt");
    });

    await test.step("home logo prompt dismissed", async () => {
      await page.getByRole("button", { name: journey.logoPrompt.dismiss }).click();
      await expect(logoPromptHeading).toHaveCount(0);
      await capture("home-logo-prompt-dismissed");
    });
  });
}

async function open(page: Page, pagePath: string) {
  await page.goto(pagePath, { waitUntil: "load" });
  await page.waitForFunction(() => navigator.serviceWorker.controller !== null);
  // The mock server runs in development mode; keep the framework's development indicator out of the captures.
  await page.addStyleTag({ content: "nextjs-portal { display: none !important; }" });
}

async function settle(page: Page) {
  await page.waitForLoadState("networkidle", { timeout: networkIdleTimeoutMilliseconds }).catch(() => undefined);
  await page.waitForTimeout(settleDelayMilliseconds);
}
