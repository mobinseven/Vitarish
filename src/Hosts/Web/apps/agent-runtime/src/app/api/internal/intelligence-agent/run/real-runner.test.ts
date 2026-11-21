/**
 * Runs the production run path against the real Agents SDK Runner, tool wrapper and result parsing. Only the
 * provider model beneath the runtime's budget wrapper is scripted, so these tests see exactly what the SDK does
 * with usage, tool errors, model resolution per round and structured output.
 */
import { NextRequest } from "next/server";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

interface ScriptedRound {
  output: unknown[];
  /** Raw text chunks of the streamed round; defaults to none. */
  chunks?: string[];
  /** Usage the provider reported; the SDK adapter normalizes an omitted usage to these zeros. */
  totalTokens?: number;
}

const scenario = vi.hoisted(() => ({
  rounds: [] as unknown[],
  calls: 0,
}));

vi.mock("@openai/agents", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@openai/agents")>();
  const scripted = () => {
    const next = () => {
      const round = scenario.rounds[scenario.calls] as ScriptedRound | undefined;
      scenario.calls += 1;
      if (!round) throw new Error("The scripted model has no further round.");
      const total = round.totalTokens ?? 0;
      return { round, usage: { inputTokens: total, outputTokens: 0, totalTokens: total, inputTokensDetails: {}, outputTokensDetails: {} } };
    };
    return {
      getResponse: async () => {
        const { round, usage } = next();
        return { usage: new actual.Usage(usage), output: round.output, responseId: "r" };
      },
      getStreamedResponse: async function* () {
        const { round, usage } = next();
        for (const delta of round.chunks ?? []) yield { type: "output_text_delta", delta };
        yield { type: "response_done", response: { id: "r", usage, output: round.output } };
      },
    };
  };
  return { ...actual, OpenAIProvider: class { getModel() { return scripted(); } } };
});

import { POST as bufferedPost } from "./route";
import { POST as streamedPost } from "./stream/route";
import { agentRunFailureWire, AgentRunFailure } from "./failure-codes";
import { AgentRunFrameKind, AgentRunStage, type AgentRunFrame } from "./stream-frames";

const sharedSecret = "test-shared-secret";
const practiceId = "11111111-1111-4111-8111-111111111111";
const maxTokens = 10_000;
const answerField = "answer";
const toolName = "lookup";
const modelCallLog = "Agent model call completed";
const tokenLimit = agentRunFailureWire[AgentRunFailure.TokenLimitExceeded].code;
const toolLimit = agentRunFailureWire[AgentRunFailure.ToolCallLimitExceeded].code;
const providerFailure = agentRunFailureWire[AgentRunFailure.ProviderFailure].code;

const toolCall = (callId: string) => ({ type: "function_call", callId, name: toolName, arguments: "{}", status: "completed" });
const message = (text: string) => ({ type: "message", role: "assistant", status: "completed", content: [{ type: "output_text", text }] });
const resultSchema = { type: "object", properties: { [answerField]: { type: "string" } }, required: [answerField], additionalProperties: false };

