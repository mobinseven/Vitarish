import { expect, test, type Page } from "@playwright/test";
import path from "node:path";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { mockScreenshotOutputEnvironmentKey, mockScreenshotScenarioEnvironmentKey } from "../../scripts/spec-capture-options.mjs";
import { clinicMessageCatalogs } from "../../shared/clinic/src/localization/catalogs";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import { supportedLocales } from "../../shared/localization/src/locales";

// Walks surgery type management of the owner app against the stateful mock API and captures each step:
// the default type the app creates for a new practice, a new type being filled in, and the saved type.
// Only the ready scenario answers with saved records; the other scenarios are covered by pages.spec.ts.
const scenario = process.env[mockScreenshotScenarioEnvironmentKey] ?? "ready";
const outputRoot = process.env[mockScreenshotOutputEnvironmentKey] ?? "artifacts/private-practice-owner-screenshots";
const settleDelayMilliseconds = 500;
const networkIdleTimeoutMilliseconds = 15_000;
// What the flow types into the forms.
const typeName = "Rhinoplasty";
const tools = ["Nasal speculum", "Osteotome"];

test.describe.configure({ mode: "parallel" });

for (const locale of supportedLocales) {
  const surgeries = clinicMessageCatalogs[locale].clinic.surgeries;
  const types = surgeries.types;

  test(`${scenario} surgery-types-flow ${locale}`, async ({ context, page }, testInfo) => {
    test.skip(scenario !== "ready", "Only the ready scenario keeps saved surgery types.");
    const origin = new URL(testInfo.project.use.baseURL ?? "").origin;
    await context.addCookies([{ name: localeCookieName, value: locale, url: origin }]);
    let step = 0;
    const capture = async (name: string) => {
      await settle(page);
      step += 1;
      await page.screenshot({
        fullPage: true,
        path: path.join(outputRoot, scenario, `${locale}-${testInfo.project.name}`, "mock-surgery-types-flow", `${String(step).padStart(2, "0")}-${name}.png`),
      });
    };
    const typeList = page.getByRole("region", { name: types.listTitle });
    // A required field's accessible name ends with its asterisk.
    const nameField = page.getByRole("textbox", { name: `${types.nameLabel}*`, exact: true });

    await test.step("default type", async () => {
      await open(page, Routes.Clinic.Settings.SurgeryTypes);
      await expect(typeList.getByRole("button", { name: surgeries.defaultType.name })).toBeVisible();
      await capture("types");
    });

    await test.step("new type", async () => {
      await page.getByRole("button", { name: types.newType }).click();
      await nameField.fill(typeName);
      for (const tool of tools) {
        await page.getByRole("textbox", { name: types.toolsLabel }).fill(tool);
        await page.getByRole("button", { name: types.addTool }).click();
      }
      await capture("new-type-filled");

      await page.getByRole("button", { name: types.save }).click();
      // The saved type stays open for editing; narrow screens show only the editor.
      await expect(page.getByRole("heading", { name: types.editTitle })).toBeVisible();
      await expect(nameField).toHaveValue(typeName);
      await capture("type-saved");
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
