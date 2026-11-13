import { expect, test, type Locator, type Page } from "@playwright/test";
import { randomBytes, randomInt } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, statSync } from "node:fs";
import { dirname } from "node:path";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { LandingRoutes } from "../../apps/vitarish-landing/src/app/routes";
import { enMessages } from "../../apps/private-practice-owner/src/localization/messages/en";
import { faMessages } from "../../apps/private-practice-owner/src/localization/messages/fa";
import {
  twoHostE2eApiOriginEnvironmentKey,
  twoHostE2eLandingOrigin,
  twoHostE2eLocaleEnvironmentKey,
  twoHostE2eProofScreenshotEnvironmentKey,
  twoHostE2eSmsCapturePathEnvironmentKey,
} from "../../scripts/two-host-e2e-artifacts.mjs";
import { REQUEST_ID_HEADER, TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import { authenticationAntiforgeryGet, authenticationSessionGet } from "../../shared/auth/src/api/generated/authentication/authentication";
import { intelligenceRunAgentConversation } from "../../shared/intelligence/src/api/generated/intelligence/intelligence";
import { getClinicGetSetupProgressQueryKey } from "../../shared/clinic/src/api/generated/clinic/clinic";
import { clinicMessageCatalogs } from "../../shared/clinic/src/localization/catalogs";
import { tenantHandoffPath } from "../../shared/journey/src/onboardingHost";
import { intelligenceMessageCatalogs } from "../../shared/intelligence/src/localization/catalogs";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import type { SupportedLocale } from "../../shared/localization/src/locales";
import { landingIssueOnboardingHandoff } from "../../shared/platform/src/api/generated/landing/landing";
import { partiesCreateOwnTenant } from "../../shared/journey/src/api/generated/parties/parties";
import { agentToolCallbackPath, signAgentCallback } from "./agentCallback";
import type { LandingOnboardingHandoffRequest, LandingOnboardingHandoffResponse } from "../../shared/platform/src/api/generated/landing/model";
import { generatedOperationPath } from "./generatedOperationPath";
import { forwardedForHeaderName } from "./signInHost";

const locale: SupportedLocale = process.env[twoHostE2eLocaleEnvironmentKey] === "fa" ? "fa" : "en";
const ownerMessages = { en: enMessages, fa: faMessages }[locale];
const authMessages = ownerMessages.auth;
const journeyMessages = ownerMessages.journey;
const setupWizardMessages = journeyMessages.setup;
const clinicMessages = clinicMessageCatalogs[locale].clinic;
const setupMessages = clinicMessages.setup;
const operationsMessages = clinicMessages.operationsAssistant;
const manageMessages = operationsMessages.manage;
const assistantStatusMessages = clinicMessages.clinicAssistant.status;
const reviewGroupMessages = intelligenceMessageCatalogs[locale].intelligence.guidedSurface.actionGroup;

const smsCapturePath = process.env[twoHostE2eSmsCapturePathEnvironmentKey];
const landingOrigin = twoHostE2eLandingOrigin;
const onboardingOrigin = "http://start.localhost:3005";
const signInOrigin = "http://auth.localhost:3005";
const ownerClientIp = "198.51.100.93";
// A new clinic's pages compile on their first visit.
const pageCompileTimeoutMilliseconds = 120_000;
const onboardingHandoffPath = generatedOperationPath(landingIssueOnboardingHandoff);
const antiforgeryPath = generatedOperationPath(authenticationAntiforgeryGet);
const createOwnTenantPath = generatedOperationPath(partiesCreateOwnTenant);
const sessionPath = generatedOperationPath(authenticationSessionGet);
const agentConversationsPath = generatedOperationPath(intelligenceRunAgentConversation);
const apiOrigin = process.env[twoHostE2eApiOriginEnvironmentKey] ?? "https://localhost:7272";
const agentSharedSecret = process.env.VITARISH_E2E_AGENT_SHARED_SECRET;
const setupProgressPath = getClinicGetSetupProgressQueryKey()[0];
const onboardingHandoffRequest = { category: null, answers: [] } satisfies LandingOnboardingHandoffRequest;

// The E2E host replaces the model with a scripted runner that executes these tool calls through the real guarded tools.
const scriptPrefix = "e2e-script:";
const manageAppointmentsTool = "appointment_manage";
const createShiftOperation = "create_shift";
const inviteMemberTool = "prepare_invite_member";
const operationsCapability = "Operations";
const staffCapability = "Staff";
const manageActionType = "appointment_manage";
const inviteActionType = "invite_member";
const shiftsGoal = "ShiftsAndBookingRules";
const shiftsPath = "/api/modules/appointments/shifts";
const tenantMembersPath = "/api/modules/users/tenant-members";

type JsonResult = { status: number; body: unknown };

test.use({ actionTimeout: 60_000 });

let step = 0;

function escapeRegExp(text: string) {
  return text.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

/** Matches a field's accessible name, which carries a trailing "*" when the field is required. */
function fieldName(label: string) {
  return new RegExp(`^${escapeRegExp(label)}\\*?$`);
}

function randomDigits(length: number): string {
  return Array.from({ length }, () => randomInt(0, 10).toString()).join("");
}

/** Fills the `{name}` placeholders of a localized message. */
function fillMessage(template: string, values: Record<string, string | number>) {
  return Object.entries(values).reduce((text, [name, value]) => text.replace(`{${name}}`, String(value)), template);
}

/** Captures a numbered screenshot next to the run's proof screenshot, never over it or over another spec's file. */
async function captureStep(page: Page, name: string, focus?: Locator) {
  step += 1;
  const suffix = `clinic-setup-home-${String(step).padStart(2, "0")}-${name}`;
  const screenshot = process.env[twoHostE2eProofScreenshotEnvironmentKey]?.replace(/\.png$/i, `-${suffix}.png`);
  if (!screenshot) return;
  mkdirSync(dirname(screenshot), { recursive: true });
  expect(existsSync(screenshot), `refusing to overwrite retained evidence at ${screenshot}`).toBe(false);
  await page.mouse.move(0, 0);
  if (focus) await focus.scrollIntoViewIfNeeded();
  await page.screenshot({ path: screenshot, fullPage: true, animations: "disabled" });
}

async function readDeliveredSmsCode(previousModifiedAt: number, nationalDigits: string): Promise<string> {
  await expect.poll(() => Boolean(smsCapturePath && existsSync(smsCapturePath) && statSync(smsCapturePath).mtimeMs > previousModifiedAt)).toBe(true);
  const lines = readFileSync(smsCapturePath!, "utf8")
    .trim()
    .split(/\r?\n/)
    .filter(Boolean)
    .map((line) => JSON.parse(line) as { Recipient: string; Text: string });
  const code = [...lines].reverse().find((entry) => entry.Recipient.endsWith(nationalDigits))?.Text;
  expect(code).toEqual(expect.stringMatching(/^\d+$/));
  return code!;
}

async function requestOnboardingHandoff(page: Page): Promise<LandingOnboardingHandoffResponse> {
  const result = await page.evaluate(async ({ tenantHostHeader, requestIdHeader, tokenPath, handoffPath, requestId, requestBody }) => {
    const headers = { [tenantHostHeader]: window.location.host, [requestIdHeader]: requestId };
    const antiforgeryResponse = await fetch(tokenPath, { credentials: "include", headers });
    if (!antiforgeryResponse.ok) return { status: antiforgeryResponse.status, body: null };
    const antiforgery = (await antiforgeryResponse.json()) as { headerName: string; requestToken: string };
    const response = await fetch(handoffPath, {
      method: "POST",
      credentials: "include",
      headers: { ...headers, "Content-Type": "application/json", [antiforgery.headerName]: antiforgery.requestToken },
      body: JSON.stringify(requestBody),
    });
    const text = await response.text();
    return { status: response.status, body: text ? JSON.parse(text) : null };
  }, {
    tenantHostHeader: TENANT_HOST_HEADER,
    requestIdHeader: REQUEST_ID_HEADER,
    tokenPath: antiforgeryPath,
    handoffPath: onboardingHandoffPath,
    requestId: `e2e-setup-home-handoff-${crypto.randomUUID()}`,
    requestBody: onboardingHandoffRequest,
  });
  expect(result.status, "the signed-in landing host must issue the onboarding handoff").toBe(200);
  return result.body as LandingOnboardingHandoffResponse;
}

async function callApi(page: Page, method: string, path: string, body?: unknown): Promise<JsonResult> {
  return page.evaluate(async ({ method: requestMethod, path: requestPath, body: requestBody, tenantHostHeader, requestIdHeader }) => {
    const headers: Record<string, string> = { [tenantHostHeader]: window.location.host };
    if (requestMethod !== "GET") {
      const antiforgery = await (await fetch("/api/modules/authentication/antiforgery", { credentials: "include", headers })).json() as { headerName: string; requestToken: string };
      headers[antiforgery.headerName] = antiforgery.requestToken;
      headers["Content-Type"] = "application/json";
      headers[requestIdHeader] = `e2e-${crypto.randomUUID()}`;
    }
    const response = await fetch(requestPath, {
      method: requestMethod,
      credentials: "include",
      headers,
      body: requestBody === undefined ? undefined : JSON.stringify(requestBody),
    });
    let responseBody: unknown = null;
    try {
      responseBody = await response.json();
    } catch {
      // The status is the diagnostic for empty or non-JSON responses.
    }
    return { status: response.status, body: responseBody };
  }, { method, path, body, tenantHostHeader: TENANT_HOST_HEADER, requestIdHeader: REQUEST_ID_HEADER });
}

/** Signs a brand-new phone number up on the shared sign-in host, creates a clinic, and lands signed in on its /home. */
async function createClinicAndLandOnHome(page: Page, clinic: { mobileNumber: string; nationalDigits: string; subdomain: string; name: string; ownerFirstName: string; ownerLastName: string }): Promise<string> {
  expect(smsCapturePath, `${twoHostE2eSmsCapturePathEnvironmentKey} must be supplied by the two-host runner`).toBeTruthy();

  await page.goto(`${signInOrigin}${Routes.Login.Phone}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: authMessages.otp.phoneTitle, exact: true })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
  await page.getByRole("textbox", { name: fieldName(authMessages.otp.phoneLabel) }).fill(clinic.mobileNumber);
  const previousSmsModifiedAt = existsSync(smsCapturePath!) ? statSync(smsCapturePath!).mtimeMs : 0;
  const [beginResponse] = await Promise.all([
    page.waitForResponse((response) => response.url().endsWith("/api/modules/authentication/login/phone/begin")),
    page.getByRole("button", { name: authMessages.otp.sendAction, exact: true }).click(),
  ]);
  expect(beginResponse.status()).toBe(200);
  await page.getByLabel(fieldName(authMessages.otp.codeLabel)).fill(await readDeliveredSmsCode(previousSmsModifiedAt, clinic.nationalDigits));
  const [completeResponse] = await Promise.all([
    page.waitForResponse((response) => response.url().endsWith("/api/modules/authentication/login/phone/complete")),
    page.getByRole("button", { name: authMessages.otp.verifyAction, exact: true }).click(),
  ]);
  expect(completeResponse.status()).toBe(200);

  await expect(page).toHaveURL(new RegExp(`${Routes.Account.ProfileComplete}(?:[/?#]|$)`), { timeout: pageCompileTimeoutMilliseconds });
  await page.getByRole("textbox", { name: fieldName(journeyMessages.gates.profile.firstName) }).fill(clinic.ownerFirstName);
  await page.getByRole("textbox", { name: fieldName(journeyMessages.gates.profile.lastName) }).fill(clinic.ownerLastName);
  const saveProfileButton = page.getByRole("button", { name: journeyMessages.gates.profile.saveAndContinue, exact: true });
  await expect(saveProfileButton).toBeEnabled({ timeout: pageCompileTimeoutMilliseconds });
  await saveProfileButton.click();

  await page.waitForURL((url) => url.origin === landingOrigin && url.pathname === LandingRoutes.PlatformHome, {
    waitUntil: "domcontentloaded",
    timeout: pageCompileTimeoutMilliseconds,
  });
  const handoff = await requestOnboardingHandoff(page);
  expect(handoff.handoffUrl).toBeTruthy();
  await page.goto(handoff.handoffUrl!, { waitUntil: "domcontentloaded", timeout: pageCompileTimeoutMilliseconds });
  await page.waitForURL((url) => url.host === new URL(onboardingOrigin).host && !url.pathname.startsWith(tenantHandoffPath) && !url.pathname.startsWith(Routes.Login.Index), {
    timeout: pageCompileTimeoutMilliseconds,
  });
  await expect(page.getByRole("heading", { name: setupWizardMessages.title })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });

  await page.getByRole("textbox", { name: fieldName(setupWizardMessages.name) }).fill(clinic.name);
  await page.getByRole("textbox", { name: fieldName(setupWizardMessages.address) }).fill(clinic.subdomain);
  await expect(page.getByText(setupWizardMessages.availability.Available)).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
  await page.getByRole("button", { name: setupWizardMessages.next, exact: true }).click();
  await page.getByRole("button", { name: setupWizardMessages.specialty }).click();
  await page.getByRole("option", { name: clinicMessages.specialties.dermatology }).click();
  const [createResponse] = await Promise.all([
    page.waitForResponse((response) => response.request().method() === "POST" && new URL(response.url()).pathname === createOwnTenantPath),
    page.getByRole("button", { name: setupWizardMessages.create, exact: true }).click(),
  ]);
  const { tenantId } = await createResponse.json() as { tenantId: string };
  expect(tenantId).toBeTruthy();
  await expect(page.getByRole("heading", { name: setupWizardMessages.ready.title })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
  await page.getByRole("button", { name: setupWizardMessages.ready.goToClinic, exact: true }).click();
  await expect(page).toHaveURL(new RegExp(`^http://${escapeRegExp(clinic.subdomain)}\\.localhost:3005${escapeRegExp(Routes.Home)}(?:[/?#]|$)`), {
    timeout: pageCompileTimeoutMilliseconds,
  });
  return tenantId;
}

/** Sends one scripted model turn through the embedded composer. */
async function sendScript(assistant: Locator, script: { calls: { tool: string; arguments: unknown }[]; result: unknown }) {
  await assistant.getByRole("textbox", { name: operationsMessages.composer.label }).fill(`${scriptPrefix}${JSON.stringify(script)}`);
  await assistant.getByRole("button", { name: operationsMessages.composer.send, exact: true }).click();
}

test("a new clinic owner sets up the clinic with the assistant on the setup home, then chooses later", async ({ context, page }) => {
  test.setTimeout(900_000);

  const suffix = randomBytes(3).toString("hex");
  const nationalDigits = `912${randomDigits(7)}`;
  const subdomain = `setup-${suffix}`;
  const clinicOrigin = `http://${subdomain}.localhost:3005`;
  const inviteeNationalDigits = `935${randomDigits(7)}`;
  const inviteeFirstName = "Invitee";
  const inviteeLastName = `Staff${suffix}`;
  // Seat counts unique to this run identify the shifts this scenario creates; they differ in every digit group.
  const firstSeats = 2000 + (randomInt(0, 900));
  const secondSeats = firstSeats + 1000;
  const editedSeats = firstSeats + 2000;
  const welcomeNote = `Two shifts are ready ${suffix}.`;
  const questionPrompt = `Which step next ${suffix}?`;
  const questionOptions = [
    { key: "booking-rules", label: `Set booking rules ${suffix}` },
    { key: "invite-staff", label: `Invite staff ${suffix}` },
  ];

  await page.route(/^http:\/\/[^/]*localhost:3005\//, (route) =>
    route.continue({ headers: { ...route.request().headers(), [forwardedForHeaderName]: ownerClientIp } }),
  );
  await context.addCookies([
    { name: localeCookieName, value: locale, url: landingOrigin },
    { name: localeCookieName, value: locale, url: onboardingOrigin },
    { name: localeCookieName, value: locale, url: signInOrigin },
    { name: localeCookieName, value: locale, url: clinicOrigin },
  ]);

  // 1. A brand-new owner creates a clinic that has no services, providers or shifts, and lands on its /home.
  const tenantId = await createClinicAndLandOnHome(page, {
    mobileNumber: `0${nationalDigits}`,
    nationalDigits,
    subdomain,
    name: `Setup Home Clinic ${suffix}`,
    ownerFirstName: `SetupOwner ${suffix}`,
    ownerLastName: `Practice ${suffix}`,
  });

  // 2. The setup home shows its heading and the goal list; the side-panel toggle is not offered while it does.
  const setupHeading = page.getByRole("heading", { name: setupMessages.title, exact: true });
  await expect(setupHeading).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
  const goalList = page.getByRole("navigation", { name: intelligenceMessageCatalogs[locale].intelligence.guidedSurface.goals.title });
  for (const goalLabel of Object.values(setupMessages.goals)) await expect(goalList.getByText(goalLabel, { exact: true })).toBeVisible();
  const sideAssistantToggle = page.getByRole("button", { name: ownerMessages.appShell.operationsAssistant });
  await expect(sideAssistantToggle).toHaveCount(0);
  const assistant = page.getByRole("region", { name: operationsMessages.title });
  await expect(assistant).toBeVisible();
  await captureStep(page, "setup-home");

  // The setup tool answers through the signed runtime callback, the way the real model reaches it in production.
  expect(agentSharedSecret).toBeTruthy();
  const session = await callApi(page, "GET", sessionPath);
  const ownerUserId = (session.body as { user: { userId: string } }).user.userId;
  const callbackEnvelope: Record<string, unknown> = {
    version: 1,
    executionId: `e2e-setup-progress-${crypto.randomUUID()}`,
    agentKey: "clinic.assistant.operations",
    tenantId,
    practiceId: tenantId,
    subjectKind: "practice",
    subjectId: tenantId,
    authenticatedUserId: ownerUserId,
    conversationId: "e2e-setup-progress-conversation",
    toolCallId: "e2e-setup-progress-tool-call",
    toolName: "get_clinic_setup_progress",
    credentialVersion: 1,
    expiresAt: new Date(Date.now() + 300_000).toISOString(),
    correlationId: "e2e-setup-progress-correlation",
    role: "TenantOwner",
    locale: "en-US",
    timeZone: "UTC",
    calendar: "gregory",
    currentUtc: new Date().toISOString(),
    currentLocal: new Date().toISOString(),
    permissions: [],
  };
  const callback = await page.request.post(`${apiOrigin}${agentToolCallbackPath}`, {
    data: { executionEnvelope: callbackEnvelope, argumentsJson: "{}" },
    headers: {
      [TENANT_HOST_HEADER]: `${subdomain}.localhost`,
      "X-Vitarish-Agent-Credential": signAgentCallback(agentSharedSecret!, callbackEnvelope),
      [REQUEST_ID_HEADER]: `e2e-${crypto.randomUUID()}`,
    },
    ignoreHTTPSErrors: true,
  });
  const callbackBody = await callback.json() as { output: string; isError: boolean };
  expect(callback.status(), JSON.stringify(callbackBody)).toBe(200);
  expect(callbackBody.isError).toBe(false);
  const setupTool = JSON.parse(callbackBody.output) as { goals: { goal: string; done: boolean }[]; nextGoal: string | null };
  expect(setupTool.goals.map(({ goal }) => goal)).toEqual(Object.keys(setupMessages.goals));
  expect(setupTool.nextGoal).toBe(Object.keys(setupMessages.goals)[0]);

  // 3. One reply prepares two shifts: they appear as ONE review group with "Confirm all", a question and a next step.
  const shiftScript = (dayOfWeek: string, totalSeats: number) => ({
    tool: manageAppointmentsTool,
    arguments: {
      operation: createShiftOperation,
      payload: { visibility: "Public", dayOfWeek, startTime: "06:15", endTime: "07:45", timeZone: "UTC", totalSeats },
    },
  });
  const [firstRun] = await Promise.all([
    page.waitForResponse((response) => response.request().method() === "POST" && new URL(response.url()).pathname === agentConversationsPath),
    sendScript(assistant, {
    calls: [shiftScript("Tuesday", firstSeats), shiftScript("Wednesday", secondSeats)],
    result: {
      messages: [{ kind: "assistantMessage", content: welcomeNote }],
      actions: [
        { capability: operationsCapability, actionType: manageActionType, summary: `E2E shift ${firstSeats}` },
        { capability: operationsCapability, actionType: manageActionType, summary: `E2E shift ${secondSeats}` },
      ],
      question: { prompt: questionPrompt, options: questionOptions, maxSelections: 1 },
      nextAction: shiftsGoal,
    },
  }),
  ]);
  const homeConversationId = (await firstRun.json() as { conversationId: string }).conversationId;
  expect(homeConversationId).toBeTruthy();
  await expect(assistant.getByText(welcomeNote, { exact: true })).toBeVisible({ timeout: 90_000 });
  await expect(assistant.getByText(reviewGroupMessages.title, { exact: true })).toHaveCount(1);
  const firstItem = assistant.getByRole("listitem").filter({ hasText: String(firstSeats) });
  const secondItem = assistant.getByRole("listitem").filter({ hasText: String(secondSeats) });
  await expect(firstItem).toHaveCount(1);
  await expect(secondItem).toHaveCount(1);
  await expect(assistant.getByRole("button", { name: reviewGroupMessages.confirmAll, exact: true })).toHaveCount(1);
  await expect(assistant.getByText(questionPrompt).first()).toBeVisible();
  for (const option of questionOptions) await expect(assistant.getByText(option.label, { exact: true })).toBeVisible();
  const nextStepName = fillMessage(operationsMessages.nextStep, { goal: setupMessages.goals.ShiftsAndBookingRules });
  await expect(assistant.getByRole("button", { name: nextStepName, exact: true })).toBeVisible();
  await captureStep(page, "grouped-review", firstItem);

  // 4. Editing an item replaces its proposal with a new one inside the same group.
  await firstItem.getByRole("button", { name: manageMessages.edit, exact: true }).click();
  // While an item is edited its values sit in the form, so the only open form is addressed from the assistant.
  await assistant.getByLabel(manageMessages.fields.totalSeats, { exact: true }).fill(String(editedSeats));
  await assistant.getByRole("button", { name: manageMessages.save, exact: true }).click();
  await expect(assistant.getByText(String(editedSeats), { exact: true })).toBeVisible();
  await expect(assistant.getByText(String(firstSeats), { exact: true })).toHaveCount(0);
  await expect(assistant.getByText(reviewGroupMessages.title, { exact: true })).toHaveCount(1);
  await expect(assistant.getByRole("listitem").filter({ hasText: String(secondSeats) })).toHaveCount(1);
  await captureStep(page, "item-edited");

  // 5. "Confirm all" completes both items, and the shifts exist through the public appointments API.
  await assistant.getByRole("button", { name: reviewGroupMessages.confirmAll, exact: true }).click();
  const completedStatuses = assistant.getByRole("status").filter({ hasText: assistantStatusMessages.completed });
  await expect(completedStatuses).toHaveCount(2, { timeout: 90_000 });
  await expect(assistant.getByText(fillMessage(reviewGroupMessages.progress, { done: 2, total: 2 }), { exact: true })).toBeVisible();
  await expect(assistant.getByRole("button", { name: reviewGroupMessages.confirmAll, exact: true })).toHaveCount(0);
  await captureStep(page, "confirmed-all");
  const shifts = await callApi(page, "POST", shiftsPath, { descriptor: {} });
  expect(shifts.status, JSON.stringify(shifts.body)).toBe(200);
  const seatCounts = (shifts.body as { totalSeats: number }[]).map((shift) => shift.totalSeats);
  expect(seatCounts).toEqual(expect.arrayContaining([editedSeats, secondSeats]));
  expect(seatCounts, "the replaced proposal must not have created a shift").not.toContain(firstSeats);

  // 6. The assistant prepares a staff invitation; confirming it adds the member to Settings > Users.
  await sendScript(assistant, {
    calls: [{
      tool: inviteMemberTool,
      arguments: { phoneNumber: `0${inviteeNationalDigits}`, firstName: inviteeFirstName, lastName: inviteeLastName },
    }],
    result: {
      messages: [{ kind: "assistantMessage", content: `The invitation is ready ${suffix}.` }],
      actions: [{ capability: staffCapability, actionType: inviteActionType, summary: `Invite ${inviteeFirstName} ${inviteeLastName}` }],
      question: null,
      nextAction: null,
    },
  });
  const inviteCard = assistant.getByText(inviteeLastName, { exact: true });
  await expect(inviteCard).toBeVisible({ timeout: 90_000 });
  await expect(assistant.getByText(operationsMessages.invite.staff, { exact: true })).toBeVisible();
  await captureStep(page, "invite-prepared", inviteCard);
  await assistant.getByRole("button", { name: operationsMessages.confirm, exact: true }).click();
  await expect(assistant.getByRole("button", { name: operationsMessages.confirm, exact: true })).toHaveCount(0, { timeout: 90_000 });
  await expect(assistant.getByRole("status").filter({ hasText: assistantStatusMessages.completed }).last()).toBeVisible();
  await captureStep(page, "invite-confirmed");

  // The invited person is now a member of this clinic, read through the public Users members API.
  const members = await callApi(page, "GET", `${tenantMembersPath}?Search=${encodeURIComponent(inviteeLastName)}&MaxResults=50`);
  expect(members.status, JSON.stringify(members.body)).toBe(200);
  expect(JSON.stringify(members.body)).toContain(inviteeLastName);
  await page.goto(`${clinicOrigin}${Routes.Clinic.Settings.Users}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByText(inviteeLastName, { exact: false })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
  await captureStep(page, "users");

  // 7. "Later" hides the setup home for the clinic and brings today's dashboard back; reloading keeps it hidden.
  await page.goto(`${clinicOrigin}${Routes.Home}`, { waitUntil: "domcontentloaded" });
  await expect(setupHeading).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
  await page.getByRole("button", { name: setupMessages.later, exact: true }).click();
  await expect(setupHeading).toBeHidden();
  const dashboardPanels = [clinicMessages.appointments.visits.title, clinicMessages.surgeries.title];
  for (const panelTitle of dashboardPanels) await expect(page.getByRole("heading", { name: panelTitle, exact: true })).toBeVisible();
  await captureStep(page, "dashboard-after-later");

  const progressAfterLater = await callApi(page, "GET", setupProgressPath);
  expect(progressAfterLater.body).toMatchObject({ applies: false, dismissed: true });
  const [reloadedProgress] = await Promise.all([
    page.waitForResponse((response) => response.request().method() === "GET" && response.url().endsWith(setupProgressPath)),
    page.reload({ waitUntil: "domcontentloaded" }),
  ]);
  expect(await reloadedProgress.json()).toMatchObject({ applies: false });
  // Two frames let the page render what the answered request said before the absence is asserted.
  await page.evaluate(() => new Promise<void>((resolve) => requestAnimationFrame(() => requestAnimationFrame(() => resolve()))));
  for (const panelTitle of dashboardPanels) await expect(page.getByRole("heading", { name: panelTitle, exact: true })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
  await expect(setupHeading).toHaveCount(0);

  // 8. After "Later", the setup steps stay reachable from the assistant beside the clinic pages.
  await page.goto(`${clinicOrigin}${Routes.Clinic.Settings.Users}`, { waitUntil: "domcontentloaded" });
  await sideAssistantToggle.first().click({ timeout: pageCompileTimeoutMilliseconds });
  const sideAssistant = page.getByRole("region", { name: clinicMessages.operationsAssistant.title });
  await expect(sideAssistant.getByRole("navigation", { name: intelligenceMessageCatalogs[locale].intelligence.guidedSurface.goals.title })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
  await captureStep(page, "side-panel-setup-steps");

  // The side panel continues the conversation the setup home started.
  const [sideRun] = await Promise.all([
    page.waitForRequest((request) => request.method() === "POST" && new URL(request.url()).pathname === agentConversationsPath),
    sendScript(sideAssistant, { calls: [], result: { messages: [{ kind: "assistantMessage", content: `Continued ${suffix}` }], actions: [], question: null, nextAction: null } }),
  ]);
  expect((JSON.parse(sideRun.postData() ?? "{}") as { conversationId?: string }).conversationId).toBe(homeConversationId);
  await expect(sideAssistant.getByText(`Continued ${suffix}`)).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
});
