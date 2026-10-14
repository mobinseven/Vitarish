import type { ReactNode } from "react";
import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";
import type { Surgery } from "../models/surgeries/surgeryModels";

const hooks = vi.hoisted(() => ({
  useCanPlanSurgeries: vi.fn(),
  useSurgeryCalendar: vi.fn(),
  useClinicStaff: vi.fn(),
  useSurgery: vi.fn(),
}));

vi.mock("../hooks/surgeries/useCanPlanSurgeries", () => ({
  useCanPlanSurgeries: hooks.useCanPlanSurgeries,
}));
vi.mock("../hooks/surgeries/useSurgeryCalendar", () => ({
  useSurgeryCalendar: hooks.useSurgeryCalendar,
}));
vi.mock("../hooks/surgeries/useClinicStaff", () => ({
  useClinicStaff: hooks.useClinicStaff,
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
  useSurgeryMutations: () => ({
    create: { mutate: vi.fn() },
    update: { mutate: vi.fn() },
    complete: { mutate: vi.fn(), isPending: false },
    cancel: { mutate: vi.fn(), isPending: false },
  }),
}));

import { SurgeryCalendarPage } from "../components/surgeries/SurgeryCalendarPage";
import { SurgeryDetailPage } from "../components/surgeries/SurgeryDetailPage";

function message(key: string): string {
  const value = key
    .split(".")
    .reduce<unknown>(
      (current, part) =>
        current && typeof current === "object"
          ? (current as Record<string, unknown>)[part]
          : undefined,
      clinicMessageCatalogs.en,
    );
  return typeof value === "string" ? value : key;
}

function Wrapper({ children }: { children: ReactNode }) {
  return (
    <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>
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
    scheduledAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    team: [],
    tools: [],
    reminderPolicies: [],
    concurrencyToken: "token-1",
    createdAt: new Date().toISOString(),
    ...overrides,
  };
}

beforeEach(() => {
  hooks.useCanPlanSurgeries.mockReturnValue(false);
  hooks.useSurgeryCalendar.mockReturnValue({
    data: [],
    isLoading: false,
    error: null,
    refetch: vi.fn(),
  });
  hooks.useClinicStaff.mockReturnValue({
    data: [],
    isLoading: false,
    error: null,
  });
  hooks.useSurgery.mockReturnValue({
    data: makeSurgery(),
    isPending: false,
    isError: false,
    error: null,
    refetch: vi.fn(),
  });
});

afterEach(() => {
  cleanup();
  hooks.useCanPlanSurgeries.mockReset();
  hooks.useSurgeryCalendar.mockReset();
  hooks.useClinicStaff.mockReset();
  hooks.useSurgery.mockReset();
});

describe("surgery owner gating", () => {
  it("does not offer planning to a non-owner on the calendar page", () => {
    const onNavigate = vi.fn();
    render(<SurgeryCalendarPage onNavigate={onNavigate} />, {
      wrapper: Wrapper,
    });

    expect(
      screen.queryByRole("button", {
        name: message("clinic.surgeries.actions.plan"),
      }),
    ).not.toBeInTheDocument();
  });

  it("does not offer edit or cancellation to a non-owner on the detail page", () => {
    render(<SurgeryDetailPage surgeryId="surgery-1" onNavigate={vi.fn()} />, {
      wrapper: Wrapper,
    });

    expect(
      screen.queryByRole("button", {
        name: message("clinic.surgeries.actions.edit"),
      }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", {
        name: message("clinic.surgeries.actions.cancel"),
      }),
    ).not.toBeInTheDocument();
  });
});
