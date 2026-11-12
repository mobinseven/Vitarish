import { expect, test, type Page } from "@playwright/test";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { twoHostE2eTenantBClinicOrigin, twoHostE2eTenantBOwnerUsername, twoHostE2eTenantBOwnerUsernameEnvironmentKey } from "../../scripts/two-host-e2e-artifacts.mjs";
import { defaultClinicOrigin, forwardedForHeaderName, signInToClinic } from "./signInHost";

const ownerUsername = process.env.VITARISH_E2E_USERNAME ?? "e2e-owner";
const tenantBOwnerUsername = process.env[twoHostE2eTenantBOwnerUsernameEnvironmentKey] ?? twoHostE2eTenantBOwnerUsername;
const password = process.env.VITARISH_E2E_PASSWORD;
// Seeded by the E2E host (E2eFixtureSeeder) for tenant A.
const tenantAId = "9c8b6b3a-7d6e-4f75-a88d-7d3f5b1b0f92";
const clinicApi = "/api/modules/clinic";
const operationsAgentKey = "clinic.assistant.operations";
const practiceSubjectKind = "practice";
// The E2E host replaces the model with a scripted runner that executes these tool calls through the real guarded tools.
const scriptPrefix = "e2e-script:";
const prepareShiftTool = "prepare_create_shift";
const operationsCapability = "Operations";
const createShiftActionType = "create_shift";

type JsonResult = { status: number; body: unknown };
type PreparedAction = { id: string; capability: string; actionType: string; status: string; summary: string };
type ConversationBody = { agentRunId: string; conversationId: string; result: { actions: PreparedAction[] } };
type ActionDetails = { actionId: string; status: string; completionPayload?: Record<string, unknown> | null };

