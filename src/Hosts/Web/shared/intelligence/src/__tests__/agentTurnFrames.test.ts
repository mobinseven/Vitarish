import { describe, expect, it } from "vitest";
import {
  AgentTurnStreamError,
  readAgentTurnFrames,
  StreamChunkKindValue,
  type AgentTurnFrame,
} from "../api/intelligence/agentTurnFrames";

const encoder = new TextEncoder();

function frame(kind: number, sequence: number, extra: Record<string, unknown> = {}, operationId = "operation-1") {
  return JSON.stringify({
    kind,
    metadata: { operationId, sequence, timestamp: "2026-11-21T00:00:00Z", stage: 5 },
    delta: null,
    error: null,
    ...extra,
  });
}

const completed = (sequence: number) =>
  frame(StreamChunkKindValue.Completed, sequence, {
    delta: { result: { agentRunId: "run-1", conversationId: "conversation-1", result: { messages: [] } } },
  });

function bodyOf(...chunks: Array<string | Uint8Array>): ReadableStream<Uint8Array> {
  return new ReadableStream({
    start(controller) {
      for (const chunk of chunks) controller.enqueue(typeof chunk === "string" ? encoder.encode(chunk) : chunk);
      controller.close();
    },
  });
}

async function collect(body: ReadableStream<Uint8Array>): Promise<AgentTurnFrame[]> {
  const frames: AgentTurnFrame[] = [];
  for await (const next of readAgentTurnFrames(body)) frames.push(next);
  return frames;
}

