import { expect, test, type Locator, type Page } from "@playwright/test";
import { existsSync, mkdirSync } from "node:fs";
import { dirname } from "node:path";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { clinicMessageCatalogs } from "../../shared/clinic/src/localization/catalogs";
import { twoHostE2eProofScreenshotEnvironmentKey } from "../../scripts/two-host-e2e-artifacts.mjs";
import { forwardedForHeaderName, signInToClinic } from "./signInHost";

const ownerUsername = process.env.VITARISH_E2E_USERNAME ?? "e2e-owner";
const password = process.env.VITARISH_E2E_PASSWORD;
// Seeded by the E2E host (E2eFixtureSeeder) for tenant A.
const seededPatientId = "2d7a3eb9-1d47-4c63-9a45-19d7e6a0fb18";
const seededPatientCode = "E2E-0001";
const seededPatientName = "E2E Patient";
// The E2E browser resolves the English locale from its Accept-Language.
const clinicMessages = clinicMessageCatalogs.en.clinic;
const surgeryMessages = clinicMessages.surgeries;

async function login(page: Page) {
  expect(password).toBeTruthy();
  // Its own client IP keeps this spec out of the shared loopback login rate limit.
  await page.setExtraHTTPHeaders({ [forwardedForHeaderName]: "198.51.100.42" });
  await signInToClinic(page, ownerUsername, password!);
}

let step = 0;

/** Matches a field's accessible name, which carries a trailing "*" when the field is required. */
function fieldName(label: string) {
  return new RegExp(`^${label.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}\\*?$`);
}

/**
 * Captures the next numbered walkthrough screenshot next to the run's proof screenshot.
 * The app scrolls inside its own pane, so a full-page capture only shows the viewport;
 * `focus` scrolls the part the step is about into that viewport first.
 */
async function captureStep(page: Page, name: string, focus?: Locator) {
  step += 1;
  const suffix = `${String(step).padStart(2, "0")}-${name}`;
  const screenshot = process.env[twoHostE2eProofScreenshotEnvironmentKey]?.replace(/\.png$/i, `-${suffix}.png`);
  if (!screenshot) return;
  mkdirSync(dirname(screenshot), { recursive: true });
  expect(existsSync(screenshot), `refusing to overwrite retained evidence at ${screenshot}`).toBe(false);
  // Let tooltips and popovers from the last action settle out of the frame.
  await page.mouse.move(0, 0);
  if (focus) await focus.scrollIntoViewIfNeeded();
  // Finish finite transitions and suppress repeating animations during capture.
  await page.screenshot({ path: screenshot, fullPage: true, animations: "disabled" });
}

/** Types into React Aria date/time segments the way a keyboard user does. */
async function typeSegment(group: Locator, segment: RegExp, value: string) {
  const spinbutton = group.getByRole("spinbutton", { name: segment });
  await spinbutton.click();
  await spinbutton.pressSequentially(value);
}

async function fillSchedule(page: Page, scheduledAt: Date) {
  const date = page.getByRole("group", { name: fieldName(surgeryMessages.fields.date) });
  await typeSegment(date, /month/i, String(scheduledAt.getMonth() + 1).padStart(2, "0"));
  await typeSegment(date, /day/i, String(scheduledAt.getDate()).padStart(2, "0"));
  await typeSegment(date, /year/i, String(scheduledAt.getFullYear()));

  const time = page.getByRole("group", { name: fieldName(surgeryMessages.fields.time) });
  const dayPeriod = time.getByRole("spinbutton", { name: /AM\/PM/i });
  const hasDayPeriod = await dayPeriod.count() > 0;
  await typeSegment(time, /hour/i, String(scheduledAt.getHours()).padStart(2, "0"));
  await typeSegment(time, /minute/i, String(scheduledAt.getMinutes()).padStart(2, "0"));
  if (hasDayPeriod) {
    await dayPeriod.click();
    await dayPeriod.press(scheduledAt.getHours() < 12 ? "a" : "p");
  }
}

async function selectOption(page: Page, label: string, option: string | RegExp) {
  await page.getByRole("button", { name: new RegExp(label) }).first().click();
  await page.getByRole("option", { name: option }).first().click();
}

