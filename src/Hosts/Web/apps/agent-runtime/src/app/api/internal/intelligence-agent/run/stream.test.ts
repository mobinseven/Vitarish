import { NextRequest } from "next/server";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

interface ModelCall {
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
  drive: undefined as unknown as (model: ScriptedModel, tools: ScriptedTool[], signal: AbortSignal) => Promise<void>,
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
    async run(agent: { tools: ScriptedTool[] }, _input: unknown, options: { stream?: boolean; signal: AbortSignal }) {
      await scenario.drive(await this.options.modelProvider.getModel("test-model"), agent.tools, options.signal);
      const result = { finalOutput: "reply", history: [], rawResponses: [{}], runContext: { usage: { inputTokens: 1, outputTokens: 1 } } };
      return options.stream
        ? { ...result, error: undefined, completed: Promise.resolve(), async *[Symbol.asyncIterator]() {} }
        : result;
    }
  },
}));

import { POST as bufferedPost } from "./route";
import { POST as streamedPost } from "./stream/route";
import {
  AGENT_RUN_FRAME_BUFFER_LIMIT,
  AGENT_RUN_STREAM_CONTENT_TYPE,
  AgentRunFrameKind,
  AgentRunStage,
  type AgentRunFrame,
} from "./stream-frames";

const sharedSecret = "test-shared-secret";
const practiceId = "11111111-1111-4111-8111-111111111111";
const maxTokens = 10_000;
const bufferedPath = "run";
const streamedPath = "run/stream";
const secretArgument = "secret-argument-value";
const secretOutput = "secret-tool-output";
const secretText = "secret-answer-text";
const secretReasoning = "secret-reasoning";
const toolName = "lookup";
const providerFailureCode = 10;
const toolLimitCode = 5;
const tokenLimitCode = 6;

function done(totalTokens: number) {
  return { type: "response_done", response: { id: "r", usage: { inputTokens: 0, outputTokens: 0, totalTokens }, output: [] } };
}
async function* stream(...events: unknown[]) {
  for (const event of events) yield event;
}
async function drain(source: AsyncIterable<unknown>) {
  for await (const event of source) void event;
}
function abortedProviderStream(request: ModelCall, onStopped: () => void): AsyncIterable<unknown> {
  return (async function* () {
    await new Promise<void>((resolve) => {
      if (request.signal?.aborted) resolve();
      else request.signal?.addEventListener("abort", () => resolve(), { once: true });
    });
    onStopped();
    throw new DOMException("aborted", "AbortError");
  })();
}

function runRequest(path: string, signal?: AbortSignal, authorization = `Bearer ${sharedSecret}`, maxToolCalls = 2): NextRequest {
  const now = new Date();
  return new NextRequest(`http://localhost/api/internal/intelligence-agent/${path}`, {
    method: "POST",
    signal,
    headers: { authorization, "content-type": "application/json" },
    body: JSON.stringify({
      definition: { model: "test-model", instructions: "instructions" },
      input: { userInput: "question" },
      tools: [{ name: toolName, description: "lookup", parametersJsonSchema: JSON.stringify({ type: "object", properties: {}, additionalProperties: false }) }],
      executionEnvelope: {
        version: 1, executionId: "execution-1", agentKey: "agent", tenantId: practiceId, practiceId,
        subjectKind: "practice", subjectId: practiceId, authenticatedUserId: "user", conversationId: "conversation",
        toolCallId: "", toolName: "", credentialVersion: 1, expiresAt: new Date(now.getTime() + 60_000).toISOString(),
        correlationId: "correlation", role: "owner", locale: "en", timeZone: "UTC", calendar: "gregory",
        currentUtc: now.toISOString(), currentLocal: now.toISOString(), permissions: ["read"],
      },
      limits: { maxIterations: 4, maxToolCalls, maxTokens, effectiveTimeout: "00:01:00" },
      callbackUrl: "http://localhost/callback",
      callbackCredential: "credential",
    }),
  });
}

async function frames(response: Response): Promise<AgentRunFrame[]> {
  const text = await response.text();
  return text.split("\n").filter((line) => line.length > 0).map((line) => JSON.parse(line) as AgentRunFrame);
}

function withoutKind(frame: unknown): Record<string, unknown> {
  const { kind, ...body } = frame as Record<string, unknown>;
  void kind;
  return body;
}

/** Runs both routes against the current scenario and returns the buffered body and the streamed frames. */
async function runBoth() {
  const buffered = (await (await bufferedPost(runRequest(bufferedPath))).json()) as Record<string, unknown>;
  const streamed = await frames(await streamedPost(runRequest(streamedPath)));
  return { buffered, streamed, terminal: streamed[streamed.length - 1] };
}

const tick = () => new Promise((resolve) => setTimeout(resolve, 20));

