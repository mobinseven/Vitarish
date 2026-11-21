import type { IntelligenceError } from "../generated/intelligence/model";
import type { RunAgentConversationResponse } from "../generated/intelligence/model";

/**
 * Wire values of the Intelligence contract enums the streamed agent turn uses. The OpenAPI document publishes
 * these enums as plain integers, so the ordinals are mirrored here from `StreamChunkKind` and
 * `IntelligenceErrorCode` in the Intelligence contracts, which stay their authority.
 */
export const StreamChunkKindValue = {
  Started: 0,
  Progress: 1,
  Delta: 2,
  Completed: 3,
  Failed: 4,
  Canceled: 5,
} as const;

export const IntelligenceErrorCodeValue = {
  StreamingNotEnabled: 16,
  ConversationNotFound: 17,
} as const;

/** One validated frame of a streamed agent turn. */
export type AgentTurnFrame =
  | { kind: "started" }
  | { kind: "progress"; stage: number; ordinal: number | null }
  | { kind: "delta"; messageId: string; text: string }
  | { kind: "completed"; response: RunAgentConversationResponse }
  | { kind: "failed"; error: IntelligenceError }
  | { kind: "canceled" };

export type AgentTurnStreamViolation =
  | "malformed"
  | "out-of-order"
  | "duplicate-terminal"
  | "ended-without-terminal"
  | "line-too-long";

/** The stream broke its own contract after the server accepted the turn; what the turn did is not known. */
export class AgentTurnStreamError extends Error {
  readonly violation: AgentTurnStreamViolation;

  constructor(violation: AgentTurnStreamViolation, options?: ErrorOptions) {
    super(`The agent turn stream is invalid: ${violation}`, options);
    this.name = "AgentTurnStreamError";
    this.violation = violation;
  }
}

/** A frame carries the whole terminal result, so a line is bounded generously rather than unboundedly. */
const maxLineCharacters = 4 * 1024 * 1024;

const terminalKinds: ReadonlySet<number> = new Set([
  StreamChunkKindValue.Completed,
  StreamChunkKindValue.Failed,
  StreamChunkKindValue.Canceled,
]);

type JsonObject = { [key: string]: unknown };

const isRecord = (value: unknown): value is JsonObject =>
  typeof value === "object" && value !== null && !Array.isArray(value);

/** Integers arrive as numbers or, for large values, as decimal strings. */
function toInteger(value: unknown): number | null {
  if (typeof value === "number") return Number.isSafeInteger(value) ? value : null;
  if (typeof value === "string" && /^-?(?:0|[1-9]\d*)$/.test(value)) {
    const parsed = Number(value);
    return Number.isSafeInteger(parsed) ? parsed : null;
  }
  return null;
}

const malformed = () => new AgentTurnStreamError("malformed");

function toFrame(kind: number, delta: unknown, error: unknown): AgentTurnFrame {
  const hasDelta = delta !== null && delta !== undefined;
  const hasError = error !== null && error !== undefined;
  switch (kind) {
    case StreamChunkKindValue.Started:
    case StreamChunkKindValue.Canceled:
      if (hasDelta || hasError) throw malformed();
      return { kind: kind === StreamChunkKindValue.Started ? "started" : "canceled" };
    case StreamChunkKindValue.Progress: {
      if (hasError || !isRecord(delta)) throw malformed();
      const stage = toInteger(delta.stage);
      const ordinal = delta.ordinal === null || delta.ordinal === undefined ? null : toInteger(delta.ordinal);
      if (stage === null || (delta.ordinal != null && ordinal === null)) throw malformed();
      return { kind: "progress", stage, ordinal };
    }
    case StreamChunkKindValue.Delta: {
      if (hasError || !isRecord(delta)) throw malformed();
      if (typeof delta.messageId !== "string" || delta.messageId.length === 0 || typeof delta.text !== "string") throw malformed();
      return { kind: "delta", messageId: delta.messageId, text: delta.text };
    }
    case StreamChunkKindValue.Completed: {
      if (hasError || !isRecord(delta) || !isRecord(delta.result)) throw malformed();
      const response = delta.result;
      if (
        typeof response.agentRunId !== "string" ||
        typeof response.conversationId !== "string" ||
        response.conversationId.length === 0 ||
        !isRecord(response.result) ||
        (response.blocks != null && !Array.isArray(response.blocks))
      ) throw malformed();
      return { kind: "completed", response: response as unknown as RunAgentConversationResponse };
    }
    case StreamChunkKindValue.Failed: {
      if (hasDelta || !isRecord(error)) throw malformed();
      if (toInteger(error.code) === null || typeof error.message !== "string") throw malformed();
      return { kind: "failed", error: error as unknown as IntelligenceError };
    }
    default:
      throw malformed();
  }
}