interface RunOptions {
  maxToolCalls?: number;
  withTool?: boolean;
  result?: boolean;
  answerStream?: boolean;
  attempt?: number;
}
function runRequest(path: string, options: RunOptions = {}): NextRequest {
  const now = new Date();
  const tools = options.withTool === false
    ? []
    : [{ name: toolName, description: "lookup", parametersJsonSchema: JSON.stringify({ type: "object", properties: {}, additionalProperties: false }) }];
  return new NextRequest(`http://localhost/api/internal/intelligence-agent/${path}`, {
    method: "POST",
    headers: { authorization: `Bearer ${sharedSecret}`, "content-type": "application/json" },
    body: JSON.stringify({
      definition: {
        model: "test-model",
        instructions: "instructions",
        result: options.result ? { name: "reply", strict: true, jsonSchema: JSON.stringify(resultSchema) } : undefined,
      },
      input: { userInput: "question" },
      tools,
      answerStream: options.answerStream ? { messageId: "answer-1", fieldPath: [answerField] } : undefined,
      attempt: options.attempt,
      executionEnvelope: {
        version: 1, executionId: "execution-1", agentKey: "agent", tenantId: practiceId, practiceId,
        subjectKind: "practice", subjectId: practiceId, authenticatedUserId: "user", conversationId: "conversation",
        toolCallId: "", toolName: "", credentialVersion: 1, expiresAt: new Date(now.getTime() + 60_000).toISOString(),
        correlationId: "correlation", role: "owner", locale: "en", timeZone: "UTC", calendar: "gregory",
        currentUtc: now.toISOString(), currentLocal: now.toISOString(), permissions: ["read"],
      },
      limits: { maxIterations: 6, maxToolCalls: options.maxToolCalls ?? 2, maxTokens, effectiveTimeout: "00:01:00" },
      callbackUrl: "http://localhost/callback",
      callbackCredential: "credential",
    }),
  });
}

type Transport = "buffered" | "streamed";
const transports: Transport[] = ["buffered", "streamed"];

interface Outcome {
  isSuccess: boolean;
  code?: number;
  stages: Array<{ stage: string; ordinal: number }>;
}
async function run(transport: Transport, options: RunOptions = {}): Promise<Outcome> {
  if (transport === "buffered") {
    const body = await (await bufferedPost(runRequest("run", options))).json();
    return { isSuccess: body.isSuccess, code: body.error?.code, stages: [] };
  }
  const text = await (await streamedPost(runRequest("run/stream", options))).text();
  const frames = text.split("\n").filter((line) => line.length > 0).map((line) => JSON.parse(line) as AgentRunFrame);
  const result = frames[frames.length - 1] as { kind: string; isSuccess: boolean; error?: { code: number } };
  expect(result.kind).toBe(AgentRunFrameKind.Result);
  const stages = frames.filter((frame) => frame.kind === AgentRunFrameKind.Stage) as Array<{ stage: string; ordinal: number }>;
  return { isSuccess: result.isSuccess, code: result.error?.code, stages };
}