describe("readAgentTurnFrames", () => {
  it("reads a started, progress, delta and completed turn", async () => {
    const frames = await collect(bodyOf(
      `${frame(StreamChunkKindValue.Started, 0)}\n`,
      `${frame(StreamChunkKindValue.Progress, 1, { delta: { stage: 0, ordinal: 1 } })}\n`,
      `${frame(StreamChunkKindValue.Delta, 2, { delta: { messageId: "m1", text: "Hello" } })}\n`,
      `${completed(3)}\n`,
    ));

    expect(frames.map(({ kind }) => kind)).toEqual(["started", "progress", "delta", "completed"]);
    expect(frames[2]).toEqual({ kind: "delta", messageId: "m1", text: "Hello" });
    expect(frames[3]).toMatchObject({ response: { conversationId: "conversation-1" } });
  });

  it("reassembles Persian UTF-8 split inside a character and across reads", async () => {
    const line = `${frame(StreamChunkKindValue.Delta, 1, { delta: { messageId: "m1", text: "سلام، نوبت شما ثبت شد" } })}\n`;
    const bytes = encoder.encode(line);
    // Split in the middle of the first two-byte Persian letter and again mid-line.
    const first = bytes.indexOf(0xd8) + 1;
    const frames = await collect(bodyOf(
      `${frame(StreamChunkKindValue.Started, 0)}\n`,
      bytes.slice(0, first),
      bytes.slice(first, first + 9),
      bytes.slice(first + 9),
      `${completed(2)}\n`,
    ));

    expect(frames[1]).toEqual({ kind: "delta", messageId: "m1", text: "سلام، نوبت شما ثبت شد" });
  });

  it("keeps escaped quotes, backslashes and line breaks inside text", async () => {
    const text = 'He said "yes"\\no\nnext';
    const frames = await collect(bodyOf(
      `${frame(StreamChunkKindValue.Started, 0)}\n${frame(StreamChunkKindValue.Delta, 1, { delta: { messageId: "m1", text } })}\n${completed(2)}\n`,
    ));

    expect(frames[1]).toMatchObject({ text });
  });

  it("accepts a final frame without a trailing line feed", async () => {
    const frames = await collect(bodyOf(`${frame(StreamChunkKindValue.Started, 0)}\n${completed(1)}`));
    expect(frames.at(-1)?.kind).toBe("completed");
  });

  it("reads a failed turn's error", async () => {
    const frames = await collect(bodyOf(
      `${frame(StreamChunkKindValue.Started, 0)}\n${frame(StreamChunkKindValue.Failed, 1, { error: { code: 12, message: "failed" } })}\n`,
    ));
    expect(frames.at(-1)).toEqual({ kind: "failed", error: { code: 12, message: "failed" } });
  });

  it.each([
    ["a reordered frame", [frame(0, 0), frame(2, 2, { delta: { messageId: "m", text: "x" } }), frame(2, 1, { delta: { messageId: "m", text: "y" } })], "out-of-order"],
    ["a repeated sequence", [frame(0, 0), frame(2, 0, { delta: { messageId: "m", text: "x" } })], "out-of-order"],
    ["a first frame that is not started", [frame(2, 0, { delta: { messageId: "m", text: "x" } })], "out-of-order"],
    ["a mid-turn operation change", [frame(0, 0), frame(2, 1, { delta: { messageId: "m", text: "x" } }, "operation-2")], "out-of-order"],
    ["a duplicate terminal", [frame(0, 0), completed(1), completed(2)], "duplicate-terminal"],
    ["a frame after a terminal", [frame(0, 0), frame(5, 1), frame(2, 2, { delta: { messageId: "m", text: "x" } })], "duplicate-terminal"],
    ["a non-JSON line", [frame(0, 0), "{not json"], "malformed"],
    ["an unknown kind", [frame(0, 0), frame(9, 1)], "malformed"],
    ["a delta without a message id", [frame(0, 0), frame(2, 1, { delta: { text: "x" } })], "malformed"],
    ["a completed frame without a result", [frame(0, 0), frame(3, 1, { delta: {} })], "malformed"],
    ["a started frame with a payload", [frame(0, 0, { delta: { text: "x" } })], "malformed"],
    ["a failed frame without an error", [frame(0, 0), frame(4, 1)], "malformed"],
    ["a frame that is not an object", [frame(0, 0), "[1]"], "malformed"],
  ])("rejects %s", async (_name, lines, violation) => {
    const read = collect(bodyOf(`${lines.join("\n")}\n`));
    await expect(read).rejects.toBeInstanceOf(AgentTurnStreamError);
    await expect(collect(bodyOf(`${lines.join("\n")}\n`))).rejects.toMatchObject({ violation });
  });

  it("rejects a stream that ends before a terminal frame", async () => {
    await expect(collect(bodyOf(`${frame(0, 0)}\n${frame(2, 1, { delta: { messageId: "m", text: "x" } })}\n`)))
      .rejects.toMatchObject({ violation: "ended-without-terminal" });
    await expect(collect(bodyOf(""))).rejects.toMatchObject({ violation: "ended-without-terminal" });
  });

  it("rejects an unfinished final line", async () => {
    await expect(collect(bodyOf(`${frame(0, 0)}\n${completed(1).slice(0, 40)}`))).rejects.toMatchObject({ violation: "malformed" });
  });

  it("reports a read failure as a stream that ended without a terminal frame", async () => {
    const body = new ReadableStream<Uint8Array>({
      start(controller) {
        controller.enqueue(encoder.encode(`${frame(0, 0)}\n`));
        controller.error(new TypeError("network error"));
      },
    });
    await expect(collect(body)).rejects.toMatchObject({ violation: "ended-without-terminal" });
  });

  it("rejects invalid UTF-8", async () => {
    await expect(collect(bodyOf(`${frame(0, 0)}\n`, new Uint8Array([0xff, 0xfe, 0x0a]))))
      .rejects.toMatchObject({ violation: "malformed" });
  });

  it("stops reading once the consumer leaves after the terminal frame", async () => {
    let cancelled = false;
    const body = new ReadableStream<Uint8Array>({
      start(controller) {
        controller.enqueue(encoder.encode(`${frame(0, 0)}\n${completed(1)}\n`));
      },
      cancel() {
        cancelled = true;
      },
    });
    for await (const next of readAgentTurnFrames(body)) if (next.kind === "completed") break;
    expect(cancelled).toBe(true);
  });
});
