import { expect, test, type Page } from "@playwright/test";
import { existsSync, mkdirSync } from "node:fs";
import { dirname } from "node:path";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { clinicMessageCatalogs } from "../../shared/clinic/src/localization/catalogs";
import { twoHostE2eApiOriginEnvironmentKey, twoHostE2eProofScreenshotEnvironmentKey } from "../../scripts/two-host-e2e-artifacts.mjs";
import { agentToolCallbackPath, signAgentCallback } from "./agentCallback";

const ownerUsername = process.env.VITARISH_E2E_USERNAME ?? "e2e-owner";
const password = process.env.VITARISH_E2E_PASSWORD;
const apiOrigin = process.env[twoHostE2eApiOriginEnvironmentKey] ?? "https://localhost:7272";
const agentSharedSecret = process.env.VITARISH_E2E_AGENT_SHARED_SECRET;
// Seeded by the E2E host (E2eFixtureSeeder) for tenant A.
const tenantAId = "9c8b6b3a-7d6e-4f75-a88d-7d3f5b1b0f92";
const ownerUserId = "7b0f8d4d-3d6c-4d43-9f5e-9d5d3f9e6f4c";
const seededPatientId = "2d7a3eb9-1d47-4c63-9a45-19d7e6a0fb18";
const seededPatientName = "E2E Patient";
const clinicApi = "/api/modules/clinic";
// The E2E browser resolves the English locale from its Accept-Language.
const surgeryMessages = clinicMessageCatalogs.en.clinic.surgeries;

type JsonResult = { status: number; body: unknown };

async function login(page: Page) {
  expect(password).toBeTruthy();
  await page.setExtraHTTPHeaders({ "X-Forwarded-For": "198.51.100.41" });
  await page.goto("/login/password", { waitUntil: "domcontentloaded" });
  await expect(page.locator("form")).toHaveAttribute("method", "post", { timeout: 60_000 });
  await expect(page.locator('button[type="submit"]')).toBeEnabled({ timeout: 30_000 });
  await page.locator('input[name="username"]').fill(ownerUsername);
  await page.locator('input[name="password"]').fill(password!);
  const [response] = await Promise.all([
    page.waitForResponse((candidate) => candidate.url().endsWith("/api/modules/authentication/login/password")),
    page.locator('button[type="submit"]').click(),
  ]);
  expect(response.status()).toBe(200);
}

