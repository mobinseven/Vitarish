import { ApiError } from "@vitarish/api-client";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  AgentTurnCanceledError,
  AgentTurnFailedError,
  isConversationNotFound,
  resetAgentConversationCapabilities,
  runAgentConversationTurn,
  UnknownTurnOutcomeError,
} from "../api/intelligence/agentConversationTurn";
import { IntelligenceErrorCodeValue, StreamChunkKindValue } from "../api/intelligence/agentTurnFrames";
import {
  intelligenceGetAgentConversationCapabilities,
  intelligenceRunAgentConversation,
  intelligenceStreamAgentConversation,
} from "../api/generated/intelligence/intelligence";

vi.mock("../api/generated/intelligence/intelligence", () => ({
  intelligenceGetAgentConversationCapabilities: vi.fn(),
  intelligenceRunAgentConversation: vi.fn(),
  intelligenceStreamAgentConversation: vi.fn(),
}));

const request = { agentKey: "agent", subjectKind: "practice", subjectId: "practice", message: "hi", conversationId: null };
const buffered = { agentRunId: "run", conversationId: "buffered", result: {} };
const encoder = new TextEncoder();

const capabilities = (streamingEnabled: boolean) => ({
  agents: [{ agentKey: "agent", subjectKind: "practice", streamingEnabled }],
});

function frame(kind: number, sequence: number, extra: Record<string, unknown> = {}) {
  return `${JSON.stringify({
    kind,
    metadata: { operationId: "op", sequence, timestamp: "2026-11-21T00:00:00Z", stage: 5 },
    delta: null,
    error: null,
    ...extra,
  })}\n`;
}

const delta = (sequence: number, text: string, messageId = "m1") =>
  frame(StreamChunkKindValue.Delta, sequence, { delta: { messageId, text } });
const completed = (sequence: number) =>
  frame(StreamChunkKindValue.Completed, sequence, {
    delta: { result: { agentRunId: "run", conversationId: "streamed", result: { messages: [] } } },
  });

const streamOf = (...lines: string[]) =>
  new ReadableStream<Uint8Array>({
    start(controller) {
      for (const line of lines) controller.enqueue(encoder.encode(line));
      controller.close();
    },
  });

function streamReturns(...lines: string[]) {
  vi.mocked(intelligenceStreamAgentConversation).mockResolvedValue(streamOf(...lines) as never);
}

describe("runAgentConversationTurn", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    resetAgentConversationCapabilities();
    vi.mocked(intelligenceRunAgentConversation).mockResolvedValue(buffered as never);
  });

  it("stays on the buffered route unless the agent profile enables streaming", async () => {
    vi.mocked(intelligenceGetAgentConversationCapabilities).mockResolvedValue(capabilities(false) as never);

    await expect(runAgentConversationTurn(request)).resolves.toBe(buffered);
    expect(intelligenceStreamAgentConversation).not.toHaveBeenCalled();
  });

  it("stays buffered when the capabilities cannot be read", async () => {
    vi.mocked(intelligenceGetAgentConversationCapabilities).mockRejectedValue(new Error("down"));

    await expect(runAgentConversationTurn(request)).resolves.toBe(buffered);
    expect(intelligenceStreamAgentConversation).not.toHaveBeenCalled();
  });

  describe("when streaming is enabled", () => {
    beforeEach(() => {
      vi.mocked(intelligenceGetAgentConversationCapabilities).mockResolvedValue(capabilities(true) as never);
    });

    it("requests the unread body, reports provisional text per message and resolves with the completed response", async () => {
      streamReturns(
        frame(StreamChunkKindValue.Started, 0),
        delta(1, "Hel"),
        delta(2, "lo"),
        delta(3, "Second", "m2"),
        completed(4),
      );
      const seen: Array<{ messageId: string; text: string }> = [];
      const signal = new AbortController().signal;

      const response = await runAgentConversationTurn(request, { signal, onProvisionalAnswer: (answer) => seen.push(answer) });

      expect(response).toMatchObject({ conversationId: "streamed" });
      expect(seen).toEqual([
        { messageId: "m1", text: "Hel" },
        { messageId: "m1", text: "Hello" },
        { messageId: "m2", text: "Second" },
      ]);
      expect(intelligenceStreamAgentConversation).toHaveBeenCalledWith(request, { responseType: "stream" }, signal);
      expect(intelligenceRunAgentConversation).not.toHaveBeenCalled();
    });

    it("treats a stream that ends without a terminal frame as an unknown outcome", async () => {
      streamReturns(frame(StreamChunkKindValue.Started, 0), delta(1, "partial"));
      await expect(runAgentConversationTurn(request)).rejects.toBeInstanceOf(UnknownTurnOutcomeError);
    });

    it("treats a malformed accepted stream as an unknown outcome", async () => {
      streamReturns(frame(StreamChunkKindValue.Started, 0), "{broken\n");
      await expect(runAgentConversationTurn(request)).rejects.toBeInstanceOf(UnknownTurnOutcomeError);
    });

    it("treats an abort or network failure after sending as an unknown outcome", async () => {
      vi.mocked(intelligenceStreamAgentConversation).mockRejectedValue(new DOMException("aborted", "AbortError"));
      await expect(runAgentConversationTurn(request)).rejects.toBeInstanceOf(UnknownTurnOutcomeError);
    });

    it("keeps an HTTP rejection as the API error, since nothing ran", async () => {
      const rejection = new ApiError({ status: 502, code: "Failure" });
      vi.mocked(intelligenceStreamAgentConversation).mockRejectedValue(rejection);
      await expect(runAgentConversationTurn(request)).rejects.toBe(rejection);
    });

    it("reports a terminal failure and a terminal cancellation as known outcomes", async () => {
      streamReturns(frame(StreamChunkKindValue.Started, 0), frame(StreamChunkKindValue.Failed, 1, { error: { code: 12, message: "no" } }));
      await expect(runAgentConversationTurn(request)).rejects.toBeInstanceOf(AgentTurnFailedError);

      streamReturns(frame(StreamChunkKindValue.Started, 0), frame(StreamChunkKindValue.Canceled, 1));
      await expect(runAgentConversationTurn(request)).rejects.toBeInstanceOf(AgentTurnCanceledError);
    });

    it("falls back to the buffered route when the server rejects streaming before running anything", async () => {
      vi.mocked(intelligenceStreamAgentConversation).mockRejectedValue(
        new ApiError({ status: 409, code: IntelligenceErrorCodeValue.StreamingNotEnabled as unknown as string }),
      );

      await expect(runAgentConversationTurn(request)).resolves.toBe(buffered);
      expect(intelligenceRunAgentConversation).toHaveBeenCalledTimes(1);
    });
  });
});

describe("isConversationNotFound", () => {
  const apiError = (status: number, code: unknown) => new ApiError({ status, code: code as string });

  it("is true only for a 404 that carries the conversation-not-found code", () => {
    expect(isConversationNotFound(apiError(404, IntelligenceErrorCodeValue.ConversationNotFound))).toBe(true);
  });

  it("is false for a generic 404, an unknown route, other statuses and other errors", () => {
    expect(isConversationNotFound(apiError(404, undefined))).toBe(false);
    expect(isConversationNotFound(apiError(404, 13))).toBe(false);
    expect(isConversationNotFound(apiError(409, IntelligenceErrorCodeValue.ConversationNotFound))).toBe(false);
    expect(isConversationNotFound(apiError(502, 12))).toBe(false);
    expect(isConversationNotFound({ status: 404 })).toBe(false);
    expect(isConversationNotFound(new UnknownTurnOutcomeError())).toBe(false);
  });
});
