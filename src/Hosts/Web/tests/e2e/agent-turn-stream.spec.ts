import { expect, test, type Page } from "@playwright/test";
import { randomUUID } from "node:crypto";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { REQUEST_ID_HEADER, TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import { authenticationAntiforgeryGet } from "../../shared/auth/src/api/generated/authentication/authentication";
import { clinicMessageCatalogs } from "../../shared/clinic/src/localization/catalogs";
import { StreamChunkKindValue } from "../../shared/intelligence/src/api/intelligence/agentTurnFrames";
import {
  intelligenceRunAgentConversation,
  intelligenceStreamAgentConversation,
} from "../../shared/intelligence/src/api/generated/intelligence/intelligence";
import { generatedOperationPath } from "./generatedOperationPath";
import { defaultClinicOrigin, forwardedForHeaderName, signInToClinic } from "./signInHost";

const ownerUsername = process.env.VITARISH_E2E_USERNAME ?? "e2e-owner";
const password = process.env.VITARISH_E2E_PASSWORD;
const ownerClientIp = "198.51.100.62";
const antiforgeryPath = generatedOperationPath(authenticationAntiforgeryGet);
const runPath = generatedOperationPath(intelligenceRunAgentConversation);
const streamPath = generatedOperationPath(intelligenceStreamAgentConversation);
const operationsMessages = clinicMessageCatalogs.en.clinic.operationsAssistant;
const workspaceMessages = clinicMessageCatalogs.en.clinic.assistantWorkspace;

// The E2E host replaces the model provider with a scripted runner (see E2eScriptedAgentRunner).
const scriptPrefix = "e2e-script:";
const operationsAgentKey = "clinic.assistant.operations";
const helpAgentKey = "clinic.assistant.operations.help";
const practiceSubjectKind = "practice";
const practiceSubjectId = "practice";
const prepareShiftTool = "prepare_create_shift";
const assistantMessageKind = "assistantMessage";
const turnTimeoutMilliseconds = 30_000;

type TurnRequest = { agentKey: string; subjectKind: string; subjectId: string; message: string; conversationId?: string };
type TurnBody = { agentRunId: string; conversationId: string; result: { messages: { kind: string; content: string }[]; actions: unknown[] } };
type StreamFrame = { kind: number; delta?: { text?: string; messageId?: string; result?: TurnBody } | null; error?: unknown };
type StreamState = {
  status: number;
  contentType: string | null;
  contentEncoding: string | null;
  frames: StreamFrame[];
  arrivals: number[];
  ended: boolean;
  failure: string | null;
};

const script = (value: Record<string, unknown>) => `${scriptPrefix}${JSON.stringify(value)}`;
const answerResult = (answer: string) => ({ answer });
const messageResult = (content: string, actions: unknown[] = []) => ({ messages: [{ kind: assistantMessageKind, content }], actions });
const helpRequest = (message: string, conversationId?: string): TurnRequest => ({
  agentKey: helpAgentKey, subjectKind: practiceSubjectKind, subjectId: practiceSubjectId, message, ...(conversationId ? { conversationId } : {}),
});

async function signIn(page: Page) {
  expect(password).toBeTruthy();
  await page.setExtraHTTPHeaders({ [forwardedForHeaderName]: ownerClientIp });
  await signInToClinic(page, ownerUsername, password!, defaultClinicOrigin);
  await page.goto(ClinicRoutes.Clinic.Index, { waitUntil: "domcontentloaded" });
}

/** One buffered turn through the browser's own origin, as the shared Web transport sends it. */
async function runBuffered(page: Page, request: TurnRequest): Promise<{ status: number; body: TurnBody }> {
  return page.evaluate(async ({ path, antiforgery, tenantHeader, requestIdHeader, payload }) => {
    const tenantHeaders = { [tenantHeader]: window.location.host };
    const token = await (await fetch(antiforgery, { credentials: "include", headers: tenantHeaders })).json() as { headerName: string; requestToken: string };
    const response = await fetch(path, {
      method: "POST",
      credentials: "include",
      headers: { ...tenantHeaders, [token.headerName]: token.requestToken, [requestIdHeader]: `e2e-${crypto.randomUUID()}`, "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });
    let body: unknown = null;
    try { body = await response.json(); } catch { /* the status is the diagnostic */ }
    return { status: response.status, body: body as TurnBody };
  }, { path: runPath, antiforgery: antiforgeryPath, tenantHeader: TENANT_HOST_HEADER, requestIdHeader: REQUEST_ID_HEADER, payload: request });
}

/**
 * Opens one streamed turn in the page and keeps reading it there: each decoded delta is written into a probe
 * element as it arrives, so what the test reads is what the browser received through the Next proxy.
 */
async function openStream(page: Page, id: string, request: TurnRequest): Promise<number> {
  return page.evaluate(async ({ streamId, path, antiforgery, tenantHeader, requestIdHeader, payload, deltaKind }) => {
    const tenantHeaders = { [tenantHeader]: window.location.host };
    const token = await (await fetch(antiforgery, { credentials: "include", headers: tenantHeaders })).json() as { headerName: string; requestToken: string };
    const controller = new AbortController();
    const state: StreamState & { controller: AbortController } = {
      status: 0, contentType: null, contentEncoding: null, frames: [], arrivals: [], ended: false, failure: null, controller,
    };
    const registry = ((window as unknown as { __turnStreams?: Record<string, typeof state> }).__turnStreams ??= {});
    registry[streamId] = state;
    const probe = document.body.appendChild(document.createElement("p"));
    probe.setAttribute("data-stream-probe", streamId);
    const response = await fetch(path, {
      method: "POST",
      credentials: "include",
      headers: { ...tenantHeaders, [token.headerName]: token.requestToken, [requestIdHeader]: `e2e-${crypto.randomUUID()}`, "Content-Type": "application/json" },
      body: JSON.stringify(payload),
      signal: controller.signal,
    });
    state.status = response.status;
    state.contentType = response.headers.get("content-type");
    state.contentEncoding = response.headers.get("content-encoding");
    void (async () => {
      const decoder = new TextDecoder("utf-8", { fatal: true });
      const reader = response.body!.getReader();
      let pending = "";
      let text = "";
      try {
        for (;;) {
          const chunk = await reader.read();
          pending += chunk.done ? decoder.decode() : decoder.decode(chunk.value, { stream: true });
          let newline = pending.indexOf("\n");
          while (newline >= 0) {
            const line = pending.slice(0, newline);
            pending = pending.slice(newline + 1);
            if (line.trim().length > 0) {
              const frame = JSON.parse(line) as StreamFrame;
              state.frames.push(frame);
              state.arrivals.push(performance.now());
              if (frame.kind === deltaKind && typeof frame.delta?.text === "string") {
                text += frame.delta.text;
                probe.textContent = text;
              }
            }
            newline = pending.indexOf("\n");
          }
          if (chunk.done) break;
        }
      } catch (error) {
        state.failure = error instanceof Error ? error.name : "error";
      } finally {
        state.ended = true;
      }
    })();
    return response.status;
  }, { streamId: id, path: streamPath, antiforgery: antiforgeryPath, tenantHeader: TENANT_HOST_HEADER, requestIdHeader: REQUEST_ID_HEADER, payload: request, deltaKind: StreamChunkKindValue.Delta });
}

async function readStream(page: Page, id: string): Promise<StreamState> {
  return page.evaluate((streamId) => {
    const { controller: _controller, ...state } = (window as unknown as { __turnStreams: Record<string, StreamState & { controller: AbortController }> }).__turnStreams[streamId];
    return state;
  }, id);
}

async function abortStream(page: Page, id: string) {
  await page.evaluate((streamId) => (window as unknown as { __turnStreams: Record<string, { controller: AbortController }> }).__turnStreams[streamId].controller.abort(), id);
}

type TrackCounts = Record<string, { started: number; completed: number; canceled: number }>;

/** Releases the named gates and reads the runner's per-track counters, on a conversation of its own. */
async function control(page: Page, release: string[] = []): Promise<TrackCounts> {
  const reply = await runBuffered(page, {
    agentKey: operationsAgentKey, subjectKind: practiceSubjectKind, subjectId: practiceSubjectId,
    message: script({ release, report: true, result: messageResult("report") }),
  });
  expect(reply.status, JSON.stringify(reply.body)).toBe(200);
  return JSON.parse(reply.body.result.messages[0].content) as TrackCounts;
}

const frameKinds = (state: StreamState) => state.frames.map((frame) => frame.kind);
const deltaText = (state: StreamState) => state.frames.filter((frame) => frame.kind === StreamChunkKindValue.Delta).map((frame) => frame.delta?.text ?? "").join("");
const terminalFrame = (state: StreamState) => state.frames.find((frame) => frame.kind === StreamChunkKindValue.Completed);

/** Types into the assistant composer and sends; a fill made before the page hydrates is lost, so it repeats until Send accepts it. */
async function sendFromComposer(page: Page, message: string) {
  const send = page.getByRole("button", { name: operationsMessages.composer.send, exact: true });
  await expect(async () => {
    await page.getByRole("textbox", { name: operationsMessages.composer.label }).fill(message);
    await expect(send).toBeEnabled({ timeout: 1_000 });
  }).toPass({ timeout: 60_000 });
  await send.click();
}

test.use({ actionTimeout: 30_000 });

test("a held provider paints the first answer segments in the browser through the proxy before the turn is released", async ({ page }) => {
  const gate = `gate-${randomUUID()}`;
  const track = `help-${randomUUID()}`;
  const firstSegment = "To book a patient, open the schedule ";
  const secondSegment = "and choose a free seat.";
  const answer = `${firstSegment}${secondSegment}`;
  await signIn(page);

  const status = await openStream(page, "held", helpRequest(script({
    track,
    steps: [{ answerDelta: firstSegment }, { answerDelta: secondSegment }, { hold: gate }],
    result: answerResult(answer),
  })));
  expect(status).toBe(200);

  // The provider is still held, yet the model-authored segments are on the page: a spinner or a replayed buffer cannot satisfy this.
  const probe = page.locator('[data-stream-probe="held"]');
  await expect(probe).toHaveText(answer, { timeout: turnTimeoutMilliseconds });
  const held = await readStream(page, "held");
  expect(held.contentType).toContain("application/x-ndjson");
  expect(held.contentEncoding, "the proxy must not compress the stream").toBeNull();
  expect(held.ended).toBe(false);
  expect(terminalFrame(held)).toBeUndefined();
  expect(frameKinds(held)[0]).toBe(StreamChunkKindValue.Started);
  expect(frameKinds(held).filter((kind) => kind === StreamChunkKindValue.Delta)).toHaveLength(2);
  expect(JSON.stringify(held.frames)).not.toContain("actions");
  expect((await control(page))[track]).toEqual({ started: 1, completed: 0, canceled: 0 });

  // Releasing the provider completes the turn; the authoritative result reconciles with what was painted.
  await control(page, [gate]);
  await expect.poll(async () => (await readStream(page, "held")).ended, { timeout: turnTimeoutMilliseconds }).toBe(true);
  const finished = await readStream(page, "held");
  expect(finished.failure).toBeNull();
  const completed = terminalFrame(finished)?.delta?.result;
  expect(completed?.result.messages.map((message) => message.content)).toEqual([answer]);
  expect(completed?.result.actions).toEqual([]);
  expect(deltaText(finished)).toBe(answer);
  expect(frameKinds(finished).at(-1)).toBe(StreamChunkKindValue.Completed);
  expect((await control(page))[track]).toEqual({ started: 1, completed: 1, canceled: 0 });
});

test("a browser that disconnects during a silent provider interval releases the lease, runs nothing twice and hears nothing of the canceled turn", async ({ page }) => {
  const gate = `gate-${randomUUID()}`;
  const canceledTrack = `canceled-${randomUUID()}`;
  const nextTrack = `next-${randomUUID()}`;
  const nextAnswer = "The next turn works.";
  await signIn(page);

  // An existing conversation carries the lease the aborted turn holds.
  const first = await runBuffered(page, helpRequest(script({ result: answerResult("First answer.") })));
  expect(first.status, JSON.stringify(first.body)).toBe(200);
  const conversationId = first.body.conversationId;

  expect(await openStream(page, "silent", helpRequest(script({ track: canceledTrack, steps: [{ hold: gate }], result: answerResult("Never delivered.") }), conversationId))).toBe(200);
  await expect.poll(async () => frameKinds(await readStream(page, "silent")), { timeout: turnTimeoutMilliseconds }).toContain(StreamChunkKindValue.Started);
  await expect.poll(async () => (await control(page))[canceledTrack]?.started, { timeout: turnTimeoutMilliseconds }).toBe(1);

  // While the provider is silent the conversation is leased: a second turn on it is refused, not run in parallel.
  const busy = await runBuffered(page, helpRequest(script({ track: nextTrack, result: answerResult("Parallel.") }), conversationId));
  expect(busy.status).toBe(409);

  // The browser goes away mid-turn: the abort travels through the proxy and cancels the run and its lease.
  await abortStream(page, "silent");
  await expect.poll(async () => (await control(page))[canceledTrack], { timeout: turnTimeoutMilliseconds }).toEqual({ started: 1, completed: 0, canceled: 1 });
  const afterAbort = await readStream(page, "silent");
  expect(terminalFrame(afterAbort)).toBeUndefined();

  // The lease is free: the next turn runs once and its stream carries nothing of the canceled turn.
  expect(await openStream(page, "next", helpRequest(script({ track: nextTrack, steps: [{ answerDelta: nextAnswer }], result: answerResult(nextAnswer) }), conversationId))).toBe(200);
  await expect.poll(async () => (await readStream(page, "next")).ended, { timeout: turnTimeoutMilliseconds }).toBe(true);
  const next = await readStream(page, "next");
  expect(next.failure).toBeNull();
  expect(deltaText(next)).toBe(nextAnswer);
  expect(terminalFrame(next)?.delta?.result?.result.messages.map((message) => message.content)).toEqual([nextAnswer]);
  expect(JSON.stringify(next.frames)).not.toContain("Never delivered");

  // Releasing the old gate later executes nothing: no duplicate or late completion of the canceled turn.
  const counts = await control(page, [gate]);
  expect(counts[canceledTrack]).toEqual({ started: 1, completed: 0, canceled: 1 });
  expect(counts[nextTrack]).toEqual({ started: 1, completed: 1, canceled: 0 });
  expect((await readStream(page, "silent")).frames).toEqual(afterAbort.frames);
});

test("Persian answer text crosses the proxy intact and an accepted stream that ends without a terminal frame is an unknown outcome that is never replayed", async ({ page }, testInfo) => {
  const gate = `gate-${randomUUID()}`;
  const helpTrack = `persian-${randomUUID()}`;
  const unknownTrack = `unknown-${randomUUID()}`;
  const segments = ["برای ثبت نوبت، ", "از بخش «نوبت‌دهی» ", "یک شیفت آزاد را انتخاب کنید."];
  const persianAnswer = segments.join("");
  const unknownText = `پاسخ ناشناخته ${randomUUID().slice(0, 8)}`;
  await signIn(page);

  // Persian segments arrive over the held stream as separate multi-byte UTF-8 frames.
  expect(await openStream(page, "persian", helpRequest(script({
    track: helpTrack,
    steps: [...segments.map((answerDelta) => ({ answerDelta })), { hold: gate }],
    result: answerResult(persianAnswer),
  })))).toBe(200);
  await expect(page.locator('[data-stream-probe="persian"]')).toHaveText(persianAnswer, { timeout: turnTimeoutMilliseconds });
  await control(page, [gate]);
  await expect.poll(async () => (await readStream(page, "persian")).ended, { timeout: turnTimeoutMilliseconds }).toBe(true);
  const persian = await readStream(page, "persian");
  expect(persian.failure).toBeNull();
  expect(terminalFrame(persian)?.delta?.result?.result.messages.map((message) => message.content)).toEqual([persianAnswer]);
  expect(deltaText(persian)).toBe(persianAnswer);
  expect(deltaText(persian)).not.toContain("�");

  // The assistant page runs its turn on the streamed route. The response keeps the real server's accepted frames but
  // loses its terminal frame, as a connection that drops after acceptance does.
  const streamRequests: string[] = [];
  await page.route((url) => url.pathname === streamPath, async (route) => {
    streamRequests.push(route.request().url());
    const response = await route.fetch();
    const lines = (await response.text()).split("\n").filter((line) => line.trim().length > 0);
    const withoutTerminal = lines.filter((line) => {
      const kind = (JSON.parse(line) as StreamFrame).kind;
      return kind !== StreamChunkKindValue.Completed && kind !== StreamChunkKindValue.Failed && kind !== StreamChunkKindValue.Canceled;
    });
    await route.fulfill({ response, body: `${withoutTerminal.join("\n")}\n` });
  });
  await page.goto(ClinicRoutes.Clinic.Assistant, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("textbox", { name: operationsMessages.composer.label })).toBeVisible({ timeout: 120_000 });
  await sendFromComposer(page, script({ track: unknownTrack, result: messageResult(unknownText) }));

  await expect(page.getByText(operationsMessages.outcomeUnknown, { exact: true })).toBeVisible({ timeout: turnTimeoutMilliseconds });
  await expect(page.getByText(unknownText, { exact: true })).toHaveCount(0);
  await expect(page.getByRole("button", { name: operationsMessages.retry })).toHaveCount(0);
  await page.screenshot({ path: testInfo.outputPath("agent-stream-unknown-outcome.png") });
  // One request reached the server and ran once; nothing replayed it.
  await page.waitForTimeout(2_000);
  expect(streamRequests).toHaveLength(1);
  expect((await control(page))[unknownTrack]).toEqual({ started: 1, completed: 1, canceled: 0 });
});

test("a streamed operations turn shows no prepared action before it completes and renders the Persian reply afterwards", async ({ page }, testInfo) => {
  const gate = `gate-${randomUUID()}`;
  const track = `ui-${randomUUID()}`;
  const seats = 4000 + (parseInt(randomUUID().slice(0, 3), 16) % 900);
  const summary = `پیش‌نویس شیفت ${seats}`;
  const reply = `شیفت آماده بررسی است ${seats}.`;
  await signIn(page);
  const streamRequests: string[] = [];
  page.on("request", (request) => { if (new URL(request.url()).pathname === streamPath) streamRequests.push(request.url()); });
  await page.goto(ClinicRoutes.Clinic.Assistant, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("textbox", { name: operationsMessages.composer.label })).toBeVisible({ timeout: 120_000 });

  await sendFromComposer(page, script({
    track,
    steps: [{ hold: gate }],
    calls: [{ tool: prepareShiftTool, arguments: { visibility: "Public", dayOfWeek: "Tuesday", startTime: "05:15", endTime: "06:45", timeZone: "UTC", totalSeats: seats } }],
    result: { messages: [{ kind: assistantMessageKind, content: reply }], actions: [{ capability: "Operations", actionType: "create_shift", summary }] },
  }));

  // The runner holds the provider silent: the page shows the working state and neither the reply nor any action.
  await expect.poll(async () => (await control(page))[track]?.started, { timeout: turnTimeoutMilliseconds }).toBe(1);
  await expect(page.getByLabel(workspaceMessages.running).first()).toBeVisible();
  await expect(page.getByText(operationsMessages.reviewTitle, { exact: true })).toHaveCount(0);
  await expect(page.getByText(reply, { exact: true })).toHaveCount(0);
  expect(streamRequests).toHaveLength(1);

  await control(page, [gate]);
  await expect(page.getByText(reply, { exact: true })).toBeVisible({ timeout: turnTimeoutMilliseconds });
  await expect(page.getByText(operationsMessages.reviewTitle, { exact: true }).first()).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath("agent-stream-reply.png") });
  expect((await control(page))[track]).toEqual({ started: 1, completed: 1, canceled: 0 });
});
