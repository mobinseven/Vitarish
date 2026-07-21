import type { ReactNode } from "react";
import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { PatientProceduresPanel } from "../components/patients/procedures/PatientProceduresPanel";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";

const hooks = vi.hoisted(() => ({
  usePatientProcedures: vi.fn(),
  usePatientProcedureTriage: vi.fn(),
}));

vi.mock("../hooks/patients/procedures/usePatientProcedures", () => ({ usePatientProcedures: hooks.usePatientProcedures }));
vi.mock("../hooks/patients/procedures/usePatientProcedureTriage", () => ({ usePatientProcedureTriage: hooks.usePatientProcedureTriage }));

function Wrapper({ children }: { children: ReactNode }) {
  return <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>{children}</LocalizationProvider>;
}

describe("PatientProceduresPanel", () => {
  it("displays each procedure as a navigation button", () => {
    hooks.usePatientProcedures.mockReturnValue({
      data: [{ id: "procedure-1", projectId: "patient-1", name: "Skin renewal", description: "Five-step protocol", eventCount: 5, committedEventCount: 4, eventItemCount: 2, newItems: 1, createdAt: "2026-07-20T08:00:00Z" }],
      isLoading: false,
      error: null,
    });
    hooks.usePatientProcedureTriage.mockReturnValue({ data: { totalProcedures: 1, incompleteProcedures: 1, newEvidence: 1 } });

    const onNavigate = vi.fn();
    render(<PatientProceduresPanel patientId="patient-1" onNavigate={onNavigate} />, { wrapper: Wrapper });

    expect(screen.getByRole("button", { name: /Skin renewal/ })).toBeInTheDocument();
    expect(screen.getByText("Five-step protocol")).toBeInTheDocument();
    expect(screen.getByText("4/5 events")).toBeInTheDocument();
  });
});
