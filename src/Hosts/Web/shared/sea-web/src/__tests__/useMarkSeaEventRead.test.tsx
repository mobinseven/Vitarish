import type { ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, renderHook, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { markSeaEventRead } from "../api/events/seaEventsApi";
import { seaEventKeys } from "../hooks/events/seaEventKeys";
import {
  useMarkRenderedSeaEventsRead,
  useMarkSeaEventRead,
} from "../hooks/events/useMarkSeaEventRead";

vi.mock("../api/events/seaEventsApi", () => ({
  markSeaEventRead: vi.fn(),
}));

const markSeaEventReadMock = vi.mocked(markSeaEventRead);

function createWrapper(queryClient: QueryClient) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );
  };
}

function RenderedEvents({ ids }: { ids: readonly string[] }) {
  useMarkRenderedSeaEventsRead(ids);
  return <div>Event content</div>;
}

describe("event read receipts", () => {
  beforeEach(() => {
    markSeaEventReadMock.mockReset();
    markSeaEventReadMock.mockResolvedValue({});
  });

  it("marks each unique, non-empty rendered ID once across rerenders and refetches", async () => {
    const queryClient = new QueryClient();
    const wrapper = createWrapper(queryClient);
    const { rerender } = renderHook(
      ({ ids }) => useMarkRenderedSeaEventsRead(ids),
      { initialProps: { ids: ["event-1", "", "event-1", "event-2"] }, wrapper },
    );

    await waitFor(() => expect(markSeaEventReadMock).toHaveBeenCalledTimes(2));
    expect(markSeaEventReadMock.mock.calls.map(([id]) => id)).toEqual([
      "event-1",
      "event-2",
    ]);

    rerender({ ids: ["event-2", "event-1", "event-2"] });
    rerender({ ids: ["event-1", "event-2", "event-3"] });

    await waitFor(() => expect(markSeaEventReadMock).toHaveBeenCalledTimes(3));
    expect(markSeaEventReadMock).toHaveBeenLastCalledWith("event-3");
  });

  it("allows a remounted view to send the idempotent receipt again", async () => {
    const queryClient = new QueryClient();
    const wrapper = createWrapper(queryClient);
    const first = renderHook(() => useMarkRenderedSeaEventsRead(["event-1"]), {
      wrapper,
    });
    await waitFor(() => expect(markSeaEventReadMock).toHaveBeenCalledOnce());
    first.unmount();

    renderHook(() => useMarkRenderedSeaEventsRead(["event-1"]), { wrapper });
    await waitFor(() => expect(markSeaEventReadMock).toHaveBeenCalledTimes(2));
  });

  it("invalidates all event queries only after a successful receipt", async () => {
    const queryClient = new QueryClient();
    const invalidate = vi.spyOn(queryClient, "invalidateQueries");
    const { result } = renderHook(() => useMarkSeaEventRead(), {
      wrapper: createWrapper(queryClient),
    });

    result.current.mutate("event-1");

    await waitFor(() =>
      expect(invalidate).toHaveBeenCalledWith({ queryKey: seaEventKeys.all }),
    );
  });

  it("does not invalidate on failure or let receipt state replace rendered content", async () => {
    markSeaEventReadMock.mockRejectedValueOnce(new Error("receipt failed"));
    const queryClient = new QueryClient({
      defaultOptions: { mutations: { retry: false } },
    });
    const invalidate = vi.spyOn(queryClient, "invalidateQueries");

    render(<RenderedEvents ids={["event-1"]} />, {
      wrapper: createWrapper(queryClient),
    });

    expect(screen.getByText("Event content")).toBeInTheDocument();
    await waitFor(() => expect(markSeaEventReadMock).toHaveBeenCalledOnce());
    expect(invalidate).not.toHaveBeenCalled();
    expect(screen.getByText("Event content")).toBeInTheDocument();
  });
});
