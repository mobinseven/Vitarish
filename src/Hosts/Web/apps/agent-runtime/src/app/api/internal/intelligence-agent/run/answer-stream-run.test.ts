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
      const result = { finalOutput: { answer: "terminal" }, history: [], rawResponses: [{}], runContext: { usage: { inputTokens: 1, outputTokens: 1 } } };
      return options.stream
        ? { ...result, error: undefined, completed: Promise.resolve(), async *[Symbol.asyncIterator]() {} }
        : result;
    }
  },
}));

import { POST as bufferedPost } from "./route";
import { POST as streamedPost } from "./stream/route";
import { AgentRunFrameKind, MAX_ANSWER_DELTA_CHARS, type AgentRunFrame } from "./stream-frames";

const sharedSecret = "test-shared-secret";
const practiceId = "11111111-1111-4111-8111-111111111111";
const maxTokens = 10_000;
const messageId = "answer-1";
const answerField = "answer";
const secretReasoning = "secret-reasoning";
const secretField = "secret-other-field";
const providerFailureCode = 10;
const tokenLimitCode = 6;
const toolDefinition = { name: "lookup", description: "lookup", parametersJsonSchema: JSON.stringify({ type: "object", properties: {}, additionalProperties: false }) };
const resultDefinition = { name: "reply", strict: true, jsonSchema: JSON.stringify({ type: "object" }) };

function done(totalTokens: number, output: unknown[] = []) {
  return { type: "response_done", response: { id: "r", usage: { inputTokens: 0, outputTokens: 0, totalTokens }, output } };
}
async function* stream(...events: unknown[]) {
  for (const event of events) yield event;
}
const textEvent = (delta: string) => ({ type: "output_text_delta", delta });
async function drain(source: AsyncIterable<unknown>) {
  for await (const event of source) void event;
}
function chunked(text: string, size: number): string[] {
  const chunks: string[] = [];
  for (let start = 0; start < text.length; start += size) chunks.push(text.slice(start, start + size));
  return chunks;
}
/** The model output text; JSON.stringify supplies every escape (quotes, backslashes, newlines) under test. */
function answerJson(answer: string): string {
  return JSON.stringify({ reasoning: secretReasoning, other: secretField, [answerField]: answer });
}

