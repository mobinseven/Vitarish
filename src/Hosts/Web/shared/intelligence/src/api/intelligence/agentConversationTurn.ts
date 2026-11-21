import { ApiError } from "@vitarish/api-client";
import {
  intelligenceGetAgentConversationCapabilities,
  intelligenceRunAgentConversation,
  intelligenceStreamAgentConversation,
} from "../generated/intelligence/intelligence";
import type {
  AgentConversationCapabilitiesResponse,
  IntelligenceError,
  RunAgentConversationRequest,
  RunAgentConversationResponse,
} from "../generated/intelligence/model";
import {
  IntelligenceErrorCodeValue,
  readAgentTurnFrames,
} from "./agentTurnFrames";

/**
 * The turn was accepted and may have run, but the client never received its authoritative outcome: the stream ended
 * without a terminal frame, broke its contract, or was aborted. The answer may or may not be saved, so the turn must
 * not be replayed; the person checks the conversation instead.
 */
export class UnknownTurnOutcomeError extends Error {
  constructor(options?: ErrorOptions) {
    super("The outcome of the agent turn is unknown.", options);
    this.name = "UnknownTurnOutcomeError";
  }
}

/** The server reported, in the terminal frame, that the accepted turn failed. The outcome is known. */
export class AgentTurnFailedError extends Error {
  readonly error: IntelligenceError;

  constructor(error: IntelligenceError) {
    super(error.message);
    this.name = "AgentTurnFailedError";
    this.error = error;
  }
}

/** The server reported, in the terminal frame, that it cancelled the accepted turn (for example at its deadline). */
export class AgentTurnCanceledError extends Error {
  constructor() {
    super("The agent turn was canceled.");
    this.name = "AgentTurnCanceledError";
  }
}

/**
 * True only for the Intelligence signal that the conversation named by a resumed request is missing, expired or not
 * visible to the caller. The server raises it before anything runs, so starting a new conversation is safe. A generic
 * 404, an unknown route and every other failure are not stale-conversation signals.
 */
export function isConversationNotFound(error: unknown): boolean {
  return error instanceof ApiError
    && error.status === 404
    && Number(error.code) === IntelligenceErrorCodeValue.ConversationNotFound;
}

function isStreamingNotEnabled(error: unknown): boolean {
  return error instanceof ApiError
    && error.status === 409
    && Number(error.code) === IntelligenceErrorCodeValue.StreamingNotEnabled;
}

/** What a streamed turn shows before its authoritative answer: the text so far of one assistant message. */
export interface ProvisionalAnswer {
  messageId: string;
  text: string;
}

export interface AgentConversationTurnOptions {
  signal?: AbortSignal;
  /** Called with the accumulated text of the answer message being written; never authoritative. */
  onProvisionalAnswer?: (answer: ProvisionalAnswer) => void;
}

let capabilities: Promise<AgentConversationCapabilitiesResponse> | null = null;

/** Streaming is off unless the agent profile's capability says on; an unreadable capability keeps the buffered route. */
async function isStreamingEnabled(request: RunAgentConversationRequest, signal?: AbortSignal): Promise<boolean> {
  try {
    capabilities ??= intelligenceGetAgentConversationCapabilities(undefined, signal);
    const response = await capabilities;
    return response.agents.some(
      (agent) => agent.agentKey === request.agentKey && agent.subjectKind === request.subjectKind && agent.streamingEnabled,
    );
  } catch {
    capabilities = null;
    return false;
  }
}

/** Forgets the cached capabilities, for a new sign-in or a test. */
export function resetAgentConversationCapabilities(): void {
  capabilities = null;
}

async function runStreamed(
  request: RunAgentConversationRequest,
  { signal, onProvisionalAnswer }: AgentConversationTurnOptions,
): Promise<RunAgentConversationResponse> {
  let body: ReadableStream<Uint8Array>;
  try {
    body = (await intelligenceStreamAgentConversation(request, { responseType: "stream" }, signal)) as unknown as ReadableStream<Uint8Array>;
  } catch (error) {
    // An HTTP rejection carries a status the server chose before it ran anything. Anything else (an abort or a
    // network failure) may have left an accepted turn running.
    if (error instanceof ApiError) throw error;
    throw new UnknownTurnOutcomeError({ cause: error });
  }

  let messageId: string | null = null;
  let text = "";
  try {
    for await (const frame of readAgentTurnFrames(body)) {
      switch (frame.kind) {
        case "delta":
          if (frame.messageId !== messageId) {
            messageId = frame.messageId;
            text = "";
          }
          text += frame.text;
          onProvisionalAnswer?.({ messageId, text });
          break;
        case "completed":
          return frame.response;
        case "failed":
          throw new AgentTurnFailedError(frame.error);
        case "canceled":
          throw new AgentTurnCanceledError();
        default:
          break;
      }
    }
  } catch (error) {
    if (error instanceof AgentTurnFailedError || error instanceof AgentTurnCanceledError) throw error;
    throw new UnknownTurnOutcomeError({ cause: error });
  }
  // The reader never ends normally without a terminal frame; guard the contract regardless.
  throw new UnknownTurnOutcomeError();
}

/**
 * Runs one agent conversation turn on the route the agent profile's capability selects: the streamed route when the
 * profile enables it, the buffered route otherwise. Both resolve with the same authoritative response; streamed
 * provisional text is reported separately and never replaces it.
 */
export async function runAgentConversationTurn(
  request: RunAgentConversationRequest,
  options: AgentConversationTurnOptions = {},
): Promise<RunAgentConversationResponse> {
  if (await isStreamingEnabled(request, options.signal)) {
    try {
      return await runStreamed(request, options);
    } catch (error) {
      // Rejected before anything ran: the capability changed, so use the buffered route for this and later turns.
      if (!isStreamingNotEnabled(error)) throw error;
      resetAgentConversationCapabilities();
    }
  }
  return intelligenceRunAgentConversation(request, undefined, options.signal);
}
