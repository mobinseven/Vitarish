import type { ReactNode } from "react";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";
import type { Surgery } from "../models/surgeries/surgeryModels";

const hooks = vi.hoisted(() => ({
  usePatientSurgeries: vi.fn(),
}));

vi.mock("../hooks/surgeries/usePatientSurgeries", () => ({
  usePatientSurgeries: hooks.usePatientSurgeries,
}));

vi.mock("../hooks/surgeries/useCanPlanSurgeries", () => ({
  useCanPlanSurgeries: () => true,
}));

import { PatientSurgeriesPanel } from "../components/surgeries/PatientSurgeriesPanel";

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
    catalogId: "type-1",
    name: "Knee arthroscopy",
    status: "Planned",
    scheduledAt: "2026-07-16T08:00:00.000Z",
    team: [],
    tools: [],
    reminderPolicies: [],
    concurrencyToken: "token-1",
    createdAt: "2026-07-01T08:00:00.000Z",
    ...overrides,
  };
}

afterEach(() => {
  cleanup();
  hooks.usePatientSurgeries.mockReset();
});

describe("PatientSurgeriesPanel", () => {
  it("lists the patient's surgeries with their status labels", () => {
    hooks.usePatientSurgeries.mockReturnValue({
      data: [
        makeSurgery({
          id: "surgery-2",
          name: "Follow-up surgery",
          status: "Completed",
        }),
        makeSurgery(),
      ],
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });

    render(
      <PatientSurgeriesPanel patientId="patient-1" onNavigate={vi.fn()} />,
      { wrapper: Wrapper },
    );

    expect(screen.getByText("Knee arthroscopy")).toBeInTheDocument();
    expect(screen.getByText("Follow-up surgery")).toBeInTheDocument();
    expect(screen.getByText("Planned")).toBeInTheDocument();
    expect(screen.getByText("Completed")).toBeInTheDocument();
  });

  it("requests a new surgery for the patient", () => {
    hooks.usePatientSurgeries.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });
    const onNavigate = vi.fn();

    render(
      <PatientSurgeriesPanel patientId="patient-1" onNavigate={onNavigate} />,
      { wrapper: Wrapper },
    );
    fireEvent.click(screen.getByRole("button", { name: "Plan surgery" }));

    expect(onNavigate).toHaveBeenCalledWith({
      kind: "surgeryNew",
      patientId: "patient-1",
    });
  });

  it("offers a retry when the surgeries fail to load", () => {
    const refetch = vi.fn();
    hooks.usePatientSurgeries.mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error("boom"),
      refetch,
    });

    render(
      <PatientSurgeriesPanel patientId="patient-1" onNavigate={vi.fn()} />,
      { wrapper: Wrapper },
    );
    fireEvent.click(screen.getByRole("button", { name: "Try again" }));

    expect(refetch).toHaveBeenCalled();
  });
});