async function login(page: Page, username: string, origin: string, clientIp: string) {
  expect(password).toBeTruthy();
  await page.setExtraHTTPHeaders({ [forwardedForHeaderName]: clientIp });
  await signInToClinic(page, username, password!, origin);
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

/** Runs one scripted operations conversation that prepares a shift and returns the single proposed action. */
async function prepareShift(page: Page, totalSeats: number): Promise<{ action: PreparedAction; conversation: ConversationBody }> {
  const script = {
    calls: [{
      tool: prepareShiftTool,
      arguments: { visibility: "Public", dayOfWeek: "Tuesday", startTime: "06:15", endTime: "07:45", timeZone: "UTC", totalSeats },
    }],
    result: {
      messages: [{ kind: "assistantMessage", content: "The shift is ready for review." }],
      actions: [{ capability: operationsCapability, actionType: createShiftActionType, summary: `E2E shift for ${totalSeats} seats` }],
    },
  };
  const run = await callApi(page, "POST", "/api/modules/intelligence/agent-conversations", {
    agentKey: operationsAgentKey,
    subjectKind: practiceSubjectKind,
    subjectId: tenantAId,
    message: `${scriptPrefix}${JSON.stringify(script)}`,
  });
  expect(run.status, JSON.stringify(run.body)).toBe(200);
  const conversation = run.body as ConversationBody;
  expect(conversation.result.actions).toHaveLength(1);
  const [action] = conversation.result.actions;
  expect(action).toMatchObject({ capability: operationsCapability, actionType: createShiftActionType, status: "Proposed" });
  return { action, conversation };
}

const actionPath = (actionId: string, suffix = "") => `${clinicApi}/assistant/actions/${actionId}${suffix}`;

test("prepared assistant actions are confirmed, rejected, and tenant-scoped through the real hosts", async ({ page, browser }) => {
  const runId = crypto.randomUUID();
  // A seat count unique to this run identifies the shift this scenario created.
  const rejectedSeats = 2000 + (parseInt(runId.slice(0, 4), 16) % 3000);
  const confirmedSeats = rejectedSeats + 3000;
  let createdShiftId: string | undefined;
  const tenantBContext = await browser.newContext();
  const tenantBPage = await tenantBContext.newPage();

  try {
    await login(page, ownerUsername, defaultClinicOrigin, "198.51.100.51");
    await page.goto(ClinicRoutes.Home, { waitUntil: "domcontentloaded" });

    // 1-2. The scripted run prepares exactly one action and the owner reads it back as Proposed.
    const { action: rejectedAction } = await prepareShift(page, rejectedSeats);
    const proposed = await callApi(page, "GET", actionPath(rejectedAction.id));
    expect(proposed.status, JSON.stringify(proposed.body)).toBe(200);
    expect(proposed.body).toMatchObject({ actionId: rejectedAction.id, capability: operationsCapability, actionType: createShiftActionType, status: "Proposed" });

    // 3. Another tenant's authenticated owner can neither read nor confirm the action, and it stays Proposed.
    await login(tenantBPage, tenantBOwnerUsername, twoHostE2eTenantBClinicOrigin, "198.51.100.52");
    const foreignRead = await callApi(tenantBPage, "GET", actionPath(rejectedAction.id));
    const foreignConfirm = await callApi(tenantBPage, "POST", actionPath(rejectedAction.id, "/confirm"));
    expect(foreignRead.status).toBe(404);
    expect(foreignConfirm.status).toBe(404);
    expect(JSON.stringify([foreignRead.body, foreignConfirm.body])).not.toContain(createShiftActionType);
    const stillProposed = await callApi(page, "GET", actionPath(rejectedAction.id));
    expect(stillProposed.body).toMatchObject({ status: "Proposed" });

    // 4. Rejecting records the decision; a later confirm conflicts and executes nothing.
    const rejected = await callApi(page, "POST", actionPath(rejectedAction.id, "/reject"), { actionId: rejectedAction.id, reason: `E2E rejection ${runId}` });
    expect(rejected.status, JSON.stringify(rejected.body)).toBe(200);
    expect(rejected.body).toMatchObject({ actionId: rejectedAction.id, status: "Rejected" });
    const confirmAfterReject = await callApi(page, "POST", actionPath(rejectedAction.id, "/confirm"));
    expect(confirmAfterReject.status).toBe(409);
    expect((await callApi(page, "GET", actionPath(rejectedAction.id))).body).toMatchObject({ status: "Rejected" });

    // 5. A second run's action is confirmed once, replays idempotently, and its effect is visible through the public API.
    const { action: confirmedAction } = await prepareShift(page, confirmedSeats);
    const confirmed = await callApi(page, "POST", actionPath(confirmedAction.id, "/confirm"));
    expect(confirmed.status, JSON.stringify(confirmed.body)).toBe(200);
    expect(confirmed.body).toMatchObject({ actionId: confirmedAction.id, status: "Completed" });
    const replayed = await callApi(page, "POST", actionPath(confirmedAction.id, "/confirm"));
    expect(replayed.status, JSON.stringify(replayed.body)).toBe(200);
    expect(replayed.body).toMatchObject({ actionId: confirmedAction.id, status: "Completed" });

    const completed = (await callApi(page, "GET", actionPath(confirmedAction.id))).body as ActionDetails;
    expect(completed.status).toBe("Completed");
    const completion = completed.completionPayload ?? {};
    createdShiftId = (completion.entityId ?? completion.EntityId) as string | undefined;
    expect(createdShiftId, `the completion payload must name the created shift: ${JSON.stringify(completion)}`).toBeTruthy();

    const shift = await callApi(page, "GET", `/api/modules/appointments/shifts/${createdShiftId}`);
    expect(shift.status, JSON.stringify(shift.body)).toBe(200);
    expect(shift.body).toMatchObject({ id: createdShiftId, totalSeats: confirmedSeats });

    // The rejected action created no shift: no listed shift carries its seat count.
    const shifts = await callApi(page, "POST", "/api/modules/appointments/shifts", { descriptor: {} });
    expect(shifts.status).toBe(200);
    expect((shifts.body as { totalSeats: number }[]).filter((candidate) => candidate.totalSeats === rejectedSeats)).toEqual([]);
  } finally {
    // Cleanup of the one shift this scenario created; the prepared-action rows are retained operational records.
    if (createdShiftId) await callApi(page, "DELETE", `/api/modules/appointments/shifts/${createdShiftId}`).catch(() => undefined);
    await tenantBContext.close();
  }
});
