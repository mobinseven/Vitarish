import { expect, test, type Locator, type Page } from "@playwright/test";
import path from "node:path";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { enMessages } from "../../apps/private-practice-owner/src/localization/messages/en";
import { faMessages } from "../../apps/private-practice-owner/src/localization/messages/fa";
import { mockScreenshotOutputEnvironmentKey, mockScreenshotScenarioEnvironmentKey } from "../../scripts/spec-capture-options.mjs";
import { clinicMessageCatalogs } from "../../shared/clinic/src/localization/catalogs";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import { supportedLocales, type SupportedLocale } from "../../shared/localization/src/locales";
import { mockFixtureIds as ids } from "../../shared/mock-runtime/src/mockStore";

// The surgery steps the surgery flows walk against the stateful mock API. Each step opens its own page,
// because mock records last for one page load; a flow spec registers the steps it captures.
const scenario = process.env[mockScreenshotScenarioEnvironmentKey] ?? "ready";
const outputRoot = process.env[mockScreenshotOutputEnvironmentKey] ?? "artifacts/private-practice-owner-screenshots";
const settleDelayMilliseconds = 500;
const networkIdleTimeoutMilliseconds = 15_000;
// The development server compiles a page on its first visit.
const pageCompileTimeoutMilliseconds = 60_000;
const ownerMessages = { en: enMessages, fa: faMessages };
// What the steps type into the forms. The plan runs into the mock's surgery at 13:00 today in the same room.
const typeName = "Rhinoplasty";
const editedTypeName = "Open rhinoplasty";
const tools = ["Nasal speculum", "Osteotome"];
const planTool = "Headlight";
const planHour = "11";
const planMinute = "30";
const planDurationMinutes = "120";
const planLocation = "OR 1";
const editedName = "Arthroscopic meniscus repair";
const editedDurationMinutes = "150";
const editedLocation = "OR 3";
const editedTool = "Arthroscope";
const reminderMinutes = "1440";
const reminderSubject = "Fasting from midnight";
const reminderMessage = "Please do not eat or drink after midnight before your surgery.";
const cancellationSubject = "Your surgery is cancelled";
const cancellationMessage = "The clinic will call you to choose a new date.";
const assistantRequest = "Cancel today's knee arthroscopy.";

type SurgeryFlowContext = {
  page: Page;
  locale: SupportedLocale;
  capture: (name: string) => Promise<void>;
};

export type SurgeryFlowStep = (flow: SurgeryFlowContext) => Promise<void>;

/** Registers one test per locale that walks the steps and captures them into `mock-<flow>/`. */
export function describeSurgeryFlow(flow: string, steps: readonly SurgeryFlowStep[]) {
  test.describe.configure({ mode: "parallel" });

  for (const locale of supportedLocales) {
    test(`${scenario} ${flow} ${locale}`, async ({ context, page }, testInfo) => {
      test.skip(scenario !== "ready", "Only the ready scenario keeps saved surgeries.");
      // Each step pays the development server's first compile of its pages.
      test.setTimeout(steps.length * 180_000);
      const origin = new URL(testInfo.project.use.baseURL ?? "").origin;
      await context.addCookies([{ name: localeCookieName, value: locale, url: origin }]);
      let step = 0;
      const capture = async (name: string) => {
        await settle(page);
        step += 1;
        await page.screenshot({
          fullPage: true,
          path: path.join(outputRoot, scenario, `${locale}-${testInfo.project.name}`, `mock-${flow}`, `${String(step).padStart(2, "0")}-${name}.png`),
        });
      };
      for (const run of steps) await run({ page, locale, capture });
    });
  }
}

