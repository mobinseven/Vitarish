import type { ReactNode } from "react";
import { renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../api/client/apiFetch";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";
import { useSurgerySettings } from "../hooks/surgeries/useSurgerySettings";

const hooks = vi.hoisted(() => ({
  useCanPlanSurgeries: vi.fn(),
  clinicGetSurgerySettings: vi.fn(),
  clinicEnsureDefaultSurgerySettings: vi.fn(),
}));

vi.mock("../hooks/surgeries/useCanPlanSurgeries", () => ({
  useCanPlanSurgeries: hooks.useCanPlanSurgeries,
}));
vi.mock("../api/generated/clinic/clinic", () => ({
  clinicGetSurgerySettings: hooks.clinicGetSurgerySettings,
  clinicEnsureDefaultSurgerySettings: hooks.clinicEnsureDefaultSurgerySettings,
}));

const settingsMessages = clinicMessageCatalogs.en.clinic.surgeries.defaultSettings;

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return function wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>
          {children}
        </LocalizationProvider>
      </QueryClientProvider>
    );
  };
}

beforeEach(() => {
  hooks.useCanPlanSurgeries.mockReset();
  hooks.clinicGetSurgerySettings.mockReset();
  hooks.clinicEnsureDefaultSurgerySettings.mockReset();
  hooks.clinicGetSurgerySettings.mockRejectedValue(
    new ApiError({ status: 404 }),
  );
  hooks.clinicEnsureDefaultSurgerySettings.mockResolvedValue({
    cancellationSubject: settingsMessages.cancellationSubject,
    cancellationTextBody: settingsMessages.cancellationText,
    concurrencyToken: "settings-token",
  });
});

describe("useSurgerySettings", () => {
  it("seeds the localized default once for an owner when the server has none", async () => {
    hooks.useCanPlanSurgeries.mockReturnValue(true);

    renderHook(() => useSurgerySettings(), { wrapper: createWrapper() });

    await waitFor(() =>
      expect(hooks.clinicEnsureDefaultSurgerySettings).toHaveBeenCalledTimes(1),
    );
    expect(hooks.clinicEnsureDefaultSurgerySettings).toHaveBeenCalledWith({
      cancellationSubject: settingsMessages.cancellationSubject,
      cancellationTextBody: settingsMessages.cancellationText,
    });

    await waitFor(() =>
      expect(hooks.clinicGetSurgerySettings.mock.calls.length).toBeGreaterThan(
        1,
      ),
    );
    expect(hooks.clinicEnsureDefaultSurgerySettings).toHaveBeenCalledTimes(1);
  });

  it("never seeds for a viewer who cannot plan surgeries", async () => {
    hooks.useCanPlanSurgeries.mockReturnValue(false);

    const { result } = renderHook(() => useSurgerySettings(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.error).toBeTruthy());
    expect(hooks.clinicEnsureDefaultSurgerySettings).not.toHaveBeenCalled();
  });
});
