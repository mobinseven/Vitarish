import { act, renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
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
});
