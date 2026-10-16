import type { ReactNode } from "react";
import { cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { PatientVisitsPanel } from "../components/patients/procedures/PatientVisitsPanel";
import { patientVisitsProcedureName } from "../hooks/patients/procedures/useEnsurePatientVisitsProcedure";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";

const hooks = vi.hoisted(() => ({
  usePatientProcedures: vi.fn(),
  usePatientProcedureEvents: vi.fn(),
}));

vi.mock("../hooks/patients/procedures/usePatientProcedures", () => ({
  usePatientProcedures: hooks.usePatientProcedures,
}));
vi.mock("../hooks/patients/procedures/usePatientProcedureEvents", () => ({
  usePatientProcedureEvents: hooks.usePatientProcedureEvents,
}));

const procedures = clinicMessageCatalogs.en.clinic.procedures;

function Wrapper({ children }: { children: ReactNode }) {
  return (
    <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>
      {children}
    </LocalizationProvider>
  );
}

afterEach(cleanup);

const visit = (id: string, title: string, createdAt: string, isCommitted: boolean) => ({
  id,
  procedureId: "visits-1",
  projectId: "patient-1",
  title,
  isCommitted,
  attachments: [],
  itemCount: 0,
  position: 0,
  createdAt,
});

describe("PatientVisitsPanel", () => {
  it("lists recorded visits newest first and opens the chosen visit", () => {
    hooks.usePatientProcedures.mockReturnValue({
      data: [{ id: "visits-1", name: patientVisitsProcedureName }],
      isLoading: false,
    });
    hooks.usePatientProcedureEvents.mockReturnValue({
      data: [
        visit("visit-1", "First visit", "2026-02-01T09:00:00Z", true),
        visit("visit-2", "Follow-up", "2026-03-01T09:00:00Z", false),
      ],
    });
    const onNavigate = vi.fn();

    render(<PatientVisitsPanel patientId="patient-1" onNavigate={onNavigate} />, { wrapper: Wrapper });

    const rows = screen.getAllByRole("listitem");
    expect(rows).toHaveLength(2);
    expect(rows[0]).toHaveTextContent("Follow-up");
    expect(rows[1]).toHaveTextContent("First visit");
    expect(within(rows[0]).getByText(procedures.events.pendingState)).toBeVisible();
    expect(within(rows[1]).getByText(procedures.events.committedState)).toBeVisible();

    fireEvent.click(within(rows[1]).getByRole("button", { name: procedures.visits.open }));

    expect(onNavigate).toHaveBeenCalledWith({
      kind: "patientProcedureEventDetail",
      patientId: "patient-1",
      procedureId: "visits-1",
      eventId: "visit-1",
    });
  });
});
