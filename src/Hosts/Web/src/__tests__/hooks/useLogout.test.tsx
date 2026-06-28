import { renderHook, act, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode } from "react";
import { useLogout } from "@/hooks/auth/useLogout";

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

function wrapper({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe("useLogout", () => {
  beforeEach(() => {
    queryClient.clear();
  });

  it("should logout successfully", async () => {
    const { result } = renderHook(() => useLogout(), { wrapper });

    await act(async () => {
      await result.current.logout();
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.error).toBeNull();
  });
});
