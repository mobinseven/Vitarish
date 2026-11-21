import { act, renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { UnknownTurnOutcomeError } from "../api/intelligence/agentConversationTurn";
import { useConversationController } from "../hooks/useConversationController";

describe("useConversationController", () => {
  it("keeps ids internal, preserves them for retry, and resets on owner change", async () => {
    const run = vi
      .fn()
      .mockResolvedValueOnce({
        conversationId: "conversation-1",
        result: { messages: ["first"] },
      })
      .mockResolvedValueOnce({
        conversationId: "conversation-1",
        result: { messages: ["retry"] },
      });
    const deleteConversation = vi.fn().mockResolvedValue(undefined);
    const { result, rerender } = renderHook(
      ({ ownerKey }) =>
        useConversationController({
          ownerKey,
          isOpen: true,
          run,
          deleteConversation,
        }),
      { initialProps: { ownerKey: "patient-1" } },
    );

    await act(() => result.current.submit("first"));
    await act(() => result.current.retry());

    expect(run.mock.calls[0].slice(0, 2)).toEqual(["first", null]);
    expect(run.mock.calls[1].slice(0, 2)).toEqual(["first", "conversation-1"]);
    expect(result.current).not.toHaveProperty("conversationId");

    rerender({ ownerKey: "patient-2" });
    await act(() => result.current.submit("other patient"));
    expect(run.mock.calls.at(-1)?.slice(0, 2)).toEqual(["other patient", null]);
  });

  it("silently starts fresh when a legacy conversation is not found", async () => {
    const notFound = { status: 404 };
    const run = vi
      .fn()
      .mockResolvedValueOnce({ conversationId: "legacy", result: "first" })
      .mockRejectedValueOnce(notFound)
      .mockResolvedValueOnce({ conversationId: "fresh", result: "second" });
    const { result } = renderHook(() =>
      useConversationController({
        ownerKey: "patient-1",
        isOpen: true,
        run,
        deleteConversation: vi.fn(),
        isNotFound: (error) => error === notFound,
      }),
    );

    await act(() => result.current.submit("first"));
    await act(() => result.current.submit("second"));

    expect(run.mock.calls.at(-1)?.slice(0, 2)).toEqual(["second", null]);
    expect(result.current.isError).toBe(false);
  });

  it("deletes the current conversation and resets", async () => {
    const deleteConversation = vi.fn().mockResolvedValue(undefined);
    const run = vi
      .fn()
      .mockResolvedValueOnce({
        conversationId: "conversation-1",
        result: "first",
      })
      .mockResolvedValueOnce({
        conversationId: "conversation-2",
        result: "fresh",
      });
    const { result } = renderHook(() =>
      useConversationController({
        ownerKey: "patient-1",
        isOpen: true,
        run,
        deleteConversation,
      }),
    );

    await act(() => result.current.submit("first"));
    await act(() => result.current.deleteCurrent());
    await act(() => result.current.submit("fresh"));

    expect(deleteConversation).toHaveBeenCalledTimes(1);
    expect(deleteConversation).toHaveBeenCalledWith("conversation-1");
    expect(run.mock.calls.at(-1)?.slice(0, 2)).toEqual(["fresh", null]);
  });

  it("preserves the current conversation when deletion fails", async () => {
    const deleteConversation = vi.fn().mockRejectedValue({ status: 409 });
    const run = vi
      .fn()
      .mockResolvedValue({ conversationId: "conversation-1", result: "first" });
    const { result } = renderHook(() =>
      useConversationController({
        ownerKey: "patient-1",
        isOpen: true,
        run,
        deleteConversation,
      }),
    );

    await act(() => result.current.submit("first"));
    let deletion: Promise<void>;
    act(() => {
      deletion = result.current.deleteCurrent();
    });
    await expect(deletion!).rejects.toEqual({ status: 409 });
    await act(() => result.current.retry());

    expect(run.mock.calls.at(-1)?.slice(0, 2)).toEqual([
      "first",
      "conversation-1",
    ]);
  });

  it("rejects a pending result after the sheet closes", async () => {
    let resolveRun!: (value: {
      conversationId: string;
      result: string;
    }) => void;
    const run = vi.fn(
      () =>
        new Promise<{ conversationId: string; result: string }>((resolve) => {
          resolveRun = resolve;
        }),
    );
    const { result, rerender } = renderHook(
      ({ isOpen }) =>
        useConversationController({
          ownerKey: "patient-1",
          isOpen,
          run,
          deleteConversation: vi.fn(),
        }),
      { initialProps: { isOpen: true } },
    );

    let submitted!: Promise<string | undefined>;
    act(() => {
      submitted = result.current.submit("pending");
    });
    rerender({ isOpen: false });
    await act(async () => {
      resolveRun({ conversationId: "stale", result: "stale" });
      await submitted;
    });

    expect(await submitted).toBeUndefined();
    expect(result.current.isPending).toBe(false);
    expect(result.current.isError).toBe(false);
  });

  it("ignores a concurrent submission while a request is pending", async () => {
    let resolveRun!: (value: {
      conversationId: string;
      result: string;
    }) => void;
    const run = vi.fn(
      () =>
        new Promise<{ conversationId: string; result: string }>((resolve) => {
          resolveRun = resolve;
        }),
    );
    const deleteConversation = vi.fn();
    const { result } = renderHook(() =>
      useConversationController({
        ownerKey: "page:home",
        isOpen: true,
        run,
        deleteConversation,
      }),
    );

    let first!: Promise<string | undefined>;
    let second!: Promise<string | undefined>;
    act(() => {
      first = result.current.submit("first");
      second = result.current.submit("second");
    });
    await expect(second).resolves.toBeUndefined();
    expect(run).toHaveBeenCalledTimes(1);

    await act(async () => {
      resolveRun({ conversationId: "conversation-1", result: "answer" });
      await first;
    });
    expect(await first).toBe("answer");
  });

  it("aborts pending work when the owner unmounts", async () => {
    let capturedSignal: AbortSignal | undefined;
    const run = vi.fn(
      (
        _message: string,
        _conversationId: string | null,
        signal: AbortSignal,
      ) => {
        capturedSignal = signal;
        return new Promise<{ conversationId: string; result: string }>(
          () => undefined,
        );
      },
    );
    const deleteConversation = vi.fn();
    const { result, unmount } = renderHook(
      () =>
        useConversationController({
          ownerKey: "page:about",
          isOpen: true,
          run,
          deleteConversation,
        }),
      { reactStrictMode: false },
    );
    act(() => {
      void result.current.submit("pending");
    });
    expect(capturedSignal?.aborted).toBe(false);
    unmount();
    await waitFor(() => expect(capturedSignal?.aborted).toBe(true));
  });
});

describe("useConversationController continued conversations", () => {
  const answer = (conversationId: string) => ({ conversationId, result: "answer" });

  it("sends an initial conversation with the first run and does not delete it on mount", async () => {
    const run = vi.fn().mockResolvedValue(answer("shared-1"));
    const deleteConversation = vi.fn().mockResolvedValue(undefined);
    const onConversationIdChange = vi.fn();
    const { result } = renderHook(() =>
      useConversationController({
        ownerKey: "practice",
        isOpen: true,
        resetOnClose: false,
        initialConversationId: "shared-1",
        onConversationIdChange,
        run,
        deleteConversation,
      }),
    );

    expect(deleteConversation).not.toHaveBeenCalled();
    expect(onConversationIdChange).not.toHaveBeenCalled();
    await act(() => result.current.submit("continue"));

    expect(run.mock.calls[0].slice(0, 2)).toEqual(["continue", "shared-1"]);
    expect(onConversationIdChange).toHaveBeenCalledWith("shared-1");
    expect(deleteConversation).not.toHaveBeenCalled();
  });

  it("deletes the current conversation and starts fresh when the owner changes after mount", async () => {
    const run = vi
      .fn()
      .mockResolvedValueOnce(answer("fresh-1"))
      .mockResolvedValueOnce(answer("fresh-2"));
    const deleteConversation = vi.fn().mockResolvedValue(undefined);
    const onConversationIdChange = vi.fn();
    const { result, rerender } = renderHook(
      ({ ownerKey }) =>
        useConversationController({
          ownerKey,
          isOpen: true,
          resetOnClose: false,
          initialConversationId: "shared-1",
          onConversationIdChange,
          run,
          deleteConversation,
        }),
      { initialProps: { ownerKey: "practice-a" } },
    );
    expect(deleteConversation).not.toHaveBeenCalled();

    rerender({ ownerKey: "practice-b" });

    expect(deleteConversation).toHaveBeenCalledTimes(1);
    expect(deleteConversation).toHaveBeenCalledWith("shared-1");
    expect(onConversationIdChange).toHaveBeenLastCalledWith(null);
    await act(() => result.current.submit("hello"));
    expect(run.mock.calls[0].slice(0, 2)).toEqual(["hello", null]);
  });

  it("keeps the conversation when the surface closes and reopens with resetOnClose disabled", async () => {
    const run = vi.fn().mockResolvedValue(answer("keep-1"));
    const deleteConversation = vi.fn().mockResolvedValue(undefined);
    const { result, rerender } = renderHook(
      ({ isOpen }) =>
        useConversationController({
          ownerKey: "practice",
          isOpen,
          resetOnClose: false,
          run,
          deleteConversation,
        }),
      { initialProps: { isOpen: true } },
    );
    await act(() => result.current.submit("first"));

    rerender({ isOpen: false });
    rerender({ isOpen: true });
    await act(() => result.current.submit("second"));

    expect(deleteConversation).not.toHaveBeenCalled();
    expect(run.mock.calls[1].slice(0, 2)).toEqual(["second", "keep-1"]);
  });

  it("deletes the conversation when the surface closes by default", async () => {
    const run = vi
      .fn()
      .mockResolvedValueOnce(answer("close-1"))
      .mockResolvedValueOnce(answer("close-2"));
    const deleteConversation = vi.fn().mockResolvedValue(undefined);
    const { result, rerender } = renderHook(
      ({ isOpen }) =>
        useConversationController({
          ownerKey: "patient-1",
          isOpen,
          run,
          deleteConversation,
        }),
      { initialProps: { isOpen: true } },
    );
    await act(() => result.current.submit("first"));

    rerender({ isOpen: false });

    expect(deleteConversation).toHaveBeenCalledTimes(1);
    expect(deleteConversation).toHaveBeenCalledWith("close-1");
    rerender({ isOpen: true });
    await act(() => result.current.submit("again"));
    expect(run.mock.calls[1].slice(0, 2)).toEqual(["again", null]);
  });

  it("stops waiting for a turn without an error and continues the same conversation", async () => {
    const run = vi
      .fn()
      .mockResolvedValueOnce({ conversationId: "conversation-1", result: "first" })
      .mockImplementationOnce((_message: string, _id: string | null, signal: AbortSignal) => new Promise((_resolve, reject) => {
        signal.addEventListener("abort", () => reject(new DOMException("Aborted", "AbortError")));
      }))
      .mockResolvedValueOnce({ conversationId: "conversation-1", result: "third" });
    const deleteConversation = vi.fn();
    const { result } = renderHook(() =>
      useConversationController({ ownerKey: "practice", isOpen: true, run, deleteConversation }),
    );

    await act(() => result.current.submit("first"));
    let stopped: Promise<unknown> = Promise.resolve();
    act(() => { stopped = result.current.submit("second"); });
    await waitFor(() => expect(result.current.isPending).toBe(true));
    act(() => result.current.cancel());
    await act(async () => { expect(await stopped).toBeUndefined(); });

    expect(result.current.isPending).toBe(false);
    expect(result.current.isError).toBe(false);
    expect(deleteConversation).not.toHaveBeenCalled();
    await act(() => result.current.submit("third"));
    expect(run.mock.calls.at(-1)?.slice(0, 2)).toEqual(["third", "conversation-1"]);
  });

  it("treats deleting a conversation that is already gone as done", async () => {
    const notFound = { status: 404 };
    const run = vi
      .fn()
      .mockResolvedValueOnce({ conversationId: "expired", result: "first" })
      .mockResolvedValueOnce({ conversationId: "fresh", result: "second" });
    const deleteConversation = vi.fn().mockRejectedValueOnce(notFound);
    const { result } = renderHook(() =>
      useConversationController({
        ownerKey: "practice",
        isOpen: true,
        run,
        deleteConversation,
        isNotFound: (error) => error === notFound,
      }),
    );

    await act(() => result.current.submit("first"));
    await act(() => result.current.deleteCurrent());
    await act(() => result.current.submit("second"));

    expect(run.mock.calls.at(-1)?.slice(0, 2)).toEqual(["second", null]);
  });

  describe("stale conversation and unknown outcome", () => {
    const ok = (conversationId: string, result = "reply") => ({ conversationId, result });
    const staleSignal = { code: "ConversationNotFound" };
    const controller = (run: ReturnType<typeof vi.fn>, extra: Record<string, unknown> = {}) =>
      renderHook(() =>
        useConversationController({
          ownerKey: "practice",
          isOpen: true,
          run,
          deleteConversation: vi.fn(),
          ...extra,
        }),
      );

    it("starts a new conversation only on the stale-conversation signal", async () => {
      const run = vi.fn()
        .mockResolvedValueOnce(ok("first"))
        .mockRejectedValueOnce(staleSignal)
        .mockResolvedValueOnce(ok("fresh"));
      const { result } = controller(run, { isStaleConversation: (error: unknown) => error === staleSignal });

      await act(() => result.current.submit("one"));
      await act(() => result.current.submit("two"));

      expect(run.mock.calls.map((call) => call[1])).toEqual([null, "first", null]);
      expect(result.current.isError).toBe(false);
    });

    it("never replays after a generic 404 or other failure, and offers retry", async () => {
      const generic = { status: 404 };
      const run = vi.fn()
        .mockResolvedValueOnce(ok("first"))
        .mockRejectedValueOnce(generic)
        .mockResolvedValueOnce(ok("first", "retried"));
      const { result } = controller(run, { isNotFound: () => true, isStaleConversation: (error: unknown) => error === staleSignal });

      await act(() => result.current.submit("one"));
      await act(() => result.current.submit("two"));

      expect(run).toHaveBeenCalledTimes(2);
      expect(result.current.isError).toBe(true);
      expect(result.current.isOutcomeUnknown).toBe(false);
      await act(() => result.current.retry());
      expect(run.mock.calls.at(-1)?.slice(0, 2)).toEqual(["two", "first"]);
    });

    it("reports an unknown outcome without an error, and suppresses retry", async () => {
      const run = vi.fn()
        .mockResolvedValueOnce(ok("first"))
        .mockRejectedValueOnce(new UnknownTurnOutcomeError());
      const { result } = controller(run);

      await act(() => result.current.submit("one"));
      await act(() => result.current.submit("two"));

      expect(result.current.isOutcomeUnknown).toBe(true);
      expect(result.current.isError).toBe(false);
      expect(result.current.isPending).toBe(false);
      await act(() => result.current.retry());
      expect(run).toHaveBeenCalledTimes(2);

      // A new message is a new request, not a replay, and clears the state.
      run.mockResolvedValueOnce(ok("first", "new"));
      await act(() => result.current.submit("three"));
      expect(run).toHaveBeenCalledTimes(3);
      expect(result.current.isOutcomeUnknown).toBe(false);
    });

    it("treats a stopped streamed turn as an unknown outcome", async () => {
      const run = vi.fn((_message: string, _id: string | null, signal: AbortSignal) =>
        new Promise<never>((_resolve, reject) => {
          signal.addEventListener("abort", () => reject(new UnknownTurnOutcomeError()));
        }));
      const { result } = controller(run);

      let pending: Promise<unknown> = Promise.resolve();
      act(() => { pending = result.current.submit("one"); });
      await waitFor(() => expect(result.current.isPending).toBe(true));
      act(() => result.current.cancel());
      await act(() => pending);

      expect(result.current.isOutcomeUnknown).toBe(true);
      await act(() => result.current.retry());
      expect(run).toHaveBeenCalledTimes(1);
    });

    it("shows provisional text only for the running turn and drops it at the terminal response", async () => {
      let finish: (value: { conversationId: string; result: string }) => void = () => undefined;
      let report: (answer: { messageId: string; text: string }) => void = () => undefined;
      const run = vi.fn((_m: string, _id: string | null, _s: AbortSignal, onProvisionalAnswer: typeof report) => {
        report = onProvisionalAnswer;
        return new Promise<{ conversationId: string; result: string }>((resolve) => { finish = resolve; });
      });
      const { result } = controller(run);

      let pending: Promise<unknown> = Promise.resolve();
      act(() => { pending = result.current.submit("one"); });
      await waitFor(() => expect(result.current.isPending).toBe(true));
      expect(result.current.provisional).toBeNull();

      act(() => { report({ messageId: "m1", text: "Hel" }); report({ messageId: "m1", text: "Hello" }); });
      await waitFor(() => expect(result.current.provisional).toMatchObject({ messageId: "m1", text: "Hello" }));
      const turnId = result.current.provisional?.turnId;

      await act(async () => { finish({ conversationId: "c1", result: "Final" }); await pending; });
      expect(result.current.provisional).toBeNull();

      // Late text from the finished turn is ignored.
      act(() => report({ messageId: "m1", text: "late" }));
      await new Promise((resolve) => setTimeout(resolve, 120));
      expect(result.current.provisional).toBeNull();
      expect(turnId).toBeGreaterThan(0);
    });
  });
});
