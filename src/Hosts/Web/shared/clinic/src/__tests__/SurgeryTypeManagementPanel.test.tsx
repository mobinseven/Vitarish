import type { ReactNode } from "react";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";
import { SurgeryTypeManagementPanel } from "../components/surgeries/SurgeryTypeManagementPanel";

const hooks = vi.hoisted(() => ({
  useSurgeryTypes: vi.fn(),
  useSurgeryTypeMutations: vi.fn(),
  useSurgerySuggestions: vi.fn(),
  useCanPlanSurgeries: vi.fn(),
  useSurgerySettings: vi.fn(),
  useSurgerySettingsMutation: vi.fn(),
}));

vi.mock("../hooks/surgeries/useSurgeryTypes", () => ({
  useSurgeryTypes: hooks.useSurgeryTypes,
}));
vi.mock("../hooks/surgeries/useSurgeryTypeMutations", () => ({
  useSurgeryTypeMutations: hooks.useSurgeryTypeMutations,
}));
vi.mock("../hooks/surgeries/useSurgerySuggestions", () => ({
  useSurgerySuggestions: hooks.useSurgerySuggestions,
}));
vi.mock("../hooks/surgeries/useCanPlanSurgeries", () => ({
  useCanPlanSurgeries: hooks.useCanPlanSurgeries,
}));
vi.mock("../hooks/surgeries/useSurgerySettings", () => ({
  useSurgerySettings: hooks.useSurgerySettings,
}));
vi.mock("../hooks/surgeries/useSurgerySettingsMutation", () => ({
  useSurgerySettingsMutation: hooks.useSurgerySettingsMutation,
}));

function Wrapper({ children }: { children: ReactNode }) {
  return (
    <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>
      {children}
    </LocalizationProvider>
  );
}

afterEach(cleanup);

const surgeryType = {
  id: "type-1",
  name: "General",
  tools: ["Scalpel"],
  defaultTeamRoles: ["Surgeon", "Assistant"],
  reminderPolicies: [],
  concurrencyToken: "token-1",
};

function arrange() {
  const update = { mutate: vi.fn(), isPending: false };
  const create = { mutate: vi.fn(), isPending: false };
  const remove = { mutate: vi.fn(), isPending: false };
  const settingsUpdate = { mutate: vi.fn(), isPending: false };
  hooks.useSurgeryTypeMutations.mockReturnValue({ create, update, remove });
  hooks.useSurgeryTypes.mockReturnValue({
    types: [surgeryType],
    isLoading: false,
    error: null,
    refetch: vi.fn(),
  });
  hooks.useSurgerySuggestions.mockReturnValue({
    data: { locations: [], roles: ["Surgeon", "Assistant"] },
    isLoading: false,
    error: null,
  });
  hooks.useCanPlanSurgeries.mockReturnValue(true);
  hooks.useSurgerySettings.mockReturnValue({
    settings: {
      cancellationSubject: "Surgery cancelled",
      cancellationTextBody: "The clinic will contact you.",
      concurrencyToken: "settings-token",
    },
    isLoading: false,
    error: null,
    refetch: vi.fn(),
  });
  hooks.useSurgerySettingsMutation.mockReturnValue({
    update: settingsUpdate,
  });
  return { update, settingsUpdate };
}

describe("SurgeryTypeManagementPanel", () => {
  it("saves the edited name and team roles", () => {
    const { update } = arrange();
    render(<SurgeryTypeManagementPanel />, { wrapper: Wrapper });

    fireEvent.click(screen.getByRole("button", { name: /General/ }));
    fireEvent.change(screen.getByLabelText("Name"), {
      target: { value: "General surgery" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Remove Assistant" }));
    fireEvent.click(screen.getByRole("button", { name: "Save type" }));

    expect(update.mutate).toHaveBeenCalledTimes(1);
    expect(update.mutate).toHaveBeenCalledWith(
      expect.objectContaining({
        id: "type-1",
        name: "General surgery",
        defaultTeamRoles: ["Surgeon"],
        concurrencyToken: "token-1",
      }),
      expect.anything(),
    );
  });

  it("requires a name before saving", () => {
    const { update } = arrange();
    render(<SurgeryTypeManagementPanel />, { wrapper: Wrapper });

    fireEvent.click(screen.getByRole("button", { name: /General/ }));
    fireEvent.change(screen.getByLabelText("Name"), {
      target: { value: "   " },
    });
    fireEvent.click(screen.getByRole("button", { name: "Save type" }));

    expect(screen.getByText("Enter a type name.")).toBeInTheDocument();
    expect(update.mutate).not.toHaveBeenCalled();
  });

  it("saves the cancellation message with its concurrency token", () => {
    const { settingsUpdate } = arrange();
    render(<SurgeryTypeManagementPanel />, { wrapper: Wrapper });

    fireEvent.change(screen.getByLabelText("Subject"), {
      target: { value: "Appointment called off" },
    });
    fireEvent.change(screen.getByLabelText("Message"), {
      target: { value: "We are sorry to cancel your surgery." },
    });
    fireEvent.click(screen.getByRole("button", { name: "Save message" }));

    expect(settingsUpdate.mutate).toHaveBeenCalledWith(
      expect.objectContaining({
        cancellationSubject: "Appointment called off",
        cancellationTextBody: "We are sorry to cancel your surgery.",
        concurrencyToken: "settings-token",
      }),
      expect.anything(),
    );
  });

  it("hides management from non-owners and explains why", () => {
    const { settingsUpdate } = arrange();
    hooks.useCanPlanSurgeries.mockReturnValue(false);
    render(<SurgeryTypeManagementPanel />, { wrapper: Wrapper });

    expect(
      screen.getByText("Only the clinic owner can plan and manage surgeries."),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "New type" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Save message" }),
    ).not.toBeInTheDocument();
    expect(settingsUpdate.mutate).not.toHaveBeenCalled();
  });
});