interface RunOptions {
  tools?: unknown[];
  answerStream?: unknown;
  result?: unknown;
}
function runRequest(path: string, options: RunOptions = {}): NextRequest {
  const now = new Date();
  return new NextRequest(`http://localhost/api/internal/intelligence-agent/${path}`, {
    method: "POST",
    headers: { authorization: `Bearer ${sharedSecret}`, "content-type": "application/json" },
    body: JSON.stringify({
      definition: { model: "test-model", instructions: "instructions", result: "result" in options ? options.result : resultDefinition },
      input: { userInput: "question" },
      tools: options.tools ?? [],
      answerStream: "answerStream" in options ? options.answerStream : { messageId, fieldPath: [answerField] },
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

async function frames(response: Response): Promise<AgentRunFrame[]> {
  const text = await response.text();
  return text.split("\n").filter((line) => line.length > 0).map((line) => JSON.parse(line) as AgentRunFrame);
}
const deltas = (received: AgentRunFrame[]) => received.filter((frame) => frame.kind === AgentRunFrameKind.Delta);
const answerText = (received: AgentRunFrame[]) => deltas(received).map((frame) => (frame as { text: string }).text).join("");
const terminalOf = (received: AgentRunFrame[]) => received[received.length - 1];
const tick = () => new Promise((resolve) => setTimeout(resolve, 20));

function scriptRound(...chunks: string[]) {
  scenario.model.getStreamedResponse = () => stream(...chunks.map(textEvent), done(1));
  scenario.drive = async (model) => drain(model.getStreamedResponse({}));
}

describe("agent runtime answer-field streaming", () => {
  beforeEach(() => {
    vi.stubEnv("AGENT_RUNTIME_SHARED_SECRET", sharedSecret);
    vi.spyOn(console, "info").mockImplementation(() => undefined);
    vi.spyOn(console, "error").mockImplementation(() => undefined);
    vi.stubGlobal("fetch", vi.fn(async () => Response.json({ callId: "call", output: "tool-output", isError: false })));
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

  it("streams only the declared answer text, decoded across tiny frames, ahead of the authoritative terminal frame", async () => {
    const answer = ['He said "hi" ', String.fromCharCode(92), String.fromCharCode(10), " سلام دنیا 😀 end"].join(" ");
    scriptRound(...chunked(answerJson(answer), 3));

    const received = await frames(await streamedPost(runRequest("run/stream")));

    expect(answerText(received)).toBe(answer);
    for (const frame of deltas(received)) {
      expect(Object.keys(frame).sort()).toEqual(["kind", "messageId", "text"]);
      expect(frame).toMatchObject({ messageId });
    }
    const kinds = received.map((frame) => frame.kind);
    expect(kinds.filter((kind) => kind === AgentRunFrameKind.Result)).toHaveLength(1);
    expect(kinds[kinds.length - 1]).toBe(AgentRunFrameKind.Result);
    expect(kinds.indexOf(AgentRunFrameKind.Delta)).toBeLessThan(kinds.indexOf(AgentRunFrameKind.Result));
    const wire = JSON.stringify(deltas(received));
    for (const forbidden of [secretReasoning, secretField, "reasoning"]) expect(wire).not.toContain(forbidden);
    expect(terminalOf(received)).toMatchObject({ isSuccess: true, structuredOutput: { answer: "terminal" } });
  });

  it("splits a long answer into bounded frames", async () => {
    const answer = "x".repeat(MAX_ANSWER_DELTA_CHARS * 2 + 5);
    scriptRound(answerJson(answer));

    const received = await frames(await streamedPost(runRequest("run/stream")));

    expect(answerText(received)).toBe(answer);
    expect(deltas(received).length).toBeGreaterThanOrEqual(3);
    for (const frame of deltas(received)) expect((frame as { text: string }).text.length).toBeLessThanOrEqual(MAX_ANSWER_DELTA_CHARS);
  });

  it("stays progress-only when the request does not declare the answer stream", async () => {
    scriptRound(...chunked(answerJson("hello"), 4));

    const absent = await frames(await streamedPost(runRequest("run/stream", { answerStream: undefined })));
    const nullDeclaration = await frames(await streamedPost(runRequest("run/stream", { answerStream: null })));

    expect(deltas(absent)).toHaveLength(0);
    expect(deltas(nullDeclaration)).toHaveLength(0);
    expect(terminalOf(absent)).toMatchObject({ isSuccess: true });
  });

  it("stays progress-only when tools are available, even for the declared field", async () => {
    scriptRound(...chunked(answerJson("hello"), 4));

    const received = await frames(await streamedPost(runRequest("run/stream", { tools: [toolDefinition] })));

    expect(deltas(received)).toHaveLength(0);
    expect(terminalOf(received)).toMatchObject({ isSuccess: true });
  });

  it("never streams from a round that requested a tool call, and not from later rounds", async () => {
    let round = 0;
    scenario.model.getStreamedResponse = () => {
      round += 1;
      return round === 1
        ? stream(done(1, [{ type: "function_call", name: "lookup" }]))
        : stream(...chunked(answerJson("second"), 5).map(textEvent), done(1));
    };
    scenario.drive = async (model) => {
      await drain(model.getStreamedResponse({}));
      await drain(model.getStreamedResponse({}));
    };

    const received = await frames(await streamedPost(runRequest("run/stream")));

    expect(deltas(received)).toHaveLength(0);
    expect(terminalOf(received)).toMatchObject({ isSuccess: true });
  });

  it("stops streaming on malformed or wrong-typed fields and still returns the buffered terminal result", async () => {
    const buffered = await (await bufferedPost(runRequest("run"))).json();
    const cases: Array<[string, string]> = [
      [`{"${answerField}":42}`, ""],
      [`{"other":"x"}}{"${answerField}":"late"}`, ""],
      [`{"other":garbage,"${answerField}":"visible"}`, ""],
      [`{"other":01,"${answerField}":"visible"}`, ""],
      [`{"nested":{"${answerField}":"wrong"},"${answerField}":"right"}`, "right"],
    ];
    for (const [output, expectedText] of cases) {
      scriptRound(...chunked(output, 2));
      const received = await frames(await streamedPost(runRequest("run/stream")));
      expect(answerText(received), output).toBe(expectedText);
      const { kind, ...body } = terminalOf(received) as Record<string, unknown>;
      expect(kind).toBe(AgentRunFrameKind.Result);
      expect(body, output).toEqual(buffered);
    }
  });

  it("fails the run when text was streamed from output that was not one well-formed object", async () => {
    const cases: Array<[string, string]> = [
      [`{"${answerField}":"visible","count":`, "visible"],
      [`{"${answerField}":"first","${answerField}":"second"}`, "first"],
      [`{"${answerField}":"ok" "more":1}`, "ok"],
      [`{"${answerField}":"ok","count":01}`, "ok"],
      [`{"${answerField}":"ok"}{"${answerField}":"again"}`, "ok"],
    ];
    for (const [output, expectedText] of cases) {
      scriptRound(...chunked(output, 2));

      const received = await frames(await streamedPost(runRequest("run/stream")));

      expect(answerText(received), output).toBe(expectedText);
      expect(terminalOf(received), output).toMatchObject({ kind: AgentRunFrameKind.Result, isSuccess: false, error: { code: providerFailureCode } });
      expect(received.filter((frame) => frame.kind === AgentRunFrameKind.Result)).toHaveLength(1);
    }
  });

  it("does not fail a run whose malformed output never streamed any text", async () => {
    scriptRound(`{"${answerField}":"first","${answerField}":"second"}`);

    const received = await frames(await streamedPost(runRequest("run/stream")));

    expect(deltas(received)).toHaveLength(0);
    expect(terminalOf(received)).toMatchObject({ isSuccess: true });
  });

  it("fails with the token-limit result when the budget is exhausted after streaming started", async () => {
    scenario.model.getStreamedResponse = () => stream(...chunked(answerJson("partial answer"), 4).map(textEvent), done(12_000));
    scenario.drive = async (model) => drain(model.getStreamedResponse({}));

    const received = await frames(await streamedPost(runRequest("run/stream")));

    expect(answerText(received)).toBe("partial answer");
    expect(terminalOf(received)).toMatchObject({ kind: AgentRunFrameKind.Result, isSuccess: false, error: { code: tokenLimitCode } });
    expect(received.filter((frame) => frame.kind === AgentRunFrameKind.Result)).toHaveLength(1);
  });

  it("aborts the provider call and stops pulling when the consumer cancels mid-answer", async () => {
    let providerStopped = false;
    let pulledAfterCancel = 0;
    let cancelled = false;
    scenario.model.getStreamedResponse = () => (async function* () {
      try {
        for (const chunk of chunked(answerJson("a".repeat(500)), 10)) {
          if (cancelled) pulledAfterCancel += 1;
          yield textEvent(chunk);
          await tick();
        }
        yield done(1);
      } finally {
        providerStopped = true;
      }
    })();
    scenario.drive = async (model, _tools, signal) => {
      signal.throwIfAborted();
      await drain(model.getStreamedResponse({ signal }));
    };

    const response = await streamedPost(runRequest("run/stream"));
    const reader = response.body!.getReader();
    for (let read = 0; read < 3; read += 1) await reader.read();
    cancelled = true;
    await reader.cancel();
    await tick();
    await tick();

    expect(providerStopped).toBe(true);
    expect(pulledAfterCancel).toBeLessThanOrEqual(2);
  });

  it("applies backpressure: a slow consumer stalls the provider instead of growing an unbounded queue", async () => {
    let chunksPulled = 0;
    const chunks = chunked(answerJson("a".repeat(2000)), 1);
    scenario.model.getStreamedResponse = () => (async function* () {
      for (const chunk of chunks) {
        chunksPulled += 1;
        yield textEvent(chunk);
      }
      yield done(1);
    })();
    scenario.drive = async (model) => drain(model.getStreamedResponse({}));

    const response = await streamedPost(runRequest("run/stream"));
    await tick();
    await tick();
    expect(chunksPulled).toBeLessThan(chunks.length / 2);

    const received = await frames(response);
    expect(answerText(received)).toBe("a".repeat(2000));
  });

  it("rejects a malformed declaration, or one without a result schema, before any execution", async () => {
    const invalid: RunOptions[] = [
      { answerStream: { messageId: "", fieldPath: [answerField] } },
      { answerStream: { messageId, fieldPath: [] } },
      { answerStream: { messageId, fieldPath: [1] } },
      { answerStream: answerField },
      { result: null },
    ];
    for (const options of invalid) {
      const response = await streamedPost(runRequest("run/stream", options));
      expect(response.status, JSON.stringify(options)).toBe(400);
    }
  });

  it("ignores the declaration on the buffered route", async () => {
    scriptRound(...chunked(answerJson("hello"), 4));

    const response = await bufferedPost(runRequest("run"));

    expect(response.headers.get("content-type")).not.toContain("ndjson");
    expect(await response.json()).toMatchObject({ isSuccess: true });
  });
});