test("walks an owner through surgery types, planning, the calendar, editing, and cancelling", async ({ page }) => {
  // One test walks the whole feature, and next dev compiles each page on its first visit.
  test.setTimeout(600_000);
  const stamp = Date.now().toString().slice(-5);
  const typeName = `Rhinoplasty ${stamp}`;
  const location = `OR ${stamp}`;
  const scheduledAt = new Date();
  scheduledAt.setDate(scheduledAt.getDate() + 2);
  scheduledAt.setHours(10, 30, 0, 0);

  await login(page);

  // 1. Surgery types live in settings; a type pre-fills tools, team roles, and reminders on new plans.
  await page.goto(ClinicRoutes.Clinic.Settings.SurgeryTypes, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: surgeryMessages.types.title }).first()).toBeVisible({ timeout: 60_000 });
  await expect(page.getByRole("region", { name: surgeryMessages.types.listTitle })).toBeVisible({ timeout: 60_000 });
  await captureStep(page, "surgery-types");

  await page.getByRole("button", { name: surgeryMessages.types.newType }).click();
  await page.getByRole("textbox", { name: fieldName(surgeryMessages.types.nameLabel) }).fill(typeName);
  for (const tool of ["Nasal speculum", "Osteotome"]) {
    await page.getByRole("textbox", { name: surgeryMessages.types.toolsLabel }).fill(tool);
    await page.getByRole("button", { name: surgeryMessages.types.addTool }).click();
  }
  for (const role of [surgeryMessages.defaultType.roleSurgeon, surgeryMessages.defaultType.roleAnesthetist]) {
    await page.getByRole("combobox", { name: surgeryMessages.types.rolesLabel }).fill(role);
    await page.getByRole("button", { name: surgeryMessages.types.addRole }).click();
  }
  await expect(page.getByText("Osteotome")).toBeVisible();
  await captureStep(page, "new-type-filled");

  await page.getByRole("button", { name: surgeryMessages.types.save }).click();
  const typeList = page.getByRole("region", { name: surgeryMessages.types.listTitle });
  await expect(typeList.getByRole("button", { name: typeName }).or(typeList.getByLabel(typeName))).toBeVisible({ timeout: 60_000 });
  await captureStep(page, "type-saved");

  // 2. Plan a surgery from the calendar, finding the patient by identification code.
  await page.goto(ClinicRoutes.Clinic.Surgeries.Index, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { level: 1, name: surgeryMessages.title })).toBeVisible({ timeout: 60_000 });
  await page.getByRole("button", { name: surgeryMessages.actions.plan }).first().click();
  await expect(page).toHaveURL(new RegExp(`${ClinicRoutes.Clinic.Surgeries.New}$`), { timeout: 60_000 });
  await page.getByRole("textbox", { name: clinicMessages.patient.identification.label }).fill(seededPatientCode);
  await captureStep(page, "plan-find-patient");

  await page.getByRole("button", { name: clinicMessages.patient.identification.lookupAction }).click();
  await expect(page.getByText(seededPatientName).first()).toBeVisible({ timeout: 60_000 });
  await selectOption(page, surgeryMessages.fields.type, typeName);
  // Choosing the type fills in its tools and its team roles.
  await expect(page.getByRole("button", { name: surgeryMessages.plan.removeTool.replace("{tool}", "Osteotome") })).toBeVisible();
  await expect(page.getByRole("combobox", { name: surgeryMessages.fields.role })).toHaveCount(2);
  await captureStep(page, "plan-type-prefilled");

  await fillSchedule(page, scheduledAt);
  await page.getByRole("textbox", { name: surgeryMessages.fields.durationMinutes }).fill("120");
  await page.getByRole("combobox", { name: surgeryMessages.fields.location }).fill(location);
  await selectOption(page, surgeryMessages.fields.member, /.+/);
  await page.getByRole("textbox", { name: surgeryMessages.fields.tools }).fill("Headlight");
  await page.getByRole("textbox", { name: surgeryMessages.fields.tools }).press("Enter");
  await captureStep(page, "plan-filled", page.getByText("Headlight"));

  await page.getByRole("button", { name: surgeryMessages.plan.submitCreate }).last().click();
  await expect(page.getByRole("heading", { level: 1, name: typeName })).toBeVisible({ timeout: 60_000 });
  const surgeryUrl = page.url();
  await expect(page.getByText(location).first()).toBeVisible();
  await expect(page.getByText("Headlight")).toBeVisible();
  await captureStep(page, "surgery-detail", page.getByText("Headlight"));

  // 3. A second plan at the same time and place warns about the overlap before saving.
  await page.goto(ClinicRoutes.Clinic.Patients.Detail(seededPatientId), { waitUntil: "domcontentloaded" });
  await expect(page.getByText(typeName).first()).toBeVisible({ timeout: 60_000 });
  await captureStep(page, "patient-profile-surgeries", page.getByText(typeName).first());

  await page.getByRole("button", { name: surgeryMessages.actions.plan }).first().click();
  await expect(page.getByText(seededPatientName).first()).toBeVisible({ timeout: 60_000 });
  await selectOption(page, surgeryMessages.fields.type, typeName);
  await fillSchedule(page, scheduledAt);
  await page.getByRole("textbox", { name: surgeryMessages.fields.durationMinutes }).fill("60");
  await page.getByRole("combobox", { name: surgeryMessages.fields.location }).fill(location);
  await expect(page.getByText(surgeryMessages.plan.conflictTitle)).toBeVisible({ timeout: 60_000 });
  await expect(page.getByText(surgeryMessages.plan.conflictSameLocation).first()).toBeVisible();
  // Close the location suggestions so they do not cover the warning.
  await page.getByRole("combobox", { name: surgeryMessages.fields.location }).press("Escape");
  await captureStep(page, "plan-conflict-warning", page.getByText(surgeryMessages.plan.conflictTitle));

  // 4. The calendar shows the planned surgery in its period.
  await page.goto(ClinicRoutes.Clinic.Surgeries.Index, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { level: 1, name: surgeryMessages.title })).toBeVisible({ timeout: 60_000 });
  if (scheduledAt.getMonth() !== new Date().getMonth()) {
    await page.getByRole("button", { name: surgeryMessages.calendar.navigation.next }).click();
  }
  const calendarEntry = page.getByRole("button", { name: new RegExp(typeName) }).first();
  await expect(calendarEntry).toBeVisible({ timeout: 60_000 });
  await captureStep(page, "calendar-month");

  await page.getByRole("radio", { name: surgeryMessages.calendar.view.week }).or(page.getByRole("tab", { name: surgeryMessages.calendar.view.week })).or(page.getByRole("button", { name: surgeryMessages.calendar.view.week, exact: true })).first().click();
  // The month step above may have moved the calendar; the week count below starts from today.
  await page.getByRole("button", { name: surgeryMessages.calendar.navigation.today }).click();
  const today = new Date();
  const startOfWeek = new Date(today);
  startOfWeek.setDate(today.getDate() - today.getDay());
  // Count whole weeks from the start of the week's first day, so the result does not depend on the time the test runs.
  startOfWeek.setHours(0, 0, 0, 0);
  const weeksAhead = Math.floor((scheduledAt.getTime() - startOfWeek.getTime()) / (7 * 24 * 60 * 60 * 1000));
  for (let week = 0; week < weeksAhead && !(await calendarEntry.isVisible()); week += 1) {
    await page.getByRole("button", { name: surgeryMessages.calendar.navigation.next }).click();
  }
  await expect(calendarEntry).toBeVisible({ timeout: 60_000 });
  await captureStep(page, "calendar-week");

  // 5. Open the surgery from the calendar and edit its plan.
  await calendarEntry.click();
  await expect(page).toHaveURL(surgeryUrl, { timeout: 60_000 });
  await expect(page.getByRole("heading", { level: 1, name: typeName })).toBeVisible({ timeout: 60_000 });
  await page.getByRole("button", { name: surgeryMessages.actions.edit }).first().click();
  await expect(page.getByRole("heading", { level: 1, name: surgeryMessages.plan.editTitle })).toBeVisible({ timeout: 60_000 });
  await page.getByRole("textbox", { name: surgeryMessages.fields.durationMinutes }).fill("90");
  await captureStep(page, "edit-plan");

  await page.getByRole("button", { name: surgeryMessages.plan.submitEdit }).last().click();
  await expect(page).toHaveURL(surgeryUrl, { timeout: 60_000 });
  await expect(page.getByText(surgeryMessages.durationValue.replace("{minutes}", "90")).first()).toBeVisible({ timeout: 60_000 });
  await captureStep(page, "surgery-detail-edited");

  // 6. The dashboard lists the upcoming surgery.
  await page.goto(ClinicRoutes.Home, { waitUntil: "domcontentloaded" });
  const dashboardEntry = page.getByRole("button", { name: new RegExp(typeName) });
  await expect(dashboardEntry).toBeVisible({ timeout: 60_000 });
  await captureStep(page, "dashboard-upcoming", dashboardEntry);

  // 7. Cancelling asks for confirmation, then the surgery offers no further actions.
  await page.goto(surgeryUrl, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { level: 1, name: typeName })).toBeVisible({ timeout: 60_000 });
  await page.getByRole("button", { name: surgeryMessages.actions.cancel }).first().click();
  const dialog = page.getByRole("alertdialog").or(page.getByRole("dialog"));
  await expect(dialog).toBeVisible();
  await expect(dialog.getByText(surgeryMessages.detail.dialogs.cancelMessage)).toBeVisible();
  await captureStep(page, "cancel-confirmation");

  await dialog.getByRole("button", { name: surgeryMessages.detail.dialogs.cancelConfirm }).click();
  await expect(dialog).toBeHidden({ timeout: 60_000 });
  await expect(page.getByRole("button", { name: surgeryMessages.actions.cancel })).toHaveCount(0);
  await expect(page.getByText(surgeryMessages.status.cancelled).first()).toBeVisible();
  await captureStep(page, "surgery-cancelled");
});