/** Surgery type management: create a type with every default, edit it, delete it, and set the cancellation message. */
export const surgeryTypeSteps: SurgeryFlowStep = async ({ page, locale, capture }) => {
  const surgeries = clinicMessageCatalogs[locale].clinic.surgeries;
  const types = surgeries.types;
  const typeList = page.getByRole("region", { name: types.listTitle });
  // A required field's accessible name ends with its asterisk.
  const nameField = page.getByRole("textbox", { name: `${types.nameLabel}*`, exact: true });

  await test.step("surgery types", async () => {
    await open(page, Routes.Clinic.Settings.SurgeryTypes);
    await expect(typeList.getByRole("button", { name: surgeries.defaultType.name })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
    await capture("types");
  });

  await test.step("create a type", async () => {
    await page.getByRole("button", { name: types.newType }).click();
    await nameField.fill(typeName);
    for (const tool of tools) {
      await page.getByRole("textbox", { name: types.toolsLabel }).fill(tool);
      await page.getByRole("button", { name: types.addTool }).click();
    }
    for (const role of [surgeries.defaultType.roleSurgeon, surgeries.defaultType.roleAnesthetist]) {
      await page.getByRole("combobox", { name: types.rolesLabel }).fill(role);
      await page.getByRole("button", { name: types.addRole }).click();
    }
    // A new type has no reminders, so the added one comes before the cancellation message's fields.
    await page.getByRole("button", { name: types.addReminder }).click();
    await page.getByRole("textbox", { name: types.reminderMinutesLabel }).first().fill(reminderMinutes);
    await page.getByRole("textbox", { name: types.reminderSubjectLabel }).first().fill(reminderSubject);
    await page.getByRole("textbox", { name: types.reminderMessageLabel, exact: true }).first().fill(reminderMessage);
    await capture("new-type-filled");

    await page.getByRole("button", { name: types.save }).click();
    // The saved type stays open for editing; narrow screens show only the editor.
    await expect(page.getByRole("heading", { name: types.editTitle })).toBeVisible();
    await expect(nameField).toHaveValue(typeName);
    await capture("type-saved");
  });

  await test.step("edit the type", async () => {
    await nameField.fill(editedTypeName);
    await page.getByRole("button", { name: types.removeTool.replace("{name}", tools[0]) }).click();
    await capture("type-editing");

    await page.getByRole("button", { name: types.save }).click();
    await expect(nameField).toHaveValue(editedTypeName);
    await expect(page.getByRole("button", { name: types.removeTool.replace("{name}", tools[0]) })).toHaveCount(0);
    await capture("type-edited");
  });

  await test.step("delete the type", async () => {
    await page.getByRole("button", { name: types.delete }).click();
    const confirmation = page.getByRole("alertdialog");
    await expect(confirmation.getByText(types.deleteConfirmMessage)).toBeVisible();
    await capture("type-delete-confirmation");

    await confirmation.getByRole("button", { name: ownerMessages[locale].common.actions.delete }).click();
    await expect(confirmation).toBeHidden();
    await expect(typeList.getByRole("button", { name: editedTypeName })).toHaveCount(0);
    await capture("type-deleted");
  });

  await test.step("cancellation message", async () => {
    // Both cancellation fields are required, so their names end with an asterisk; the reminders' fields are optional.
    const subject = page.getByRole("textbox", { name: `${types.cancellation.subjectLabel}*`, exact: true });
    const message = page.getByRole("textbox", { name: `${types.cancellation.messageLabel}*`, exact: true });
    await subject.scrollIntoViewIfNeeded();
    await subject.fill(cancellationSubject);
    await message.fill(cancellationMessage);
    await page.getByRole("button", { name: types.cancellation.save }).click();
    await expect(page.getByText(types.cancellation.saveFailed)).toHaveCount(0);
    await expect(subject).toHaveValue(cancellationSubject);
    await expect(message).toHaveValue(cancellationMessage);
    await capture("cancellation-message");
  });
};

/** Where surgeries show up: the dashboard panel, the calendar with its views and filters, and the patient profile. */
export const surgeryOverviewSteps: SurgeryFlowStep = async ({ page, locale, capture }) => {
  const surgeries = clinicMessageCatalogs[locale].clinic.surgeries;
  const dashboard = ownerMessages[locale].home.dashboard;

  await test.step("dashboard surgeries panel", async () => {
    await open(page, Routes.Home);
    const allSurgeries = page.getByRole("button", { name: dashboard.allSurgeries });
    await expect(allSurgeries).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
    await allSurgeries.scrollIntoViewIfNeeded();
    await capture("dashboard");
  });

  await test.step("calendar", async () => {
    await open(page, Routes.Clinic.Surgeries.Index);
    await expect(page.getByRole("heading", { level: 1, name: surgeries.title })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
    await capture("calendar-month");

    const filters = page.getByRole("group", { name: surgeries.calendar.filters.title });
    await filters.getByRole("row", { name: surgeries.status.cancelled }).click();
    await expect(page.getByText(surgeries.calendar.empty).first()).toBeVisible();
    await capture("calendar-filtered");
    await filters.getByRole("row", { name: surgeries.calendar.filters.statusAll }).click();

    await page.getByText(surgeries.calendar.view.week, { exact: true }).click();
    await capture("calendar-week");
  });

  await test.step("patient surgeries", async () => {
    await open(page, Routes.Clinic.Patients.Detail(ids.patient));
    const plan = await revealSurgeries(page, surgeries.title, surgeries.actions.plan);
    await plan.scrollIntoViewIfNeeded();
    await capture("patient-surgeries");
  });
};

/** Planning from the calendar: find the patient, see validation, fill every section, and save. */
export const surgeryPlanSteps: SurgeryFlowStep = async ({ page, locale, capture }) => {
  const clinic = clinicMessageCatalogs[locale].clinic;
  const surgeries = clinic.surgeries;

  await test.step("find the patient", async () => {
    await open(page, Routes.Clinic.Surgeries.Index);
    await page.getByRole("button", { name: surgeries.actions.plan }).click({ timeout: pageCompileTimeoutMilliseconds });
    await expect(page.getByRole("heading", { level: 1, name: surgeries.plan.createTitle })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
    await page.getByRole("textbox", { name: clinic.patient.identification.label }).fill(ids.patientIdentificationCode);
    await capture("plan-find-patient");

    await page.getByRole("button", { name: clinic.patient.identification.lookupAction }).click();
    await expect(page.getByRole("button", { name: surgeries.plan.changePatient })).toBeVisible();
    await capture("plan-patient-found");
  });

  await test.step("validation", async () => {
    await page.getByRole("button", { name: surgeries.plan.submitCreate }).last().click();
    const summary = page.getByText(surgeries.plan.validationSummary);
    await expect(summary).toBeVisible();
    await summary.scrollIntoViewIfNeeded();
    await capture("plan-validation");
  });

  await test.step("fill the plan", async () => {
    await chooseDefaultType(page, surgeries);
    await pickToday(page, surgeries.fields.date);
    await typeTime(page.getByRole("group", { name: surgeries.fields.time, exact: true }), planHour, planMinute);
    await page.getByRole("textbox", { name: surgeries.fields.durationMinutes }).fill(planDurationMinutes);
    await fillLocation(page, surgeries.fields.location, planLocation);
    await expect(page.getByText(surgeries.plan.conflictTitle)).toBeVisible();
    await capture("plan-details");

    // The type adds a row for each of its roles; the first two get different clinic staff members.
    // An empty picker reads its placeholder; the add and remove buttons also name a team member.
    for (const staffMember of [0, 1]) {
      await page.getByRole("button", { name: surgeries.plan.memberPlaceholder }).first().click();
      await page.getByRole("option").nth(staffMember).click();
    }
    await page.getByRole("textbox", { name: surgeries.fields.tools }).fill(planTool);
    await page.getByRole("textbox", { name: surgeries.fields.tools }).press("Enter");
    await expect(page.getByRole("button", { name: surgeries.plan.removeTool.replace("{tool}", planTool) })).toBeVisible();
    await addReminder(page, surgeries);
    await page.getByRole("heading", { name: surgeries.plan.teamSectionTitle }).scrollIntoViewIfNeeded();
    await capture("plan-team-tools-reminders");
  });

  await test.step("save the plan", async () => {
    await page.getByRole("button", { name: surgeries.plan.submitCreate }).last().click();
    await expect(page.getByRole("heading", { level: 1, name: surgeries.defaultType.name })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
    await expect(page.getByText(planTool)).toBeVisible();
    await capture("surgery-detail");
    await page.getByRole("heading", { name: surgeries.detail.sections.reminders }).scrollIntoViewIfNeeded();
    await capture("surgery-detail-reminders");
  });
};

/** Editing a planned surgery: change its details, team, tools, and reminders, and save. */
export const surgeryEditSteps: SurgeryFlowStep = async ({ page, locale, capture }) => {
  const surgeries = clinicMessageCatalogs[locale].clinic.surgeries;

  await test.step("open the plan", async () => {
    await open(page, Routes.Clinic.Surgeries.Detail(ids.surgery));
    await page.getByRole("button", { name: surgeries.actions.edit }).first().click({ timeout: pageCompileTimeoutMilliseconds });
    await expect(page.getByRole("heading", { level: 1, name: surgeries.plan.editTitle })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
    await capture("edit-plan");
  });

  await test.step("change the plan", async () => {
    await page.getByRole("textbox", { name: surgeries.fields.name, exact: true }).fill(editedName);
    await page.getByRole("textbox", { name: surgeries.fields.durationMinutes }).fill(editedDurationMinutes);
    await fillLocation(page, surgeries.fields.location, editedLocation);
    await capture("edit-details");

    await page.getByRole("button", { name: surgeries.plan.addMember }).click();
    // An empty picker reads its placeholder; the add and remove buttons also name a team member.
    await page.getByRole("button", { name: surgeries.plan.memberPlaceholder }).last().click();
    await page.getByRole("option").first().click();
    await page.getByRole("combobox", { name: surgeries.fields.role }).last().fill(surgeries.defaultType.roleSurgeon);
    await page.getByRole("combobox", { name: surgeries.fields.role }).last().press("Escape");
    await page.getByRole("textbox", { name: surgeries.fields.tools }).fill(editedTool);
    await page.getByRole("textbox", { name: surgeries.fields.tools }).press("Enter");
    await addReminder(page, surgeries);
    await page.getByRole("heading", { name: surgeries.plan.teamSectionTitle }).scrollIntoViewIfNeeded();
    await capture("edit-team-tools-reminders");
  });

  await test.step("save the changes", async () => {
    await page.getByRole("button", { name: surgeries.plan.submitEdit }).last().click();
    await expect(page.getByRole("heading", { level: 1, name: editedName })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
    await expect(page.getByText(surgeries.durationValue.replace("{minutes}", editedDurationMinutes)).first()).toBeVisible();
    await expect(page.getByText(editedTool)).toBeVisible();
    await capture("surgery-edited");
    await page.getByRole("heading", { name: surgeries.detail.sections.reminders }).scrollIntoViewIfNeeded();
    await capture("surgery-edited-reminders");
  });
};

/** Status changes: complete a past surgery, set the mistaken completion back to planned, and cancel a planned one. */
export const surgeryStatusSteps: SurgeryFlowStep = async ({ page, locale, capture }) => {
  const surgeries = clinicMessageCatalogs[locale].clinic.surgeries;
  const dialogs = surgeries.detail.dialogs;
  const dialog = page.getByRole("alertdialog").or(page.getByRole("dialog"));
  const complete = page.getByRole("button", { name: surgeries.actions.complete });

  await test.step("complete a past surgery", async () => {
    await open(page, Routes.Clinic.Surgeries.Detail(ids.pastSurgery));
    await expect(complete).toBeEnabled({ timeout: pageCompileTimeoutMilliseconds });
    await capture("past-surgery");

    await complete.click();
    await expect(dialog.getByText(dialogs.completeMessage)).toBeVisible();
    await capture("complete-confirmation");

    await dialog.getByRole("button", { name: dialogs.completeConfirm }).click();
    await expect(dialog).toBeHidden();
    await expect(complete).toHaveCount(0);
    await expect(page.getByText(surgeries.status.completed).first()).toBeVisible();
    await capture("surgery-completed");
  });

  await test.step("correct a mistaken completion", async () => {
    await page.getByRole("button", { name: surgeries.actions.edit }).first().click();
    await expect(page.getByRole("heading", { level: 1, name: surgeries.plan.editTitle })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
    // A completed surgery cannot be cancelled; its plan offers the status, so a mistaken completion is set back to planned.
    await page.getByRole("button", { name: surgeries.fields.status }).click();
    await expect(page.getByRole("option", { name: surgeries.status.planned })).toBeVisible();
    await capture("edit-status-choice");

    await page.getByRole("option", { name: surgeries.status.planned }).click();
    await page.getByRole("button", { name: surgeries.plan.submitEdit }).last().click();
    await expect(complete).toBeEnabled({ timeout: pageCompileTimeoutMilliseconds });
    await expect(page.getByText(surgeries.status.planned).first()).toBeVisible();
    await capture("completion-corrected");
  });

  await test.step("cancel a planned surgery", async () => {
    await open(page, Routes.Clinic.Surgeries.Detail(ids.surgery));
    const cancel = page.getByRole("button", { name: surgeries.actions.cancel }).first();
    await cancel.click({ timeout: pageCompileTimeoutMilliseconds });
    await expect(dialog.getByText(dialogs.cancelMessage)).toBeVisible();
    await capture("cancel-confirmation");

    await dialog.getByRole("button", { name: dialogs.cancelConfirm }).click();
    await expect(dialog).toBeHidden();
    await expect(page.getByRole("button", { name: surgeries.actions.cancel })).toHaveCount(0);
    await expect(page.getByRole("button", { name: surgeries.actions.edit })).toHaveCount(0);
    await capture("surgery-cancelled");
  });
};

/** The operations assistant prepares a surgery cancellation, which waits on its review card for confirmation. */
export const surgeryAssistantSteps: SurgeryFlowStep = async ({ page, locale, capture }) => {
  const clinic = clinicMessageCatalogs[locale].clinic;
  const operations = clinic.operationsAssistant;

  await test.step("assistant surgery review", async () => {
    // Narrow screens offer no assistant toggle yet (#889), so the review card is captured on wide screens only.
    if (test.info().project.use.isMobile) {
      test.info().annotations.push({ type: "skipped-step", description: "The operations assistant has no toggle on narrow screens." });
      return;
    }
    // The operations assistant sits beside the clinic pages, such as the surgery calendar.
    await open(page, Routes.Clinic.Surgeries.Index);
    await page.getByRole("button", { name: ownerMessages[locale].appShell.operationsAssistant }).first().click({ timeout: pageCompileTimeoutMilliseconds });
    const assistant = page.getByRole("region", { name: operations.title });
    await assistant.getByRole("textbox", { name: operations.composer.label }).fill(assistantRequest);
    await assistant.getByRole("button", { name: operations.composer.send }).click();
    const notice = assistant.getByText(clinic.surgeries.assistant.notifiesPatientAndTeam);
    await expect(notice).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
    await expect(assistant.getByRole("button", { name: operations.confirm })).toBeVisible();
    await notice.scrollIntoViewIfNeeded();
    await capture("assistant-surgery-cancellation");
  });
};

type SurgeryMessages = (typeof clinicMessageCatalogs)[SupportedLocale]["clinic"]["surgeries"];

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

/** On narrow screens the patient's surgeries sit behind a tab; wide screens show them inline. */
async function revealSurgeries(page: Page, tabName: string, planName: string): Promise<Locator> {
  const plan = page.getByRole("button", { name: planName }).filter({ visible: true }).first();
  const surgeriesTab = page.getByRole("tab", { name: tabName }).filter({ visible: true });
  await expect(plan.or(surgeriesTab).first()).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
  if (!(await plan.isVisible())) await surgeriesTab.click();
  await expect(plan).toBeVisible();
  return plan;
}

/** The plan uses the type the app creates on every load, since a type made on another page is gone. */
async function chooseDefaultType(page: Page, surgeries: SurgeryMessages) {
  await page.getByRole("button", { name: surgeries.fields.type }).first().click();
  await page.getByRole("option", { name: surgeries.defaultType.name }).click();
  // Choosing the type names the surgery after it.
  await expect(page.getByRole("textbox", { name: surgeries.fields.name, exact: true })).toHaveValue(surgeries.defaultType.name);
}

/** The date picker opens on today; Enter picks it. */
async function pickToday(page: Page, dateLabel: string) {
  await page.getByRole("group", { name: dateLabel, exact: true }).getByRole("button").click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await page.keyboard.press("Enter");
  await expect(page.getByRole("dialog")).toBeHidden();
}

/** Types the time the way a keyboard user does; a 12-hour clock also needs its day period. */
async function typeTime(time: Locator, hour: string, minute: string) {
  const segments = time.getByRole("spinbutton");
  await segments.first().click();
  await segments.first().pressSequentially(hour);
  await segments.nth(1).pressSequentially(minute);
  if ((await segments.count()) > 2) await segments.nth(2).press("a");
}

/** Types a location and closes its suggestions so they do not cover the form. */
async function fillLocation(page: Page, label: string, location: string) {
  const field = page.getByRole("combobox", { name: label });
  await field.fill(location);
  await field.press("Escape");
}

/** Adds a reminder after the ones the type filled in. */
async function addReminder(page: Page, surgeries: SurgeryMessages) {
  await page.getByRole("button", { name: surgeries.plan.addReminder }).click();
  await page.getByRole("textbox", { name: surgeries.fields.minutesBefore }).last().fill(reminderMinutes);
  await page.getByRole("textbox", { name: surgeries.fields.subject }).last().fill(reminderSubject);
  await page.getByRole("textbox", { name: surgeries.fields.message, exact: true }).last().fill(reminderMessage);
}
