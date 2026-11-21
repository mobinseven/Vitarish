/**
 * Internal frame contract of the streamed agent run (Main consumes it): newline-delimited JSON, one frame per
 * line, flushed per frame. Frames never carry tool names, arguments, results, reasoning or answer text.
 */
import type { AgentRunResponseBody } from "./agent-run";

export const AGENT_RUN_STREAM_CONTENT_TYPE = "application/x-ndjson";

export const AgentRunFrameKind = { Stage: "stage", Result: "result" } as const;

export const AgentRunStage = {
  ModelRoundStarted: "ModelRoundStarted",
  ToolStarted: "ToolStarted",
  ToolCompleted: "ToolCompleted",
} as const;
export type AgentRunStageName = (typeof AgentRunStage)[keyof typeof AgentRunStage];

/** `ordinal` is 1-based: the model round for ModelRoundStarted, the tool call for ToolStarted and ToolCompleted. */
export interface AgentRunStageFrame {
  kind: typeof AgentRunFrameKind.Stage;
  stage: AgentRunStageName;
  ordinal: number;
}

/** Terminal frame: exactly the buffered run response body, for success and for coded failure alike. */
export type AgentRunResultFrame = { kind: typeof AgentRunFrameKind.Result } & AgentRunResponseBody;

export type AgentRunFrame = AgentRunStageFrame | AgentRunResultFrame;

/** Upper bound of one encoded frame line. A result over the bound is replaced by the coded runtime failure. */
export const MAX_AGENT_RUN_FRAME_BYTES = 8 * 1024 * 1024;

/** Frames queued ahead of a slow consumer; the producing run waits once this many are pending. */
export const AGENT_RUN_FRAME_BUFFER_LIMIT = 8;

export function encodeAgentRunFrame(frame: AgentRunFrame): Uint8Array | null {
  const line = new TextEncoder().encode(`${JSON.stringify(frame)}\n`);
  return line.byteLength <= MAX_AGENT_RUN_FRAME_BYTES ? line : null;
}

/** How long the terminal frame may wait for a stalled consumer before the stream is closed without it. */
export const AGENT_RUN_TERMINAL_FRAME_STALL_MS = 30_000;