describe("agent runtime streamed run", () => {
  beforeEach(() => {
    vi.stubEnv("AGENT_RUNTIME_SHARED_SECRET", sharedSecret);
    vi.spyOn(console, "info").mockImplementation(() => undefined);
    vi.spyOn(console, "error").mockImplementation(() => undefined);
    vi.stubGlobal("fetch", vi.fn(async () => Response.json({ callId: "call", output: secretOutput, isError: false })));
    scenario.model = {
      getResponse: async () => ({ usage: { inputTokens: 0, outputTokens: 0, totalTokens: 1 }, output: [] }),
      getStreamedResponse: () => stream(done(1)),
    };
  });
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("emits stage frames in order and exactly one terminal result with the buffered response body", async () => {
    scenario.drive = async (model, tools) => {
      await drain(model.getStreamedResponse({}));
      await tools[0].execute({}, {}, { toolCall: { callId: "call" } });
      await drain(model.getStreamedResponse({}));
    };

    const response = await streamedPost(runRequest(streamedPath));
    const received = await frames(response);
    const buffered = await (await bufferedPost(runRequest(bufferedPath))).json();

    expect(response.headers.get("content-type")).toBe(AGENT_RUN_STREAM_CONTENT_TYPE);
    expect(received.map((frame) => (frame.kind === AgentRunFrameKind.Stage ? `${frame.stage}:${frame.ordinal}` : frame.kind))).toEqual([
      `${AgentRunStage.ModelRoundStarted}:1`,
      `${AgentRunStage.ToolStarted}:1`,
      `${AgentRunStage.ToolCompleted}:1`,
      `${AgentRunStage.ModelRoundStarted}:2`,
      AgentRunFrameKind.Result,
    ]);
    expect(received.filter((frame) => frame.kind === AgentRunFrameKind.Result)).toHaveLength(1);
    expect(withoutKind(received[received.length - 1])).toEqual(buffered);
  });

  it("maps token-limit, provider and tool-limit failures to the same coded result as the buffered route", async () => {
    scenario.model.getStreamedResponse = () => stream(done(12_000));
    scenario.drive = async (model) => drain(model.getStreamedResponse({}));
    const tokenLimit = await runBoth();
    expect(tokenLimit.terminal).toMatchObject({ kind: AgentRunFrameKind.Result, isSuccess: false, error: { code: tokenLimitCode } });
    expect(withoutKind(tokenLimit.terminal)).toEqual(tokenLimit.buffered);

    scenario.drive = async () => {
      throw new Error("provider secret detail");
    };
    const provider = await runBoth();
    expect(provider.terminal).toMatchObject({ isSuccess: false, error: { code: providerFailureCode } });
    expect(withoutKind(provider.terminal)).toEqual(provider.buffered);
    expect(JSON.stringify(provider.streamed)).not.toContain("provider secret detail");
    expect(provider.streamed.filter((frame) => frame.kind === AgentRunFrameKind.Result)).toHaveLength(1);

    scenario.drive = async (_model, tools) => {
      for (let call = 0; call < 3; call += 1) await tools[0].execute({}, {}, { toolCall: { callId: `call-${call}` } });
    };
    const toolLimit = await runBoth();
    expect(toolLimit.terminal).toMatchObject({ isSuccess: false, error: { code: toolLimitCode } });
    expect(withoutKind(toolLimit.terminal)).toEqual(toolLimit.buffered);
  });

  it("keeps the buffered status and shape for errors detected before streaming starts", async () => {
    const unauthorized = await streamedPost(runRequest(streamedPath, undefined, "Bearer wrong"));
    expect(unauthorized.status).toBe(401);
    expect(await unauthorized.json()).toEqual({ error: "Unauthorized" });

    const malformed = await streamedPost(new NextRequest(`http://localhost/${streamedPath}`, {
      method: "POST",
      headers: { authorization: `Bearer ${sharedSecret}` },
      body: "not json",
    }));
    expect(malformed.status).toBe(400);
    expect(malformed.headers.get("content-type")).not.toContain(AGENT_RUN_STREAM_CONTENT_TYPE);
  });

  it("aborts the provider call and leaves no work running when the consumer cancels", async () => {
    let providerSignal: AbortSignal | undefined;
    let providerCalls = 0;
    let providerStopped = false;
    scenario.model.getStreamedResponse = (request) => {
      providerCalls += 1;
      providerSignal = request.signal;
      return abortedProviderStream(request, () => { providerStopped = true; });
    };
    scenario.drive = async (model, _tools, signal) => {
      await drain(model.getStreamedResponse({ signal }));
      await drain(model.getStreamedResponse({ signal }));
    };

    const response = await streamedPost(runRequest(streamedPath));
    const reader = response.body!.getReader();
    const first = JSON.parse(new TextDecoder().decode((await reader.read()).value));
    expect(first).toMatchObject({ kind: AgentRunFrameKind.Stage, stage: AgentRunStage.ModelRoundStarted });
    await reader.cancel();
    await tick();

    expect(providerSignal?.aborted).toBe(true);
    expect(providerStopped).toBe(true);
    expect(providerCalls).toBe(1);
  });

  it("aborts the run when the incoming request is aborted", async () => {
    const disconnect = new AbortController();
    let providerStopped = false;
    scenario.model.getStreamedResponse = (request) => abortedProviderStream(request, () => { providerStopped = true; });
    scenario.drive = async (model, _tools, signal) => drain(model.getStreamedResponse({ signal }));

    const response = await streamedPost(runRequest(streamedPath, disconnect.signal));
    await tick();
    disconnect.abort();
    await response.text();

    expect(providerStopped).toBe(true);
  });

  it("bounds queued frames and stalls the run while the consumer is slow, then delivers every frame in order", async () => {
    const rounds = 40;
    let roundsStarted = 0;
    scenario.model.getStreamedResponse = () => {
      roundsStarted += 1;
      return stream(done(1));
    };
    scenario.drive = async (model) => {
      for (let round = 0; round < rounds; round += 1) await drain(model.getStreamedResponse({}));
    };

    const response = await streamedPost(runRequest(streamedPath));
    await tick();
    await tick();
    expect(roundsStarted).toBeLessThanOrEqual(AGENT_RUN_FRAME_BUFFER_LIMIT + 1);
    expect(roundsStarted).toBeLessThan(rounds);

    const received = await frames(response);
    expect(received).toHaveLength(rounds + 1);
    expect(received.slice(0, rounds).map((frame) => (frame as { ordinal: number }).ordinal)).toEqual(
      Array.from({ length: rounds }, (_, index) => index + 1),
    );
    expect(received[rounds].kind).toBe(AgentRunFrameKind.Result);
  });

  it("resumes every concurrent tool-stage writer blocked on a full queue once the consumer drains it", async () => {
    const concurrentTools = 10;
    scenario.drive = async (_model, tools) => {
      await Promise.all(Array.from({ length: concurrentTools }, (_, call) => tools[0].execute({}, {}, { toolCall: { callId: `call-${call}` } })));
    };

    const response = await streamedPost(runRequest(streamedPath, undefined, undefined, concurrentTools));
    await tick();
    await tick();
    const received = await frames(response);

    const stages = received.filter((frame) => frame.kind === AgentRunFrameKind.Stage) as Array<{ stage: string; ordinal: number }>;
    expect(stages.filter((frame) => frame.stage === AgentRunStage.ToolStarted).map((frame) => frame.ordinal).sort((a, b) => a - b))
      .toEqual(Array.from({ length: concurrentTools }, (_, index) => index + 1));
    expect(stages.filter((frame) => frame.stage === AgentRunStage.ToolCompleted)).toHaveLength(concurrentTools);
    expect(received[received.length - 1]).toMatchObject({ kind: AgentRunFrameKind.Result, isSuccess: true });
  });

  it("releases writers waiting on a full queue when the consumer cancels", async () => {
    const concurrentTools = 10;
    let settled = 0;
    scenario.drive = async (_model, tools) => {
      await Promise.all(Array.from({ length: concurrentTools }, (_, call) =>
        tools[0].execute({}, {}, { toolCall: { callId: `call-${call}` } }).catch(() => undefined).finally(() => { settled += 1; })));
    };

    const response = await streamedPost(runRequest(streamedPath, undefined, undefined, concurrentTools));
    await tick();
    expect(settled).toBe(0);
    await response.body!.cancel();
    await tick();

    expect(settled).toBe(concurrentTools);
  });

  it("does not accumulate abort listeners over a long sequence of frames to a slow consumer", async () => {
    const rounds = 120;
    scenario.model.getStreamedResponse = () => stream(done(1));
    scenario.drive = async (model) => {
      for (let round = 0; round < rounds; round += 1) await drain(model.getStreamedResponse({}));
    };
    const added = vi.spyOn(AbortSignal.prototype, "addEventListener");
    const removed = vi.spyOn(AbortSignal.prototype, "removeEventListener");

    const response = await streamedPost(runRequest(streamedPath, undefined, undefined, 2));
    const reader = response.body!.getReader();
    for (;;) {
      const { done: finished } = await reader.read();
      if (finished) break;
      await new Promise((resolve) => setTimeout(resolve, 0));
    }

    const abortCalls = (spy: typeof added) => spy.mock.calls.filter(([type]) => type === "abort").length;
    expect(abortCalls(added) - abortCalls(removed)).toBeLessThanOrEqual(2);
  });

  it("never puts tool names, arguments, outputs, answer text or reasoning in a stage frame", async () => {
    scenario.model.getStreamedResponse = () => stream(
      { type: "output_text_delta", delta: secretText },
      { type: "model", event: { type: "reasoning-delta", delta: secretReasoning } },
      done(1),
    );
    scenario.drive = async (model, tools) => {
      await drain(model.getStreamedResponse({}));
      await tools[0].execute({ value: secretArgument }, {}, { toolCall: { callId: "call" } });
    };

    const received = await frames(await streamedPost(runRequest(streamedPath)));
    const stages = received.filter((frame) => frame.kind === AgentRunFrameKind.Stage);

    expect(stages.length).toBeGreaterThan(0);
    for (const frame of stages) expect(Object.keys(frame).sort()).toEqual(["kind", "ordinal", "stage"]);
    const stageText = JSON.stringify(stages);
    for (const secret of [secretArgument, secretOutput, secretText, secretReasoning, toolName]) {
      expect(stageText).not.toContain(secret);
    }
  });
});
