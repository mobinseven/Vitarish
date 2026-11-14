import { expect, test } from "@playwright/test";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { clinicMessageCatalogs } from "../../shared/clinic/src/localization/catalogs";
import { intelligenceMessageCatalogs } from "../../shared/intelligence/src/localization/catalogs";
import { describeSurgeryFlow } from "./surgerySteps";

// Walks the owner's full-page assistant against the mock API: the starters, a reply with a prepared review group,
// a patient chosen from search results continuing in the same timeline, returning to the clinic, and a new conversation.
const pageCompileTimeoutMilliseconds = 60_000;

describeSurgeryFlow("clinic-assistant-flow", [async ({ page, locale, capture }) => {
  const clinic = clinicMessageCatalogs[locale].clinic;
  const workspace = clinic.assistantWorkspace;
  const group = intelligenceMessageCatalogs[locale].intelligence.guidedSurface.actionGroup;

  await test.step("starters", async () => {
    await page.goto(Routes.Clinic.Assistant, { waitUntil: "load" });
    await page.waitForFunction(() => navigator.serviceWorker.controller !== null);
    await page.addStyleTag({ content: "nextjs-portal { display: none !important; }" });
    await expect(page.getByRole("heading", { name: workspace.suggestions.title })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
    await capture("assistant-starters");
  });

  await test.step("prepared shifts", async () => {
    await page.getByRole("button", { name: new RegExp(workspace.suggestions.items.addShift.title) }).click();
    await expect(page.getByRole("button", { name: group.confirmAll })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
    await capture("assistant-prepared-group");
  });

  await test.step("patient search results", async () => {
    await page.getByRole("textbox", { name: clinic.operationsAssistant.composer.label }).fill(workspace.suggestions.items.findPatient.prompt);
    await page.getByRole("button", { name: clinic.operationsAssistant.composer.send }).click();
    // The newest turn is scrolled into view, below the earlier review cards.
    await expect(page.getByRole("region", { name: clinic.operationsAssistant.candidates.label })).toBeInViewport({ timeout: pageCompileTimeoutMilliseconds });
    await capture("assistant-patient-candidates");
  });

  await test.step("patient focus", async () => {
    await page.getByRole("button", { name: clinic.operationsAssistant.candidates.select }).click();
    await expect(page.getByRole("group", { name: workspace.focus.label })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
    await expect(page.getByRole("separator").filter({ hasText: workspace.nowAbout.split("{")[0].trim() })).toBeInViewport();
    await capture("assistant-patient-focus");
  });

  await test.step("back to the clinic", async () => {
    await page.getByRole("button", { name: workspace.focus.clear }).click();
    await expect(page.getByRole("separator", { name: workspace.backToClinic })).toBeInViewport();
    await capture("assistant-back-to-clinic");
  });

  await test.step("new conversation", async () => {
    await page.getByRole("button", { name: workspace.newConversation }).click();
    await expect(page.getByRole("dialog")).toBeVisible();
    await capture("assistant-new-conversation");
  });
}]);