describe("agent runtime against the real Agents SDK Runner", () => {
  let callbacks: number;
  let callbackResponse: () => Response;
  let modelCalls: () => Array<Record<string, unknown>>;
  beforeEach(() => {
    vi.stubEnv("AGENT_RUNTIME_SHARED_SECRET", sharedSecret);
    const info = vi.spyOn(console, "info").mockImplementation(() => undefined);
    vi.spyOn(console, "error").mockImplementation(() => undefined);
    modelCalls = () => info.mock.calls.filter(([entry]) => entry === modelCallLog).map(([, fields]) => fields as Record<string, unknown>);
    scenario.calls = 0;
    scenario.rounds = [];
    callbacks = 0;
    callbackResponse = () => Response.json({ callId: "call", output: "tool-output", isError: false });
    vi.stubGlobal("fetch", vi.fn(async () => {
      callbacks += 1;
      return callbackResponse();
    }));
  });
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  describe.each(transports)("%s", (transport) => {
    it("fails with the token-limit code, before any tool or second model call, when the provider reports no usage", async () => {
      scenario.rounds = [{ output: [toolCall("c1")] }, { output: [message("done")] }];

      const outcome = await run(transport);

      expect(outcome).toMatchObject({ isSuccess: false, code: tokenLimit });
      expect(callbacks).toBe(0);
      expect(scenario.calls).toBe(1);
    });

    it("still accepts a run whose provider reports usage", async () => {
      scenario.rounds = [{ output: [toolCall("c1")], totalTokens: 10 }, { output: [message("done")], totalTokens: 10 }];

      expect(await run(transport)).toMatchObject({ isSuccess: true });
      expect(callbacks).toBe(1);
      expect(scenario.calls).toBe(2);
    });

    it("ends the run with the tool-call-limit code when the SDK would turn the limit error into a tool result", async () => {
      scenario.rounds = [
        { output: [toolCall("c1"), toolCall("c2")], totalTokens: 10 },
        { output: [message("done")], totalTokens: 10 },
      ];

      const outcome = await run(transport, { maxToolCalls: 1 });

      expect(outcome).toMatchObject({ isSuccess: false, code: toolLimit });
      // The SDK runs the calls concurrently: the call that took the single slot still reaches its callback.
      await vi.waitFor(() => expect(callbacks).toBe(1));
      expect(scenario.calls).toBe(1);
    });

    it("keeps an ordinary tool failure recoverable by the model", async () => {
      callbackResponse = () => new Response("boom", { status: 500 });
      scenario.rounds = [{ output: [toolCall("c1")], totalTokens: 10 }, { output: [message("recovered")], totalTokens: 10 }];

      expect(await run(transport)).toMatchObject({ isSuccess: true });
      expect(scenario.calls).toBe(2);
    });

    it("keeps an unreachable tool callback recoverable by the model", async () => {
      vi.stubGlobal("fetch", vi.fn(async () => {
        callbacks += 1;
        throw new TypeError("fetch failed");
      }));
      scenario.rounds = [{ output: [toolCall("c1")], totalTokens: 10 }, { output: [message("recovered")], totalTokens: 10 }];

      expect(await run(transport)).toMatchObject({ isSuccess: true });
      expect(scenario.calls).toBe(2);
    });

    it("counts model rounds across the whole attempt and reports the attempt ordinal", async () => {
      scenario.rounds = [{ output: [toolCall("c1")], totalTokens: 10 }, { output: [message("done")], totalTokens: 10 }];

      const outcome = await run(transport, { attempt: 2 });

      expect(outcome.isSuccess).toBe(true);
      expect(modelCalls().map((call) => call.turn)).toEqual([1, 2]);
      expect(modelCalls().map((call) => call.attempt)).toEqual([2, 2]);
      if (transport === "streamed") {
        expect(outcome.stages.filter((frame) => frame.stage === AgentRunStage.ModelRoundStarted).map((frame) => frame.ordinal)).toEqual([1, 2]);
      }
    });

    it("reports the first attempt when the request carries none", async () => {
      scenario.rounds = [{ output: [message("done")], totalTokens: 10 }];

      await run(transport);

      expect(modelCalls().map((call) => call.attempt)).toEqual([1]);
    });

    it("fails a duplicate answer key the SDK parse would otherwise erase, however the text is chunked", async () => {
      const duplicate = `{"${answerField}":"first","${answerField}":"second"}`;
      const chunkings = transport === "buffered" ? [[duplicate]] : [[duplicate], [...duplicate]];
      for (const chunks of chunkings) {
        scenario.calls = 0;
        scenario.rounds = [{ output: [message(duplicate)], chunks, totalTokens: 10 }];

        expect(await run(transport, { withTool: false, result: true, answerStream: true })).toMatchObject({ isSuccess: false, code: providerFailure });
      }
    });

    it("accepts a well-formed answer object, including a very long number elsewhere in it", async () => {
      const text = `{"${answerField}":"fine","count":0.${"0".repeat(100)}1}`;
      scenario.rounds = [{ output: [message(text)], chunks: [...text.match(/.{1,9}/g)!], totalTokens: 10 }];

      expect(await run(transport, { withTool: false, result: true, answerStream: true })).toMatchObject({ isSuccess: true });
    });
  });

  it("rejects an invalid attempt ordinal before any execution starts", async () => {
    for (const attempt of [0, -1, 1.5, 100_000]) {
      const response = await bufferedPost(runRequest("run", { attempt }));
      expect(response.status).toBe(400);
    }
    expect(scenario.calls).toBe(0);
  });
});
