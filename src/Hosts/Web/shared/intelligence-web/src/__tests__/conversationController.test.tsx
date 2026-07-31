import { act, renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useConversationController } from "../hooks/useConversationController";

describe("useConversationController", () => {
  it("keeps ids internal, preserves them for retry, and resets on owner change", async () => {
    const run = vi.fn()
      .mockResolvedValueOnce({ conversationId: "conversation-1", result: { messages: ["first"] } })
      .mockResolvedValueOnce({ conversationId: "conversation-1", result: { messages: ["retry"] } });
    const deleteConversation = vi.fn().mockResolvedValue(undefined);
    const { result, rerender } = renderHook(
      ({ ownerKey }) => useConversationController({
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
    const run = vi.fn()
      .mockResolvedValueOnce({ conversationId: "legacy", result: "first" })
      .mockRejectedValueOnce(notFound)
      .mockResolvedValueOnce({ conversationId: "fresh", result: "second" });
    const { result } = renderHook(() => useConversationController({
      ownerKey: "patient-1",
      isOpen: true,
      run,
      deleteConversation: vi.fn(),
      isNotFound: (error) => error === notFound,
    }));

    await act(() => result.current.submit("first"));
    await act(() => result.current.submit("second"));

    expect(run.mock.calls.at(-1)?.slice(0, 2)).toEqual(["second", null]);
    expect(result.current.isError).toBe(false);
  });

  it("deletes the current conversation and resets", async () => {
    const deleteConversation = vi.fn().mockResolvedValue(undefined);
    const run = vi.fn()
      .mockResolvedValueOnce({ conversationId: "conversation-1", result: "first" })
      .mockResolvedValueOnce({ conversationId: "conversation-2", result: "fresh" });
    const { result } = renderHook(() => useConversationController({
      ownerKey: "patient-1",
      isOpen: true,
      run,
      deleteConversation,
    }));

    await act(() => result.current.submit("first"));
    await act(() => result.current.deleteCurrent());
    await act(() => result.current.submit("fresh"));

    expect(deleteConversation).toHaveBeenCalledWith("conversation-1");
    expect(run.mock.calls.at(-1)?.slice(0, 2)).toEqual(["fresh", null]);
  });

  it("preserves the current conversation when deletion fails", async () => {
    const deleteConversation = vi.fn().mockRejectedValue({ status: 409 });
    const run = vi.fn().mockResolvedValue({ conversationId: "conversation-1", result: "first" });
    const { result } = renderHook(() => useConversationController({
      ownerKey: "patient-1",
      isOpen: true,
      run,
      deleteConversation,
    }));

    await act(() => result.current.submit("first"));
    let deletion: Promise<void>;
    act(() => { deletion = result.current.deleteCurrent(); });
    await expect(deletion!).rejects.toEqual({ status: 409 });
    await act(() => result.current.retry());

    expect(run.mock.calls.at(-1)?.slice(0, 2)).toEqual(["first", "conversation-1"]);
  });

  it("rejects a pending result after the sheet closes", async () => {
    let resolveRun!: (value: { conversationId: string; result: string }) => void;
    const run = vi.fn(() => new Promise<{ conversationId: string; result: string }>((resolve) => {
      resolveRun = resolve;
    }));
    const { result, rerender } = renderHook(
      ({ isOpen }) => useConversationController({
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
});
