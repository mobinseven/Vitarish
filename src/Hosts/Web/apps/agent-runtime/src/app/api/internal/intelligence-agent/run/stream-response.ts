import { agentRunFailureBody, executeAgentRun, type AgentRunObserver, type PreparedAgentRun } from "./agent-run";
import {
  AGENT_RUN_FRAME_BUFFER_LIMIT,
  AGENT_RUN_STREAM_CONTENT_TYPE,
  AGENT_RUN_TERMINAL_FRAME_STALL_MS,
  AgentRunFrameKind,
  AgentRunStage,
  encodeAgentRunFrame,
  type AgentRunStageName,
} from "./stream-frames";

const consumerDisconnected = () => new DOMException("The stream consumer disconnected.", "AbortError");

/**
 * Streams one agent run as NDJSON frames. The queue holds at most AGENT_RUN_FRAME_BUFFER_LIMIT frames: the run
 * waits for the consumer before announcing its next stage, and a disconnect (request or consumer cancel) aborts
 * the provider call and tool callbacks through the run signal.
 */
export function streamAgentRun(prepared: PreparedAgentRun, requestSignal: AbortSignal): Response {
  const consumerGone = new AbortController();
  const signal = AbortSignal.any([requestSignal, consumerGone.signal]);
  let queue!: ReadableStreamDefaultController<Uint8Array>;
  let wake: (() => void) | undefined;

  async function write(line: Uint8Array, giveUp: AbortSignal): Promise<void> {
    for (;;) {
      if (consumerGone.signal.aborted) throw consumerDisconnected();
      giveUp.throwIfAborted();
      if ((queue.desiredSize ?? 0) > 0) {
        queue.enqueue(line);
        return;
      }
      await new Promise<void>((resolve) => {
        wake = resolve;
        giveUp.addEventListener("abort", () => resolve(), { once: true });
      });
    }
  }

  const stage = (name: AgentRunStageName, ordinal: number) =>
    write(encodeAgentRunFrame({ kind: AgentRunFrameKind.Stage, stage: name, ordinal })!, signal);
  const observer: AgentRunObserver = {
    modelRoundStarted: (ordinal) => stage(AgentRunStage.ModelRoundStarted, ordinal),
    toolStarted: (ordinal) => stage(AgentRunStage.ToolStarted, ordinal),
    toolCompleted: (ordinal) => stage(AgentRunStage.ToolCompleted, ordinal),
    answerDelta: (messageId, text) => write(encodeAgentRunFrame({ kind: AgentRunFrameKind.Delta, messageId, text })!, signal),
  };

  async function pump(): Promise<void> {
    try {
      const body = await executeAgentRun(prepared, signal, observer);
      const line = encodeAgentRunFrame({ kind: AgentRunFrameKind.Result, ...body })
        ?? encodeAgentRunFrame({ kind: AgentRunFrameKind.Result, ...agentRunFailureBody("ProviderFailure") })!;
      // A disconnected consumer has nobody to receive the terminal frame.
      if (!signal.aborted) await write(line, AbortSignal.timeout(AGENT_RUN_TERMINAL_FRAME_STALL_MS));
    } catch {
      // Consumer gone or stalled: the stream ends without a terminal frame, which the consumer treats as unknown.
    } finally {
      try {
        queue.close();
      } catch {
        // Already cancelled by the consumer.
      }
    }
  }

  const body = new ReadableStream<Uint8Array>(
    {
      start(controller) {
        queue = controller;
        void pump();
      },
      pull() {
        wake?.();
      },
      cancel() {
        consumerGone.abort(consumerDisconnected());
        wake?.();
      },
    },
    { highWaterMark: AGENT_RUN_FRAME_BUFFER_LIMIT, size: () => 1 },
  );

  return new Response(body, {
    headers: {
      "Content-Type": AGENT_RUN_STREAM_CONTENT_TYPE,
      "Cache-Control": "no-store, no-transform",
      "X-Accel-Buffering": "no",
    },
  });
}