/**
 * Reads the NDJSON frames of one streamed agent turn and yields each frame once it is validated: a known shape,
 * one operation, sequence numbers that start at zero and rise by one, `Started` first, and exactly one terminal
 * frame (`Completed`, `Failed` or `Canceled`) that nothing follows. Any violation, a read failure, or an end of
 * stream before the terminal frame throws `AgentTurnStreamError` or the read error; the consumer must treat the
 * turn's outcome as unknown. UTF-8 sequences split across reads are reassembled before parsing.
 */
export async function* readAgentTurnFrames(body: ReadableStream<Uint8Array>): AsyncGenerator<AgentTurnFrame, void, void> {
  const reader = body.getReader();
  const decoder = new TextDecoder("utf-8", { fatal: true });
  let pending = "";
  let operationId: string | null = null;
  let expectedSequence = 0;
  let terminalSeen = false;

  const accept = (line: string): AgentTurnFrame | null => {
    const text = line.endsWith("\r") ? line.slice(0, -1) : line;
    if (text.trim().length === 0) return null;
    if (terminalSeen) throw new AgentTurnStreamError("duplicate-terminal");
    let parsed: unknown;
    try {
      parsed = JSON.parse(text);
    } catch (error) {
      throw new AgentTurnStreamError("malformed", { cause: error });
    }
    if (!isRecord(parsed) || !isRecord(parsed.metadata)) throw malformed();
    const kind = toInteger(parsed.kind);
    const sequence = toInteger(parsed.metadata.sequence);
    const id = parsed.metadata.operationId;
    if (kind === null || sequence === null || typeof id !== "string" || id.length === 0 || typeof parsed.metadata.timestamp !== "string") {
      throw malformed();
    }
    if (operationId !== null && id !== operationId) throw new AgentTurnStreamError("out-of-order");
    if (sequence !== expectedSequence) throw new AgentTurnStreamError("out-of-order");
    if ((sequence === 0) !== (kind === StreamChunkKindValue.Started)) throw new AgentTurnStreamError("out-of-order");
    const frame = toFrame(kind, parsed.delta, parsed.error);
    operationId = id;
    expectedSequence += 1;
    terminalSeen = terminalKinds.has(kind);
    return frame;
  };

  try {
    for (;;) {
      let chunk: ReadableStreamReadResult<Uint8Array>;
      try {
        chunk = await reader.read();
      } catch (error) {
        throw error instanceof AgentTurnStreamError ? error : new AgentTurnStreamError("ended-without-terminal", { cause: error });
      }
      try {
        pending += chunk.done ? decoder.decode() : decoder.decode(chunk.value, { stream: true });
      } catch (error) {
        throw new AgentTurnStreamError("malformed", { cause: error });
      }
      let newline = pending.indexOf("\n");
      while (newline >= 0) {
        const frame = accept(pending.slice(0, newline));
        pending = pending.slice(newline + 1);
        if (frame) yield frame;
        newline = pending.indexOf("\n");
      }
      if (pending.length > maxLineCharacters) throw new AgentTurnStreamError("line-too-long");
      if (chunk.done) break;
    }
    // A final frame whose line feed never arrived still counts, and still has to be valid.
    const last = accept(pending);
    if (last) yield last;
    if (!terminalSeen) throw new AgentTurnStreamError("ended-without-terminal");
  } finally {
    // Stops the transfer when the consumer leaves early, for example once the terminal frame is handled.
    await reader.cancel().catch(() => undefined);
  }
}