async function callApi(page: Page, method: string, path: string, body?: unknown): Promise<JsonResult> {
  return page.evaluate(async ({ method: requestMethod, path: requestPath, body: requestBody }) => {
    const headers: Record<string, string> = { "X-Vitarish-Tenant-Host": window.location.host };
    if (requestMethod !== "GET") {
      const antiforgery = await (await fetch("/api/modules/authentication/antiforgery", { credentials: "include", headers })).json() as { headerName: string; requestToken: string };
      headers[antiforgery.headerName] = antiforgery.requestToken;
      headers["Content-Type"] = "application/json";
      // The audit boundary requires a request id on every unsafe call, as the Web client sends.
      headers["X-Request-Id"] = `e2e-${crypto.randomUUID()}`;
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
  }, { method, path, body });
}

async function captureProof(page: Page, name: string) {
  const proofScreenshot = process.env[twoHostE2eProofScreenshotEnvironmentKey]?.replace(/\.png$/i, `-${name}.png`);
  if (!proofScreenshot) return;
  mkdirSync(dirname(proofScreenshot), { recursive: true });
  expect(existsSync(proofScreenshot), `refusing to overwrite retained evidence at ${proofScreenshot}`).toBe(false);
  await page.screenshot({ path: proofScreenshot, fullPage: true });
}

test("plans, lists, and cancels a surgery through the real Web and Main hosts", async ({ page, request }) => {
  const anonymous = await request.get(`${apiOrigin}${clinicApi}/surgeries/suggestions`, { ignoreHTTPSErrors: true });
  expect(anonymous.status()).toBe(401);

  await login(page);
  await page.goto(ClinicRoutes.Home, { waitUntil: "domcontentloaded" });

  const defaultType = await callApi(page, "POST", `${clinicApi}/surgeries/catalogs/default`, {
    name: surgeryMessages.defaultType.name,
    tools: [],
    defaultTeamRoles: [surgeryMessages.defaultType.roleSurgeon],
    reminderPolicies: [
      {
        name: surgeryMessages.defaultType.dayBeforeName,
        minutesBefore: 1440,
        subject: surgeryMessages.defaultType.dayBeforeSubject,
        textBody: surgeryMessages.defaultType.dayBeforeText,
      },
      {
        name: surgeryMessages.defaultType.hoursBeforeName,
        minutesBefore: 120,
        subject: surgeryMessages.defaultType.hoursBeforeSubject,
        textBody: surgeryMessages.defaultType.hoursBeforeText,
      },
    ],
  });
  expect(defaultType.status, JSON.stringify(defaultType.body)).toBe(200);
  const catalogId = (defaultType.body as { id: string }).id;
  const repeatedDefault = await callApi(page, "POST", `${clinicApi}/surgeries/catalogs/default`, {
    name: "ignored when the default exists",
    tools: [],
    defaultTeamRoles: [],
    reminderPolicies: [],
  });
  expect((repeatedDefault.body as { id: string }).id).toBe(catalogId);

  const surgeryName = `E2E rhinoplasty ${Date.now()}`;
  const scheduledAt = new Date(Date.now() + 3 * 24 * 60 * 60 * 1000);
  scheduledAt.setUTCMinutes(0, 0, 0);
  const createRequest = {
    catalogId,
    name: surgeryName,
    status: "Planned",
    scheduledAt: scheduledAt.toISOString(),
    tools: ["Scalpel"],
    personsToNotify: [],
    contacts: [],
    reminderPolicies: [],
    idempotencyKey: `e2e-${crypto.randomUUID()}`,
    durationMinutes: 90,
    location: "OR 2",
    team: [],
  };
  const created = await callApi(page, "POST", `${clinicApi}/patients/${seededPatientId}/surgeries`, createRequest);
  expect(created.status, JSON.stringify(created.body)).toBe(201);
  const surgery = created.body as { id: string; status: string; concurrencyToken: string; location: string };
  expect(surgery).toMatchObject({ status: "Planned", location: "OR 2" });
  const replayed = await callApi(page, "POST", `${clinicApi}/patients/${seededPatientId}/surgeries`, createRequest);
  expect((replayed.body as { id: string }).id).toBe(surgery.id);

  const from = new Date(scheduledAt.getTime() - 24 * 60 * 60 * 1000).toISOString();
  const to = new Date(scheduledAt.getTime() + 24 * 60 * 60 * 1000).toISOString();
  const calendar = await callApi(page, "GET", `${clinicApi}/surgeries/calendar?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`);
  expect(calendar.status).toBe(200);
  expect(calendar.body).toEqual(expect.arrayContaining([
    expect.objectContaining({ patientDisplayName: seededPatientName, surgery: expect.objectContaining({ id: surgery.id }) }),
  ]));

  // The operations assistant reads the same calendar through its signed tool callback; it names the patient but never returns a patient id.
  expect(agentSharedSecret).toBeTruthy();
  const callbackExpiry = new Date(Date.now() + 300_000);
  const envelope: Record<string, unknown> = {
    version: 1,
    executionId: `e2e-surgery-calendar-${crypto.randomUUID()}`,
    agentKey: "clinic.assistant.operations",
    tenantId: tenantAId,
    practiceId: tenantAId,
    subjectKind: "practice",
    subjectId: tenantAId,
    authenticatedUserId: ownerUserId,
    conversationId: "e2e-surgery-calendar-conversation",
    toolCallId: "e2e-surgery-calendar-tool-call",
    toolName: "list_operations_surgeries",
    credentialVersion: 1,
    expiresAt: callbackExpiry.toISOString(),
    correlationId: "e2e-surgery-calendar-correlation",
    role: "TenantOwner",
    locale: "en-US",
    timeZone: "Asia/Tehran",
    calendar: "gregory",
    currentUtc: new Date().toISOString(),
    currentLocal: new Date().toISOString(),
    permissions: [],
  };
  const credential = signAgentCallback(agentSharedSecret!, envelope);
  const assistantCalendar = await request.post(`${apiOrigin}${agentToolCallbackPath}`, {
    data: { executionEnvelope: envelope, argumentsJson: JSON.stringify({ from, to }) },
    headers: {
      "X-Vitarish-Tenant-Host": "localhost",
      "X-Vitarish-Agent-Credential": credential,
      "X-Request-Id": `e2e-${crypto.randomUUID()}`,
    },
    ignoreHTTPSErrors: true,
  });
  const assistantCalendarBody = await assistantCalendar.json() as { output: string; isError: boolean };
  expect(assistantCalendar.status(), JSON.stringify(assistantCalendarBody)).toBe(200);
  expect(assistantCalendarBody.isError).toBe(false);
  expect(JSON.parse(assistantCalendarBody.output)).toEqual({
    surgeries: expect.arrayContaining([expect.objectContaining({ id: surgery.id, patientDisplayName: seededPatientName, status: "Planned" })]),
  });
  expect(assistantCalendarBody.output).not.toContain(seededPatientId);

  const conflicts = await callApi(page, "GET", `${clinicApi}/surgeries/conflicts?scheduledAt=${encodeURIComponent(scheduledAt.toISOString())}&durationMinutes=30&location=${encodeURIComponent("  or 2 ")}`);
  expect(conflicts.status).toBe(200);
  expect(conflicts.body).toEqual(expect.arrayContaining([
    expect.objectContaining({ reasons: ["SameLocation"], entry: expect.objectContaining({ surgery: expect.objectContaining({ id: surgery.id }) }) }),
  ]));

  const earlyCompletion = await callApi(page, "POST", `${clinicApi}/surgeries/${surgery.id}/complete`, { concurrencyToken: surgery.concurrencyToken });
  expect(earlyCompletion.status).toBe(409);

  await page.goto(ClinicRoutes.Home, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("button", { name: new RegExp(surgeryName) })).toBeVisible({ timeout: 30_000 });
  await captureProof(page, "surgery-dashboard");

  await page.goto(ClinicRoutes.Clinic.Patients.Detail(seededPatientId), { waitUntil: "domcontentloaded" });
  await expect(page.getByText(surgeryName).first()).toBeVisible({ timeout: 30_000 });
  await captureProof(page, "surgery-patient");

  await page.goto(ClinicRoutes.Clinic.Surgeries.Index, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { level: 1, name: surgeryMessages.title })).toBeVisible({ timeout: 30_000 });
  await captureProof(page, "surgery-calendar");

  await page.goto(ClinicRoutes.Clinic.Surgeries.Detail(surgery.id), { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { level: 1, name: surgeryName })).toBeVisible({ timeout: 30_000 });
  await expect(page.getByText(seededPatientName).first()).toBeVisible();

  await captureProof(page, "surgery-detail");

  await page.getByRole("button", { name: surgeryMessages.actions.cancel }).first().click();
  const dialog = page.getByRole("alertdialog").or(page.getByRole("dialog"));
  await expect(dialog).toBeVisible();
  await dialog.getByRole("button", { name: surgeryMessages.detail.dialogs.cancelConfirm }).click();
  await expect(dialog).toBeHidden({ timeout: 30_000 });
  // A cancelled surgery offers no further cancellation.
  await expect(page.getByRole("button", { name: surgeryMessages.actions.cancel })).toHaveCount(0);

  const cancelled = await callApi(page, "GET", `${clinicApi}/surgeries/${surgery.id}`);
  expect(cancelled.status).toBe(200);
  expect((cancelled.body as { surgery: { status: string } }).surgery.status).toBe("Cancelled");

  // Cancelling again is idempotent: it returns the cancelled surgery instead of failing on the stale token.
  const repeatedCancel = await callApi(page, "POST", `${clinicApi}/surgeries/${surgery.id}/cancel`, { concurrencyToken: surgery.concurrencyToken });
  expect(repeatedCancel.status).toBe(200);
  expect((repeatedCancel.body as { status: string }).status).toBe("Cancelled");
});
