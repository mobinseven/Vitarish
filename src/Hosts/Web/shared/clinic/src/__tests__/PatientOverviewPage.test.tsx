import type { ReactNode } from "react";
import { cleanup, render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";

vi.mock("../hooks/patients/usePatientDetail", () => ({
  usePatientDetail: () => ({
    data: { id: "patient-1", firstName: "Ada", lastName: "Lovelace" },
    isLoading: false,
    error: null,
  }),
}));
vi.mock("../hooks/patients/procedures/useEnsurePatientVisitsProcedure", () => ({
  patientVisitsProcedureName: "Visits",
  useEnsurePatientVisitsProcedure: () => ({ visitsProcedure: { id: "procedure-1" } }),
}));
vi.mock("../components/patients/prescriptions/PatientPrescriptionSubmissionSheet", () => ({
  PatientPrescriptionSubmissionSheet: ({ isOpen }: { isOpen: boolean }) =>
    isOpen ? <div role="dialog" aria-label="Prescription sheet" /> : null,
}));
vi.mock("../components/patients/procedures/PatientVisitEventDialog", () => ({
  PatientVisitEventDialog: ({ isOpen }: { isOpen: boolean }) =>
    isOpen ? <div role="dialog" aria-label="New visit dialog" /> : null,
}));
vi.mock("../components/patients/prescriptions/PatientPrescriptionDashboard", () => ({
  PatientPrescriptionDashboard: () => null,
  patientPrescriptionHistoryQueryKey: (patientId: string) => ["prescriptions", patientId],
}));
vi.mock("../components/patients/procedures/PatientVisitsPanel", () => ({ PatientVisitsPanel: () => null }));
vi.mock("../components/patients/procedures/PatientProceduresPanel", () => ({ PatientProceduresPanel: () => null }));
vi.mock("../components/surgeries/PatientSurgeriesPanel", () => ({ PatientSurgeriesPanel: () => null }));
vi.mock("../components/patients/overview/PatientReservationsPanel", () => ({ PatientReservationsPanel: () => null }));
vi.mock("../components/patients/overview/PatientProfileCard", () => ({ PatientProfileCard: () => null }));
vi.mock("../components/patients/overview/PatientOverviewHeader", () => ({ PatientOverviewHeader: () => null }));

import { PatientOverviewPage, type PatientQuickAction } from "../components/patients/overview/PatientOverviewPage";

function Wrapper({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={new QueryClient()}>
      <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>
        {children}
      </LocalizationProvider>
    </QueryClientProvider>
  );
}

function renderWithInitialAction(initialAction: PatientQuickAction) {
  const onInitialActionHandled = vi.fn();
  render(
    <PatientOverviewPage
      patientId="patient-1"
      onNavigate={vi.fn()}
      initialAction={initialAction}
      onInitialActionHandled={onInitialActionHandled}
    />,
    { wrapper: Wrapper },
  );
  return onInitialActionHandled;
}

describe("PatientOverviewPage initial action", () => {
  afterEach(cleanup);

  it("opens the prescription sheet on arrival and reports it once", () => {
    const onInitialActionHandled = renderWithInitialAction("prescription");

    expect(screen.getByRole("dialog", { name: "Prescription sheet" })).toBeInTheDocument();
    expect(screen.queryByRole("dialog", { name: "New visit dialog" })).not.toBeInTheDocument();
    expect(onInitialActionHandled).toHaveBeenCalledTimes(1);
  });

  it("opens the new-visit dialog on arrival and reports it once", () => {
    const onInitialActionHandled = renderWithInitialAction("newVisit");

    expect(screen.getByRole("dialog", { name: "New visit dialog" })).toBeInTheDocument();
    expect(screen.queryByRole("dialog", { name: "Prescription sheet" })).not.toBeInTheDocument();
    expect(onInitialActionHandled).toHaveBeenCalledTimes(1);
  });
});
