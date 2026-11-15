import { NextRequest } from "next/server";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@openai/agents", () => ({
  Agent: class {},
  tool: (definition: unknown) => definition,
  OpenAIProvider: class {
    getModel() {
      return {
        getResponse: async () => ({
          usage: { inputTokens: 120, outputTokens: 45, totalTokens: 165, outputTokensDetails: [{ reasoning_tokens: 30 }] },
          output: [],
        }),
        getStreamedResponse: () => undefined,
      };
    }
  },
  Runner: class {
    constructor(private readonly options: { modelProvider: { getModel: (name: string) => Promise<{ getResponse: (request: unknown) => Promise<unknown> }> } }) {}
    async run() {
      const model = await this.options.modelProvider.getModel("test-model");
      await model.getResponse({});
      return { finalOutput: "secret clinical reply", history: [], rawResponses: [{}], runContext: { usage: { inputTokens: 120, outputTokens: 45 } } };
    }
  },
}));

import { POST } from "./route";

const sharedSecret = "test-shared-secret";
const practiceId = "11111111-1111-4111-8111-111111111111";

function runRequest(): NextRequest {
  const now = new Date();
  return new NextRequest("http://localhost/api/internal/intelligence-agent/run", {
    method: "POST",
    headers: { authorization: `Bearer ${sharedSecret}`, "content-type": "application/json" },
    body: JSON.stringify({
      definition: { model: "test-model", instructions: "secret clinical instructions" },
      input: { userInput: "secret patient question" },
      tools: [],
      executionEnvelope: {
        version: 1, executionId: "execution-1", agentKey: "agent", tenantId: practiceId, practiceId,
        subjectKind: "practice", subjectId: practiceId, authenticatedUserId: "user", conversationId: "conversation",
        toolCallId: "", toolName: "", credentialVersion: 1, expiresAt: new Date(now.getTime() + 60_000).toISOString(),
        correlationId: "correlation", role: "owner", locale: "en", timeZone: "UTC", calendar: "gregory",
        currentUtc: now.toISOString(), currentLocal: now.toISOString(), permissions: ["read"],
      },
      limits: { maxIterations: 4, maxToolCalls: 2, maxTokens: 10_000, effectiveTimeout: "00:01:00" },
      callbackUrl: "http://localhost/callback",
      callbackCredential: "credential",
    }),
  });
}

describe("agent runtime latency logging", () => {
  beforeEach(() => {
    vi.stubEnv("AGENT_RUNTIME_SHARED_SECRET", sharedSecret);
  });
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.restoreAllMocks();
  });

  it("logs each model call and the run with durations and token counts but no conversation content", async () => {
    const info = vi.spyOn(console, "info").mockImplementation(() => undefined);

    const response = await POST(runRequest());

    expect((await response.json()).isSuccess).toBe(true);
    const modelCall = info.mock.calls.find(([message]) => message === "Agent model call completed")?.[1];
    expect(modelCall).toMatchObject({ executionId: "execution-1", turn: 1, inputTokens: 120, outputTokens: 45, reasoningTokens: 30 });
    expect(modelCall.durationMs).toBeGreaterThanOrEqual(0);
    const run = info.mock.calls.find(([message]) => message === "Agent run completed")?.[1];
    expect(run).toMatchObject({ executionId: "execution-1", modelCalls: 1, toolCalls: 0 });
    expect(JSON.stringify(info.mock.calls)).not.toContain("secret");
  });
});
