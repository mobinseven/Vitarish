import type { ReactNode } from "react";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";
import type { Surgery } from "../models/surgeries/surgeryModels";

const hooks = vi.hoisted(() => ({
  useSurgery: vi.fn(),
  useSurgeryMutations: vi.fn(),
}));

vi.mock("../hooks/surgeries/useSurgery", () => ({
  useSurgery: hooks.useSurgery,
}));

vi.mock("../hooks/surgeries/useSurgeryTypes", () => ({
  useSurgeryTypes: () => ({
    types: [],
    isLoading: false,
    error: null,
    refetch: vi.fn(),
  }),
}));

vi.mock("../hooks/surgeries/useSurgeryMutations", () => ({
  isSurgeryConflictError: () => false,
  useSurgeryMutations: hooks.useSurgeryMutations,
}));

vi.mock("../hooks/surgeries/useCanPlanSurgeries", () => ({
  useCanPlanSurgeries: () => true,
}));

import { SurgeryDetailPage } from "../components/surgeries/SurgeryDetailPage";

const messages = {
  ...clinicMessageCatalogs.en,
  common: { actions: { cancel: "Cancel" } },
};

function message(key: string): string {
  const value = key
    .split(".")
    .reduce<unknown>(
      (current, part) =>
        current && typeof current === "object"
          ? (current as Record<string, unknown>)[part]
          : undefined,
      messages,
    );
  return typeof value === "string" ? value : key;
}

function Wrapper({ children }: { children: ReactNode }) {
  return (
    <LocalizationProvider locale="en" messages={messages}>
      {children}
    </LocalizationProvider>
  );
}

function makeSurgery(overrides: Partial<Surgery> = {}): Surgery {
  return {
    id: "surgery-1",
    patientId: "patient-1",
    patientDisplayName: "Alice",
    catalogId: "type-1",
    name: "Knee arthroscopy",
    status: "Planned",
    scheduledAt: new Date().toISOString(),
    team: [],
    tools: [],
    personsToNotify: [],
    contacts: [],
    reminderPolicies: [],
    concurrencyToken: "token-1",
    createdAt: new Date().toISOString(),
    ...overrides,
  };
}

function mockMutations() {
  const complete = { mutate: vi.fn(), isPending: false };
  const cancel = { mutate: vi.fn(), isPending: false };
  hooks.useSurgeryMutations.mockReturnValue({
    create: { mutate: vi.fn() },
    update: { mutate: vi.fn() },
    complete,
    cancel,
  });
  return { complete, cancel };
}

beforeEach(() => {
  hooks.useSurgery.mockReset();
  hooks.useSurgeryMutations.mockReset();
});

afterEach(cleanup);

describe("SurgeryDetailPage", () => {
  it("completes a planned surgery whose start has passed", () => {
    const surgery = makeSurgery({
      scheduledAt: new Date(Date.now() - 60 * 60 * 1000).toISOString(),
    });
    hooks.useSurgery.mockReturnValue({
      data: surgery,
      isPending: false,
      isError: false,
      error: null,
      refetch: vi.fn(),
    });
    const mutations = mockMutations();

    render(<SurgeryDetailPage surgeryId={surgery.id} onNavigate={vi.fn()} />, {
      wrapper: Wrapper,
    });

    const completeButton = screen.getByRole("button", {
      name: message("clinic.surgeries.actions.complete"),
    });
    expect(completeButton).toBeEnabled();

    fireEvent.click(completeButton);
    fireEvent.click(
      screen.getByRole("button", {
        name: message("clinic.surgeries.detail.dialogs.completeConfirm"),
      }),
    );

    expect(mutations.complete.mutate).toHaveBeenCalledWith(
      surgery,
      expect.anything(),
    );
  });

  it("keeps completion disabled for a planned surgery in the future", () => {
    const surgery = makeSurgery({
      scheduledAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    });
    hooks.useSurgery.mockReturnValue({
      data: surgery,
      isPending: false,
      isError: false,
      error: null,
      refetch: vi.fn(),
    });
    mockMutations();

    render(<SurgeryDetailPage surgeryId={surgery.id} onNavigate={vi.fn()} />, {
      wrapper: Wrapper,
    });

    expect(
      screen.getByRole("button", {
        name: message("clinic.surgeries.actions.complete"),
      }),
    ).toBeDisabled();
  });

  it("offers no edit, completion, or cancellation for a cancelled surgery", () => {
    const surgery = makeSurgery({ status: "Cancelled" });
    hooks.useSurgery.mockReturnValue({
      data: surgery,
      isPending: false,
      isError: false,
      error: null,
      refetch: vi.fn(),
    });
    mockMutations();

    render(<SurgeryDetailPage surgeryId={surgery.id} onNavigate={vi.fn()} />, {
      wrapper: Wrapper,
    });

    expect(
      screen.queryByRole("button", {
        name: message("clinic.surgeries.actions.edit"),
      }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", {
        name: message("clinic.surgeries.actions.complete"),
      }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", {
        name: message("clinic.surgeries.actions.cancel"),
      }),
    ).not.toBeInTheDocument();
  });
});
