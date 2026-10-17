import { expect, test, type Locator, type Page } from "@playwright/test";
import path from "node:path";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { clinicMessageCatalogs } from "../../shared/clinic/src/localization/catalogs";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import { supportedLocales } from "../../shared/localization/src/locales";
import { mockFixtureIds as ids } from "../../shared/mock-runtime/src/mockStore";

// Walks the SEA-backed clinic features of the owner app against the stateful mock API and captures
// each step: template catalog, starting a procedure from a template, working its steps, and the event workspace.
// Only the ready scenario has SEA records to work with; the other scenarios are covered by pages.spec.ts.
const scenario = process.env.VITARISH_SCREENSHOT_SCENARIO ?? "ready";
const outputRoot = process.env.VITARISH_SCREENSHOT_OUTPUT ?? "artifacts/private-practice-owner-screenshots";
const settleDelayMilliseconds = 500;
const networkIdleTimeoutMilliseconds = 15_000;
// Seeded mock records the flow works with, and what it types into the forms.
const templateProjectName = "Orthodontic care";
const templateProcedureName = "Clear aligner treatment";
const openStepTitle = "Impressions";
const newProcedureName = "Aligner treatment (lower arch)";
const flowNote = "Lower arch scanned; aligners ordered.";

test.describe.configure({ mode: "parallel" });

for (const locale of supportedLocales) {
  const procedures = clinicMessageCatalogs[locale].clinic.procedures;

  test(`${scenario} sea-flow ${locale}`, async ({ context, page }, testInfo) => {
    test.skip(scenario !== "ready", "SEA records exist only in the ready scenario.");
    const origin = new URL(testInfo.project.use.baseURL ?? "").origin;
    await context.addCookies([{ name: localeCookieName, value: locale, url: origin }]);
    let step = 0;
    const capture = async (name: string) => {
      await settle(page);
      step += 1;
      await page.screenshot({
        fullPage: true,
        path: path.join(outputRoot, scenario, `${locale}-${testInfo.project.name}`, "mock-sea-flow", `${String(step).padStart(2, "0")}-${name}.png`),
      });
    };

    await test.step("procedure template catalog", async () => {
      await open(page, Routes.Clinic.Settings.ProcedureTemplates);
      await capture("procedure-templates");

      // Narrow screens open a category on its own; wide screens show it beside the list.
      await page.getByRole("button", { name: templateProjectName }).first().click();
      await expect(page.getByText(templateProcedureName).filter({ visible: true }).first()).toBeVisible();
      await capture("template-category");
    });

    await test.step("start a procedure from a template", async () => {
      await open(page, Routes.Clinic.Patients.Detail(ids.patient));
      const newProcedure = await revealProcedures(page, procedures.title, procedures.new);
      await newProcedure.scrollIntoViewIfNeeded();
      await capture("patient-procedures");

      await newProcedure.click();
      const picker = page.getByRole("dialog");
      await expect(picker).toBeVisible();
      await capture("template-picker");

      await picker.getByRole("button", { name: templateProjectName }).click();
      await capture("template-expanded");

      await picker.getByRole("button", { name: templateProcedureName }).click();
      await picker.getByRole("textbox").fill(newProcedureName);
      await capture("template-name");

      await picker.getByRole("button", { name: procedures.templates.createFrom }).click();
      await expect(picker).toBeHidden();
      const createdProcedure = page.getByText(newProcedureName).filter({ visible: true }).first();
      await expect(createdProcedure).toBeVisible();
      await createdProcedure.scrollIntoViewIfNeeded();
      await capture("procedure-created");
    });

    await test.step("work the procedure's steps", async () => {
      await open(page, Routes.Clinic.Patients.Procedure(ids.patient, ids.seaProcedure));
      await capture("procedure-timeline");

      const openStep = page.getByRole("button", { name: openStepTitle, exact: true });
      await openStep.click();
      await capture("step-expanded");

      await page.getByRole("button", { name: procedures.items.title, exact: true }).click();
      await capture("step-attachments");
      await page.keyboard.press("Escape");

      await page.getByRole("button", { name: procedures.events.commit, exact: true }).click();
      const confirmation = page.getByRole("alertdialog");
      await expect(confirmation).toBeVisible();
      await capture("commit-confirmation");

      await confirmation.getByRole("button", { name: procedures.events.commitConfirmation.confirmCommit }).click();
      await expect(confirmation).toBeHidden();
      await expect(page.getByRole("button", { name: procedures.events.reopen, exact: true }).last()).toBeVisible();
      await capture("step-completed");

      await page.getByRole("button", { name: procedures.events.actionMenu.label }).first().click();
      await capture("procedure-actions");
      await page.keyboard.press("Escape");
    });

    await test.step("event workspace", async () => {
      await open(page, Routes.Clinic.Patients.ProcedureEvent(ids.patient, ids.seaProcedure, ids.seaEvent));
      await expect(page.getByRole("heading", { name: openStepTitle })).toBeVisible();
      await capture("event-workspace");

      const access = page.getByRole("heading", { name: procedures.access.title });
      await access.scrollIntoViewIfNeeded();
      await capture("event-access");

      const composer = page.getByPlaceholder(procedures.items.composerPlaceholder);
      await composer.fill(flowNote);
      await page.getByRole("button", { name: procedures.items.send, exact: true }).click();
      // The composer keeps a draft that failed to save, so an emptied composer means the note was stored.
      await expect(composer).toHaveValue("");
      await expect(page.getByText(flowNote)).toBeVisible();
      await capture("event-note-added");
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

/** On narrow screens the patient's procedures sit behind a tab; wide screens show them inline. */
async function revealProcedures(page: Page, tabName: string, newProcedureName: string): Promise<Locator> {
  const newProcedure = page.getByRole("button", { name: newProcedureName }).filter({ visible: true }).first();
  const proceduresTab = page.getByRole("tab", { name: tabName }).filter({ visible: true });
  await expect(newProcedure.or(proceduresTab).first()).toBeVisible();
  if (!(await newProcedure.isVisible())) await proceduresTab.click();
  await expect(newProcedure).toBeVisible();
  return newProcedure;
}
