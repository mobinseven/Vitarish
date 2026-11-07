import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { getJourneyGetCurrentQueryKey } from "../api/generated/journey/journey";
import { signInReturnUrlQueryKey } from "../signInHost";

const apiFetchMock = vi.hoisted(() => vi.fn());

vi.mock("@vitarish/api-client", () => ({ apiFetch: apiFetchMock }));

import { useCurrentJourney } from "../hooks/useCurrentJourney";

afterEach(() => vi.clearAllMocks());

describe("useCurrentJourney", () => {
  it("sends its return path using the sign-in host's owned query key", async () => {
    apiFetchMock.mockResolvedValue({});
    const client = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <QueryClientProvider client={client}>{children}</QueryClientProvider>
    );

    renderHook(() => useCurrentJourney("/studio"), { wrapper });

    const query = new URLSearchParams({
      [signInReturnUrlQueryKey]: "/studio",
    }).toString();
    await waitFor(() => {
      expect(apiFetchMock).toHaveBeenCalledWith(
        `${getJourneyGetCurrentQueryKey()[0]}?${query}`,
      );
    });
  });
});
