import { NextRequest } from "next/server";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

interface ModelCall {
  modelSettings?: { maxTokens?: number };
  signal?: AbortSignal;
}
interface ScriptedModel {
  getResponse: (request: ModelCall) => Promise<unknown>;
  getStreamedResponse: (request: ModelCall) => AsyncIterable<unknown>;
}
interface ScriptedTool {
  execute: (argumentsValue: unknown, context: unknown, details: unknown) => Promise<unknown>;
}

const scenario = vi.hoisted(() => ({
  model: undefined as unknown as ScriptedModel,
  drive: undefined as unknown as (model: ScriptedModel, tools: ScriptedTool[]) => Promise<void>,
}));

vi.mock("@openai/agents", () => ({
  Agent: class {
    tools: ScriptedTool[];
    constructor(options: { tools: ScriptedTool[] }) {
      this.tools = options.tools;
    }
  },
  tool: (definition: unknown) => definition,
  OpenAIProvider: class {
    getModel() {
      return scenario.model;
    }
  },
  Runner: class {
    constructor(private readonly options: { modelProvider: { getModel: (name: string) => Promise<ScriptedModel> } }) {}
    async run(agent: { tools: ScriptedTool[] }) {
      await scenario.drive(await this.options.modelProvider.getModel("test-model"), agent.tools);
      return { finalOutput: "reply", history: [], rawResponses: [{}], runContext: { usage: { inputTokens: 1, outputTokens: 1 } } };
    }
  },
}));

import { POST } from "./route";

const sharedSecret = "test-shared-secret";
const practiceId = "11111111-1111-4111-8111-111111111111";
const maxTokens = 10_000;
const tokenLimitCode = 6;

function done(totalTokens: number | undefined) {
  return { type: "response_done", response: { id: "r", usage: { inputTokens: 0, outputTokens: 0, totalTokens }, output: [] } };
}
async function* stream(...events: unknown[]) {
  for (const event of events) yield event;
}
async function drain(source: AsyncIterable<unknown>) {
  for await (const event of source) void event;
}

function runRequest(): NextRequest {
  const now = new Date();
  return new NextRequest("http://localhost/api/internal/intelligence-agent/run", {
    method: "POST",
    headers: { authorization: `Bearer ${sharedSecret}`, "content-type": "application/json" },
    body: JSON.stringify({
      definition: { model: "test-model", instructions: "instructions" },
      input: { userInput: "question" },
      tools: [{ name: "lookup", description: "lookup", parametersJsonSchema: JSON.stringify({ type: "object", properties: {}, additionalProperties: false }) }],
      executionEnvelope: {
        version: 1, executionId: "execution-1", agentKey: "agent", tenantId: practiceId, practiceId,
        subjectKind: "practice", subjectId: practiceId, authenticatedUserId: "user", conversationId: "conversation",
        toolCallId: "", toolName: "", credentialVersion: 1, expiresAt: new Date(now.getTime() + 60_000).toISOString(),
        correlationId: "correlation", role: "owner", locale: "en", timeZone: "UTC", calendar: "gregory",
        currentUtc: now.toISOString(), currentLocal: now.toISOString(), permissions: ["read"],
      },
      limits: { maxIterations: 4, maxToolCalls: 2, maxTokens, effectiveTimeout: "00:01:00" },
      callbackUrl: "http://localhost/callback",
      callbackCredential: "credential",
    }),
  });
}

async function outcome(response: Response): Promise<{ code: number; message: string; isSuccess: boolean }> {
  const body = await response.json();
  return { code: body.error?.code, message: body.error?.message, isSuccess: body.isSuccess };
}

