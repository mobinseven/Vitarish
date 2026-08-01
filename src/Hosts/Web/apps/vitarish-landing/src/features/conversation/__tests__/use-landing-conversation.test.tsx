import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useConversationController } from "@vitarish/intelligence-web";
import { useLandingConversation } from "../use-landing-conversation";

vi.mock("@vitarish/intelligence-web", () => ({
  useConversationController: vi.fn(),
}));

const controller = {
  isPending: false,
  isError: false,
  submit: vi.fn(),
  retry: vi.fn(),
  reset: vi.fn(),
  deleteCurrent: vi.fn(),
};

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useConversationController).mockReturnValue(controller);
});

describe("useLandingConversation", () => {
  it("keeps the visible conversation when server deletion fails", async () => {
    controller.submit.mockResolvedValueOnce("A useful answer");
    controller.deleteCurrent.mockRejectedValueOnce(new Error("unavailable"));
    const { result } = renderHook(() => useLandingConversation("about"));

    await act(() => result.current.submit("A question"));
    expect(result.current.messages).toHaveLength(2);

    await act(() => result.current.reset());
    expect(result.current.isError).toBe(true);
    expect(result.current.messages).toHaveLength(2);
  });

  it("clears messages only after server deletion succeeds", async () => {
    controller.submit.mockResolvedValueOnce("A useful answer");
    controller.deleteCurrent.mockResolvedValueOnce(undefined);
    const { result } = renderHook(() => useLandingConversation("contact"));

    await act(() => result.current.submit("A question"));
    await act(() => result.current.reset());

    expect(controller.deleteCurrent).toHaveBeenCalledTimes(1);
    expect(result.current.messages).toEqual([]);
    expect(result.current.isError).toBe(false);
  });

  it("cancels locally during navigation without waiting for deletion", () => {
    const { result } = renderHook(() => useLandingConversation("portfolio"));

    act(() => result.current.cancel());

    expect(controller.reset).toHaveBeenCalledTimes(1);
    expect(result.current.messages).toEqual([]);
  });
});
