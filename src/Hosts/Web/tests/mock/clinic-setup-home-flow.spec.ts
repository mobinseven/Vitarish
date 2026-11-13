import { expect, test } from "@playwright/test";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { clinicMessageCatalogs } from "../../shared/clinic/src/localization/catalogs";
import { intelligenceMessageCatalogs } from "../../shared/intelligence/src/localization/catalogs";
import { describeSurgeryFlow } from "./surgerySteps";

// Walks the owner's assistant-guided setup home against the mock API: the welcome and goal list, a reply that
// prepares two shifts as one review group, the edit form, "Confirm all", and "Later".
const pageCompileTimeoutMilliseconds = 60_000;

describeSurgeryFlow("clinic-setup-home-flow", [async ({ page, locale, capture }) => {
  const clinic = clinicMessageCatalogs[locale].clinic;
  const group = intelligenceMessageCatalogs[locale].intelligence.guidedSurface.actionGroup;
  const operations = clinic.operationsAssistant;

  await test.step("setup home", async () => {
    await page.goto(Routes.Home, { waitUntil: "load" });
    await page.waitForFunction(() => navigator.serviceWorker.controller !== null);
    await page.addStyleTag({ content: "nextjs-portal { display: none !important; }" });
    await expect(page.getByRole("heading", { name: clinic.setup.title })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
    await capture("setup-welcome");
  });

  await test.step("prepared shifts", async () => {
    await page.getByRole("button", { name: clinic.setup.chips.ShiftsAndBookingRules }).click();
    await expect(page.getByRole("button", { name: group.confirmAll })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
    await capture("setup-prepared-group");
  });

  await test.step("edit a proposal", async () => {
    await page.getByRole("button", { name: operations.manage.edit }).first().click();
    await expect(page.getByRole("button", { name: operations.manage.save })).toBeVisible();
    await capture("setup-edit-proposal");
    await page.getByRole("button", { name: operations.manage.cancelEdit }).click();
  });

  await test.step("confirm all", async () => {
    await page.getByRole("button", { name: group.confirmAll }).click();
    await expect(page.getByRole("button", { name: group.confirmAll })).toBeHidden({ timeout: pageCompileTimeoutMilliseconds });
    await capture("setup-confirmed");
  });

  await test.step("later", async () => {
    await page.getByRole("button", { name: clinic.setup.later }).click();
    await expect(page.getByRole("heading", { name: clinic.setup.title })).toBeHidden({ timeout: pageCompileTimeoutMilliseconds });
    await capture("setup-later-dashboard");
  });
}]);