describe("agent runtime token budget parity between buffered and streamed model calls", () => {
  let providerCalls: number;
  let requests: ModelCall[];
  beforeEach(() => {
    vi.stubEnv("AGENT_RUNTIME_SHARED_SECRET", sharedSecret);
    vi.spyOn(console, "info").mockImplementation(() => undefined);
    vi.spyOn(console, "error").mockImplementation(() => undefined);
    providerCalls = 0;
    requests = [];
    scenario.model = {
      getResponse: async () => ({ usage: { inputTokens: 0, outputTokens: 0, totalTokens: 12_000 }, output: [] }),
      getStreamedResponse: (request) => {
        providerCalls += 1;
        requests.push(request);
        return stream(done(12_000));
      },
    };
  });
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.restoreAllMocks();
  });

  it("reports the same token-limit error when buffered and streamed usage exceed the budget", async () => {
    scenario.drive = async (model) => void (await model.getResponse({}));
    const buffered = await outcome(await POST(runRequest()));
    scenario.drive = async (model) => drain(model.getStreamedResponse({}));
    const streamed = await outcome(await POST(runRequest()));

    expect(buffered).toMatchObject({ isSuccess: false, code: tokenLimitCode });
    expect(streamed).toEqual(buffered);
  });

  it("fails safely when the stream ends or completes without reported usage", async () => {
    for (const events of [[], [done(undefined)]]) {
      scenario.model.getStreamedResponse = () => stream(...events);
      scenario.drive = async (model) => drain(model.getStreamedResponse({}));
      expect(await outcome(await POST(runRequest()))).toMatchObject({ isSuccess: false, code: tokenLimitCode });
    }
  });

  it("accounts usage across rounds, clamps each request to the remaining budget and blocks the exhausted round", async () => {
    scenario.model.getStreamedResponse = (request) => {
      requests.push(request);
      return stream(done(4_000));
    };
    scenario.drive = async (model) => {
      for (let round = 0; round < 4; round += 1) {
        await drain(model.getStreamedResponse({ modelSettings: { maxTokens } }));
      }
    };

    expect(await outcome(await POST(runRequest()))).toMatchObject({ isSuccess: false, code: tokenLimitCode });
    expect(requests.map((request) => request.modelSettings?.maxTokens)).toEqual([10_000, 6_000, 2_000]);
  });

  it("clamps buffered and streamed requests to the same remaining budget", async () => {
    const buffered: Array<number | undefined> = [];
    const streamed: Array<number | undefined> = [];
    const usage = { inputTokens: 0, outputTokens: 0, totalTokens: 4_000 };
    scenario.model.getResponse = async (request) => {
      buffered.push(request.modelSettings?.maxTokens);
      return { usage, output: [] };
    };
    scenario.model.getStreamedResponse = (request) => {
      streamed.push(request.modelSettings?.maxTokens);
      return stream(done(4_000));
    };
    const rounds = async (call: (model: ScriptedModel, settings: ModelCall) => Promise<unknown>, model: ScriptedModel) => {
      await call(model, { modelSettings: { maxTokens } });
      await call(model, { modelSettings: { maxTokens: 3_000 } });
      await call(model, {});
    };
    scenario.drive = (model) => rounds((m, request) => m.getResponse(request), model);
    await POST(runRequest());
    scenario.drive = (model) => rounds((m, request) => drain(m.getStreamedResponse(request)), model);
    await POST(runRequest());

    expect(buffered).toEqual([10_000, 3_000, 2_000]);
    expect(streamed).toEqual(buffered);
  });

  it("logs the same per-call timing and usage record for streamed and buffered calls, without content", async () => {
    const info = vi.spyOn(console, "info").mockImplementation(() => undefined);
    const reported = {
      inputTokens: 120, outputTokens: 45, totalTokens: 165,
      inputTokensDetails: { cached_tokens: 100 }, outputTokensDetails: { reasoning_tokens: 30 },
    };
    scenario.model.getResponse = async () => ({ usage: reported, output: [] });
    scenario.model.getStreamedResponse = () => stream(
      { type: "output_text_delta", delta: "secret-answer-text" },
      { type: "response_done", response: { id: "r", usage: reported, output: [] } },
    );
    const modelCalls = () => info.mock.calls.filter(([message]) => message === "Agent model call completed").map(([, fields]) => fields);

    scenario.drive = async (model) => {
      await model.getResponse({});
      await model.getResponse({});
    };
    await POST(runRequest());
    const bufferedCalls = modelCalls();
    info.mockClear();
    scenario.drive = async (model) => {
      await drain(model.getStreamedResponse({}));
      await drain(model.getStreamedResponse({}));
    };
    await POST(runRequest());
    const streamedCalls = modelCalls();

    const shared = { executionId: "execution-1", inputTokens: 120, outputTokens: 45, reasoningTokens: 30, cachedInputTokens: 100 };
    expect(bufferedCalls.map((call) => call.turn)).toEqual([1, 2]);
    expect(streamedCalls.map((call) => call.turn)).toEqual([1, 2]);
    for (const call of [...bufferedCalls, ...streamedCalls]) {
      expect(call).toMatchObject(shared);
      expect(call.durationMs).toBeGreaterThanOrEqual(0);
    }
    expect(bufferedCalls.every((call) => call.streamed === false)).toBe(true);
    expect(streamedCalls.every((call) => call.streamed === true)).toBe(true);
    expect(Object.keys(streamedCalls[0]).sort()).toEqual(Object.keys(bufferedCalls[0]).sort());
    expect(JSON.stringify(info.mock.calls)).not.toContain("secret");
  });

  it("forwards the abort signal to the provider and blocks later rounds and tools after a mid-stream cancellation", async () => {
    const controller = new AbortController();
    scenario.model.getStreamedResponse = (request) => {
      providerCalls += 1;
      requests.push(request);
      return (async function* () {
        yield { type: "output_text_delta", delta: "partial" };
        controller.abort();
        throw new DOMException("aborted", "AbortError");
      })();
    };
    let laterRound: unknown;
    let laterTool: unknown;
    scenario.drive = async (model, tools) => {
      const request = { signal: controller.signal };
      await drain(model.getStreamedResponse(request)).catch(() => undefined);
      laterRound = await drain(model.getStreamedResponse(request)).catch((error: unknown) => error);
      laterTool = await tools[0].execute({}, {}, { toolCall: { callId: "call" } }).catch((error: unknown) => error);
      throw laterRound;
    };

    const result = await outcome(await POST(runRequest()));

    expect(requests[0].signal).toBe(controller.signal);
    expect(providerCalls).toBe(1);
    expect(laterRound).toMatchObject({ code: "TokenLimitExceeded" });
    expect(laterTool).toMatchObject({ code: "TokenLimitExceeded" });
    expect(result).toMatchObject({ isSuccess: false, code: tokenLimitCode });
  });
});
