import { createCalendar, fromDate, getLocalTimeZone, toCalendar } from "@internationalized/date";
import { expect, test, type Locator, type Page } from "@playwright/test";
import crypto from "node:crypto";
import { existsSync, mkdirSync, readFileSync, statSync } from "node:fs";
import { dirname } from "node:path";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { enMessages } from "../../apps/private-practice-owner/src/localization/messages/en";
import { faMessages } from "../../apps/private-practice-owner/src/localization/messages/fa";
import {
  twoHostE2eLocaleEnvironmentKey,
  twoHostE2eProofScreenshotEnvironmentKey,
  twoHostE2eSignInOrigin,
  twoHostE2eSmsCapturePathEnvironmentKey,
} from "../../scripts/two-host-e2e-artifacts.mjs";
import { TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import { clinicMessageCatalogs } from "../../shared/clinic/src/localization/catalogs";
import { getDateCalendar } from "../../shared/localization/src/dateLocale";
import { formatTimeOfDay } from "../../shared/localization/src/format";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import type { SupportedLocale } from "../../shared/localization/src/locales";

const locale: SupportedLocale = process.env[twoHostE2eLocaleEnvironmentKey] === "fa" ? "fa" : "en";
const ownerMessageCatalogs = { en: enMessages, fa: faMessages } as const;
const ownerMessages = ownerMessageCatalogs[locale];
const authMessages = ownerMessages.auth;
const journeyMessages = ownerMessages.journey;
const setupMessages = journeyMessages.setup;
const clinicMessages = clinicMessageCatalogs[locale].clinic;
const surgeryMessages = clinicMessages.surgeries;

const smsCapturePath = process.env[twoHostE2eSmsCapturePathEnvironmentKey];
const onboardingOrigin = "http://start.localhost:3005";
const tenantBHost = "tenant-b.localhost:3005";
const tenantBOrigin = `http://${tenantBHost}`;
const takenSubdomain = "tenant-b";
const tenantBPatientId = "f4c8109a-6f22-4a31-9ab8-48d7ad0d7e2f";
const walkthroughClientIp = "198.51.100.88";
const handoffReplayClientIp = "198.51.100.89";
const publicLogoClientIp = "198.51.100.90";
// The public address the sign-in clinic list gives for a clinic's logo, on that clinic's own host.
const publicLogoPath = "/api/modules/parties/public/logo";
// A real 1x1 PNG, so the declared type, extension and leading signature all pass.
const logoPngBytes = [
  0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
  0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
  0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
  0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
  0x89, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x44, 0x41,
  0x54, 0x78, 0xDA, 0x63, 0x64, 0x60, 0xF8, 0x5F,
  0x0F, 0x00, 0x02, 0x87, 0x01, 0x80, 0xEB, 0x47,
  0xBA, 0x92, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45,
  0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82,
];
// Three hosts compile pages on their first visit during the walkthrough.
const pageCompileTimeoutMilliseconds = 120_000;

type CapturedSms = {
  Recipient: string;
  Text: string;
};

let step = 0;

// A missing control fails within a minute instead of waiting out the whole walkthrough.
test.use({ actionTimeout: 60_000 });

function escapeRegExp(text: string) {
  return text.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

/** Matches a field's accessible name, which carries a trailing "*" when the field is required. */
function fieldName(label: string) {
  return new RegExp(`^${escapeRegExp(label)}\\*?$`);
}

/** Matches the whole text, letting each space be any whitespace, such as the narrow space before "AM". */
function spacedText(text: string) {
  return new RegExp(`^${text.split(/\s+/).map(escapeRegExp).join("\\s+")}$`);
}

function randomDigits(length: number): string {
  return Array.from({ length }, () => crypto.randomInt(0, 10).toString()).join("");
}

function randomLowercaseAlphanumeric(length: number): string {
  const alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";
  return Array.from({ length }, () => alphabet[crypto.randomInt(0, alphabet.length)]).join("");
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
  await page.mouse.move(0, 0);
  if (focus) await focus.scrollIntoViewIfNeeded();
  await page.waitForFunction(() => document.getAnimations().every((animation) => animation.playState !== "running"));
  await page.screenshot({ path: screenshot, fullPage: true });
}

async function readDeliveredSmsCode(previousModifiedAt: number, nationalDigits: string): Promise<string> {
  await expect.poll(() => {
    return Boolean(smsCapturePath && existsSync(smsCapturePath) && statSync(smsCapturePath).mtimeMs > previousModifiedAt);
  }).toBe(true);

  const lines = readFileSync(smsCapturePath!, "utf8")
    .trim()
    .split(/\r?\n/)
    .filter(Boolean)
    .map((line) => JSON.parse(line) as CapturedSms);
  const code = [...lines].reverse().find((entry) => entry.Recipient.endsWith(nationalDigits))?.Text;
  expect(code).toEqual(expect.stringMatching(/^\d+$/));
  return code!;
}

/**
 * Types into React Aria date/time segments the way a keyboard user does. A segment's name starts
 * with its unit and ends with the field label; in Persian the time label is also the word for hour.
 */
async function typeSegment(group: Locator, segment: RegExp, value: string) {
  const spinbutton = group.getByRole("spinbutton", { name: segment });
  await spinbutton.click();
  await spinbutton.pressSequentially(value);
}

async function fillSchedule(page: Page, scheduledAt: Date) {
  const localizedDate = toCalendar(fromDate(scheduledAt, getLocalTimeZone()), createCalendar(getDateCalendar(locale)));
  const date = page.getByRole("group", { name: fieldName(surgeryMessages.fields.date) });
  await typeSegment(date, /^(?:month|ماه)/i, String(localizedDate.month).padStart(2, "0"));
  await typeSegment(date, /^(?:day|روز)/i, String(localizedDate.day).padStart(2, "0"));
  await typeSegment(date, /^(?:year|سال)/i, String(localizedDate.year));

  const time = page.getByRole("group", { name: fieldName(surgeryMessages.fields.time) });
  const dayPeriod = time.getByRole("spinbutton", { name: /AM\/PM|قبل‌ازظهر|بعدازظهر/i });
  const hasDayPeriod = (await dayPeriod.count()) > 0;
  await typeSegment(time, /^(?:hour|ساعت)/i, String(scheduledAt.getHours()).padStart(2, "0"));
  await typeSegment(time, /^(?:minute|دقیقه)/i, String(scheduledAt.getMinutes()).padStart(2, "0"));
  if (hasDayPeriod) {
    await dayPeriod.click();
    await dayPeriod.press(scheduledAt.getHours() < 12 ? "a" : "p");
  }
}

async function selectOption(page: Page, label: string, option: string | RegExp) {
  await page.getByRole("button", { name: new RegExp(label) }).first().click();
  await page.getByRole("option", { name: option }).first().click();
}

async function postPatientsCount(page: Page, tenantHost: string): Promise<number> {
  return page.evaluate(
    async ({ tenantHostHeader, targetTenantHost, requestId }) => {
      const antiforgeryResponse = await fetch("/api/modules/authentication/antiforgery", {
        credentials: "include",
        headers: { [tenantHostHeader]: window.location.host },
      });
      const antiforgery = (await antiforgeryResponse.json()) as { headerName: string; requestToken: string };
      const response = await fetch("/api/modules/clinic/patients/count", {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
          [tenantHostHeader]: targetTenantHost,
          "X-Request-Id": requestId,
          [antiforgery.headerName]: antiforgery.requestToken,
        },
        body: JSON.stringify({}),
      });
      return response.status;
    },
    // The id is made here: inside the page, `crypto` would name this file's Node import.
    { tenantHostHeader: TENANT_HOST_HEADER, targetTenantHost: tenantHost, requestId: `e2e-onboarding-${crypto.randomUUID()}` },
  );
}

test("a new phone number signs up, creates a clinic, lands signed in on it, and sets it up", async ({ browser, context, page }) => {
  test.setTimeout(1_200_000);
  expect(smsCapturePath, `${twoHostE2eSmsCapturePathEnvironmentKey} must be supplied by the two-host runner`).toBeTruthy();

  const suffix = randomLowercaseAlphanumeric(6);
  const nationalMobileDigits = `912${randomDigits(7)}`;
  const mobileNumber = `0${nationalMobileDigits}`;
  const subdomain = `walk-${suffix}`;
  const clinicHostname = `${subdomain}.localhost`;
  const clinicHost = `${clinicHostname}:3005`;
  const clinicOrigin = `http://${clinicHost}`;

  const ownerFirstName = `WalkOwner ${suffix}`;
  const ownerLastName = `Practice ${suffix}`;
  const clinicName = `Walkthrough Clinic ${suffix}`;
  const cityName = `Tehran ${suffix}`;
  const publicPhone = "021 1234 5678";
  const surgeryTypeName = `Rhinoplasty ${suffix}`;
  const surgeryTools = ["Nasal speculum", "Osteotome"];
  const surgeryLocation = `OR ${suffix}`;
  const patientFirstName = `WalkPatient ${suffix}`;
  const patientLastName = `Care ${suffix}`;
  const patientFullName = `${patientFirstName} ${patientLastName}`;
  const patientIdentificationCode = `WALK-${suffix.toUpperCase()}`;
  const shiftStartTime = "09:00";
  const shiftEndTime = "12:00";

  const scheduledAt = new Date();
  scheduledAt.setDate(scheduledAt.getDate() + 2);
  scheduledAt.setHours(10, 30, 0, 0);

  // Only the app's own requests carry the client IP: on a cross-origin request (the map tiles) a
  // custom header would force a CORS preflight the tile server refuses.
  await page.route(/^http:\/\/[^/]*localhost:3005\//, (route) =>
    route.continue({ headers: { ...route.request().headers(), "X-Forwarded-For": walkthroughClientIp } }),
  );
  await context.addCookies([
    { name: localeCookieName, value: locale, url: onboardingOrigin },
    { name: localeCookieName, value: locale, url: clinicOrigin },
    { name: localeCookieName, value: locale, url: tenantBOrigin },
  ]);

  // 1. Sign up on the shared sign-in host by phone OTP, complete the profile gate, and continue,
  // with no clinic yet, to clinic setup on the onboarding host.
  await page.goto(`${twoHostE2eSignInOrigin}${Routes.Login.Phone}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: authMessages.otp.phoneTitle, exact: true })).toBeVisible({
    timeout: pageCompileTimeoutMilliseconds,
  });

  await page.getByRole("textbox", { name: fieldName(authMessages.otp.phoneLabel) }).fill(mobileNumber);
  const previousSmsModifiedAt = existsSync(smsCapturePath!) ? statSync(smsCapturePath!).mtimeMs : 0;
  const [beginResponse] = await Promise.all([
    page.waitForResponse((response) => response.url().endsWith("/api/modules/authentication/login/phone/begin")),
    page.getByRole("button", { name: authMessages.otp.sendAction, exact: true }).click(),
  ]);
  expect(beginResponse.status()).toBe(200);

  const otpCode = await readDeliveredSmsCode(previousSmsModifiedAt, nationalMobileDigits);
  await page.getByLabel(fieldName(authMessages.otp.codeLabel)).fill(otpCode);
  const [completeResponse] = await Promise.all([
    page.waitForResponse((response) => response.url().endsWith("/api/modules/authentication/login/phone/complete")),
    page.getByRole("button", { name: authMessages.otp.verifyAction, exact: true }).click(),
  ]);
  expect(completeResponse.status()).toBe(200);

  await expect(page).toHaveURL(new RegExp(`${Routes.Account.ProfileComplete}(?:[/?#]|$)`), {
    timeout: pageCompileTimeoutMilliseconds,
  });
  await expect(page.getByRole("heading", { name: journeyMessages.gates.profile.title })).toBeVisible({
    timeout: pageCompileTimeoutMilliseconds,
  });

  await page.getByRole("textbox", { name: fieldName(journeyMessages.gates.profile.firstName) }).fill(ownerFirstName);
  await page.getByRole("textbox", { name: fieldName(journeyMessages.gates.profile.lastName) }).fill(ownerLastName);
  const saveProfileButton = page.getByRole("button", { name: journeyMessages.gates.profile.saveAndContinue, exact: true });
  await expect(saveProfileButton).toBeEnabled({ timeout: pageCompileTimeoutMilliseconds });
  await saveProfileButton.click();

  await expect(page).toHaveURL(new RegExp(`^${onboardingOrigin}${Routes.Setup}(?:[/?#]|$)`), {
    timeout: pageCompileTimeoutMilliseconds,
  });
  await expect(page.getByRole("heading", { name: setupMessages.title })).toBeVisible({
    timeout: pageCompileTimeoutMilliseconds,
  });
  await captureStep(page, "setup");

  // 2. Address collision refusal for an existing clinic subdomain.
  const clinicNameField = page.getByRole("textbox", { name: fieldName(setupMessages.name) });
  const clinicAddressField = page.getByRole("textbox", { name: fieldName(setupMessages.address) });
  await clinicAddressField.fill(takenSubdomain);
  await expect(page.getByText(setupMessages.availability.Taken)).toBeVisible({
    timeout: pageCompileTimeoutMilliseconds,
  });
  await captureStep(page, "address-taken");

  // 3. Fill the clinic name, unique subdomain, specialty, and business details, then create the clinic.
  await clinicNameField.fill(clinicName);
  await clinicAddressField.fill(subdomain);
  await expect(page.getByText(setupMessages.availability.Available)).toBeVisible({
    timeout: pageCompileTimeoutMilliseconds,
  });
  await page.getByRole("button", { name: setupMessages.next, exact: true }).click();

  await page.getByRole("button", { name: setupMessages.specialty }).click();
  await page.getByRole("option", { name: clinicMessages.specialties.dermatology }).click();
  await page.getByRole("button", { name: setupMessages.details.title }).click();
  await page.getByRole("textbox", { name: setupMessages.details.city, exact: true }).fill(cityName);
  await page.getByRole("textbox", { name: setupMessages.details.phone, exact: true }).first().fill(publicPhone);
  await page.getByRole("button", { name: setupMessages.create, exact: true }).click();

  await expect(page.getByRole("heading", { name: setupMessages.ready.title })).toBeVisible({
    timeout: pageCompileTimeoutMilliseconds,
  });
  await expect(page.getByText(clinicHostname, { exact: true })).toBeVisible();
  await captureStep(page, "clinic-ready");

  // 4. Handoff to the new clinic host and land signed in on its dashboard with the logo prompt visible.
  const handoffUrlPromise = page
    .waitForURL(new RegExp(`${Routes.Handoff}#.+`), {
      waitUntil: "commit",
      timeout: pageCompileTimeoutMilliseconds,
    })
    .then(() => page.url());
  await page.getByRole("button", { name: setupMessages.ready.goToClinic, exact: true }).click();
  const recordedHandoffUrl = await handoffUrlPromise;
  expect(recordedHandoffUrl).toMatch(new RegExp(`^http://${clinicHostname}:3005${Routes.Handoff}#.+$`));

  await expect(page).toHaveURL(new RegExp(`^http://${clinicHostname}:3005${Routes.Home}(?:[/?#]|$)`), {
    timeout: pageCompileTimeoutMilliseconds,
  });
  const logoPromptHeading = page.getByRole("heading", { name: journeyMessages.logoPrompt.title });
  await expect(logoPromptHeading).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
  await captureStep(page, "clinic-home-signed-in", logoPromptHeading);

  // 4b. The clinic's logo is public: once the owner uploads it, anyone loads it from the clinic's own host,
  // including an image on another host's page that sends no referrer, as the sign-in clinic list does.
  const publicLogoUrl = `${clinicOrigin}${publicLogoPath}`;
  const anonymousContext = await browser.newContext({
    extraHTTPHeaders: { "X-Forwarded-For": publicLogoClientIp },
  });
  try {
    // Chromium resolves *.localhost hosts; Node's request client does not, so a signed-out page reads the logo.
    const anonymousPage = await anonymousContext.newPage();
    // A public page on the clinic host; its sign-in pages redirect to the sign-in host.
    await anonymousPage.goto(`${clinicOrigin}${Routes.Handoff}`, { waitUntil: "domcontentloaded" });
    const readPublicLogo = () =>
      anonymousPage.evaluate(async (path) => {
        const response = await fetch(path, { credentials: "omit" });
        return {
          status: response.status,
          contentType: response.headers.get("content-type"),
          bytes: response.ok ? [...new Uint8Array(await response.arrayBuffer())] : [],
        };
      }, publicLogoPath);
    expect((await readPublicLogo()).status).toBe(404);

    await page.locator('input[type="file"]').setInputFiles({
      name: "logo.png",
      mimeType: "image/png",
      buffer: Buffer.from(logoPngBytes),
    });
    await expect(logoPromptHeading).toBeHidden({ timeout: pageCompileTimeoutMilliseconds });

    expect(await readPublicLogo()).toEqual({ status: 200, contentType: "image/png", bytes: logoPngBytes });

    await anonymousPage.goto(`${twoHostE2eSignInOrigin}${Routes.Login.Phone}`, { waitUntil: "domcontentloaded" });
    const shownWidth = await anonymousPage.evaluate(
      (src) =>
        new Promise<number>((resolve) => {
          const image = new Image();
          image.referrerPolicy = "no-referrer";
          image.onload = () => resolve(image.naturalWidth);
          image.onerror = () => resolve(0);
          image.src = src;
        }),
      publicLogoUrl,
    );
    expect(shownWidth).toBe(1);
  } finally {
    await anonymousContext.close();
  }

  // 5. Replaying the one-time handoff URL in a fresh browser context with no session cookies is refused.
  const replayContext = await browser.newContext({
    extraHTTPHeaders: { "X-Forwarded-For": handoffReplayClientIp },
  });
  try {
    await replayContext.addCookies([{ name: localeCookieName, value: locale, url: clinicOrigin }]);
    const replayPage = await replayContext.newPage();
    await replayPage.goto(recordedHandoffUrl, { waitUntil: "domcontentloaded" });
    await expect(replayPage.getByRole("heading", { name: journeyMessages.handoff.failedTitle })).toBeVisible({
      timeout: pageCompileTimeoutMilliseconds,
    });
    await captureStep(replayPage, "handoff-replay-refused");
  } finally {
    await replayContext.close();
  }

  // 6. Cross-clinic API and browser navigation refusal for tenant B.
  const ownClinicCountStatus = await postPatientsCount(page, clinicHost);
  expect(ownClinicCountStatus).toBe(200);
  // Tenant B's seeded patient (E2eFixtureSeeder) is not visible from the new clinic.
  const crossClinicPatientStatus = await page.evaluate(
    async ({ tenantHostHeader, patientId }) =>
      (
        await fetch(`/api/modules/clinic/patients/${patientId}`, {
          credentials: "include",
          headers: { [tenantHostHeader]: window.location.host },
        })
      ).status,
    { tenantHostHeader: TENANT_HOST_HEADER, patientId: tenantBPatientId },
  );
  expect([403, 404]).toContain(crossClinicPatientStatus);

  // The session cookie belongs to the new clinic's host only, so tenant B sees nobody signed in.
  await page.goto(`${tenantBOrigin}${Routes.Home}`, { waitUntil: "domcontentloaded" });
  const tenantBSessionStatus = await page.evaluate(
    async (tenantHostHeader) =>
      (
        await fetch("/api/modules/authentication/session", {
          credentials: "include",
          headers: { [tenantHostHeader]: window.location.host },
        })
      ).status,
    TENANT_HOST_HEADER,
  );
  expect(tenantBSessionStatus).toBe(401);
  expect(await postPatientsCount(page, tenantBHost)).toBe(401);
  await captureStep(page, "tenant-b-sign-in-required");

  // 7. Set up the new clinic as its owner: surgery type, patient, planned surgery, and weekly shift.
  await page.goto(`${clinicOrigin}${Routes.Clinic.Settings.SurgeryTypes}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: surgeryMessages.types.title }).first()).toBeVisible({
    timeout: pageCompileTimeoutMilliseconds,
  });
  await expect(page.getByRole("region", { name: surgeryMessages.types.listTitle })).toBeVisible({
    timeout: pageCompileTimeoutMilliseconds,
  });

  await page.getByRole("button", { name: surgeryMessages.types.newType }).click();
  const typeNameField = page.getByRole("textbox", { name: fieldName(surgeryMessages.types.nameLabel) });
  await typeNameField.fill(surgeryTypeName);
  for (const tool of surgeryTools) {
    await page.getByRole("textbox", { name: surgeryMessages.types.toolsLabel }).fill(tool);
    await page.getByRole("button", { name: surgeryMessages.types.addTool }).click();
  }
  for (const role of [surgeryMessages.defaultType.roleSurgeon, surgeryMessages.defaultType.roleAnesthetist]) {
    await page.getByRole("combobox", { name: surgeryMessages.types.rolesLabel }).fill(role);
    await page.getByRole("button", { name: surgeryMessages.types.addRole }).click();
  }
  await page.getByRole("button", { name: surgeryMessages.types.save }).click();
  await expect(page.getByRole("heading", { name: surgeryMessages.types.editTitle })).toBeVisible({
    timeout: pageCompileTimeoutMilliseconds,
  });
  await expect(typeNameField).toHaveValue(surgeryTypeName);
  const typeList = page.getByRole("region", { name: surgeryMessages.types.listTitle });
  if (await typeList.isVisible()) {
    await expect(
      typeList.getByRole("button", { name: surgeryTypeName }).or(typeList.getByLabel(surgeryTypeName)),
    ).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
  }
  await captureStep(page, "surgery-type-saved");

  await page.goto(`${clinicOrigin}${Routes.Clinic.Patients.New}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { level: 1, name: clinicMessages.patients.newPatient })).toBeVisible({
    timeout: pageCompileTimeoutMilliseconds,
  });
  await page.getByRole("textbox", { name: fieldName(clinicMessages.patient.fields.firstName) }).fill(patientFirstName);
  await page.getByRole("textbox", { name: fieldName(clinicMessages.patient.fields.lastName) }).fill(patientLastName);
  await page
    .getByRole("textbox", { name: fieldName(clinicMessages.patient.fields.identificationCode) })
    .fill(patientIdentificationCode);
  await page.getByRole("button", { name: clinicMessages.patient.edit.save }).click();

  await expect(page.getByRole("heading", { level: 1, name: patientFullName })).toBeVisible({
    timeout: pageCompileTimeoutMilliseconds,
  });
  await expect(page.getByText(new RegExp(patientIdentificationCode)).first()).toBeVisible();
  await captureStep(page, "patient-created");

  await page.goto(`${clinicOrigin}${Routes.Clinic.Surgeries.New}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { level: 1, name: surgeryMessages.plan.createTitle })).toBeVisible({
    timeout: pageCompileTimeoutMilliseconds,
  });
  await page.getByRole("textbox", { name: clinicMessages.patient.identification.label }).fill(patientIdentificationCode);
  await page.getByRole("button", { name: clinicMessages.patient.identification.lookupAction }).click();
  await expect(page.getByText(patientFullName).first()).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });

  await selectOption(page, surgeryMessages.fields.type, surgeryTypeName);
  await expect(
    page.getByRole("button", { name: surgeryMessages.plan.removeTool.replace("{tool}", surgeryTools[1]) }),
  ).toBeVisible();
  await expect(page.getByRole("combobox", { name: surgeryMessages.fields.role })).toHaveCount(2);

  await fillSchedule(page, scheduledAt);
  await page.getByRole("textbox", { name: surgeryMessages.fields.durationMinutes }).fill("120");
  await page.getByRole("combobox", { name: surgeryMessages.fields.location }).fill(surgeryLocation);
  await page.getByRole("combobox", { name: surgeryMessages.fields.location }).press("Escape");
  await selectOption(page, surgeryMessages.fields.member, /.+/);
  await page.getByRole("button", { name: surgeryMessages.plan.submitCreate }).last().click();

  await expect(page.getByRole("heading", { level: 1, name: surgeryTypeName })).toBeVisible({
    timeout: pageCompileTimeoutMilliseconds,
  });
  await expect(page.getByText(patientFullName).first()).toBeVisible();
  await expect(page.getByText(surgeryLocation).first()).toBeVisible();
  await captureStep(page, "surgery-planned");

  await page.goto(`${clinicOrigin}${Routes.Clinic.Appointments.Shifts.New}`, { waitUntil: "domcontentloaded" });
  await expect(
    page.getByRole("heading", { level: 1, name: clinicMessages.appointments.shifts.createTitle }),
  ).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
  await page.getByLabel(clinicMessages.fields.dayOfWeek).selectOption({ label: clinicMessages.weekdays.saturday });
  await page.getByLabel(clinicMessages.fields.startTime).fill(shiftStartTime);
  await page.getByLabel(clinicMessages.fields.endTime).fill(shiftEndTime);
  await page.getByRole("button", { name: ownerMessages.common.actions.save, exact: true }).click();

  await expect(
    page.getByRole("heading", {
      level: 1,
      // Shift times are written in the locale's digits and clock, without seconds.
      name: spacedText(`${clinicMessages.weekdays.saturday} ${formatTimeOfDay(shiftStartTime, locale)} - ${formatTimeOfDay(shiftEndTime, locale)}`),
    }),
  ).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
  await captureStep(page, "shift-created");
});
