import type { ReactNode } from "react";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { PatientProceduresPanel } from "../components/patients/procedures/PatientProceduresPanel";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";

const hooks = vi.hoisted(() => ({
  usePatientProcedures: vi.fn(),
  usePatientProcedureTriage: vi.fn(),
  usePatientProcedureEvents: vi.fn(),
}));

vi.mock("../hooks/patients/procedures/usePatientProcedures", () => ({
  usePatientProcedures: hooks.usePatientProcedures,
}));
vi.mock("../hooks/patients/procedures/usePatientProcedureTriage", () => ({
  usePatientProcedureTriage: hooks.usePatientProcedureTriage,
}));
vi.mock("../hooks/patients/procedures/usePatientProcedureEvents", () => ({
  usePatientProcedureEvents: hooks.usePatientProcedureEvents,
}));

function Wrapper({ children }: { children: ReactNode }) {
  return (
    <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>
      {children}
    </LocalizationProvider>
  );
}

afterEach(cleanup);

describe("PatientProceduresPanel", () => {
  it("expands the usual single procedure into an at-a-glance workflow", async () => {
    hooks.usePatientProcedures.mockReturnValue({
      data: [
        {
          id: "procedure-1",
          projectId: "patient-1",
          name: "Skin renewal",
          description: "Five-step protocol",
          eventCount: 5,
          committedEventCount: 4,
          eventItemCount: 2,
          newItems: 1,
          lastActivity: "2026-07-24T08:00:00Z",
          createdAt: "2026-07-20T08:00:00Z",
        },
      ],
      isLoading: false,
      error: null,
    });
    hooks.usePatientProcedureTriage.mockReturnValue({
      data: {
        totalProcedures: 1,
        incompleteProcedures: 1,
        newEvidence: 1,
        pendingReminders: 0,
      },
    });
    hooks.usePatientProcedureEvents.mockReturnValue({
      data: [
        {
          id: "event-1",
          procedureId: "procedure-1",
          projectId: "patient-1",
          title: "Initial assessment",
          isCommitted: true,
          attachments: [
            {
              id: "item-1",
              eventId: "event-1",
              type: "note",
              content: "Healing is on track",
              authorName: "Dr. Ali",
              createdAt: "2026-07-24T08:00:00Z",
            },
          ],
          itemCount: 1,
          position: 0,
          createdAt: "2026-07-20T08:00:00Z",
        },
        {
          id: "event-2",
          procedureId: "procedure-1",
          projectId: "patient-1",
          title: "Follow-up",
          isCommitted: false,
          attachments: [],
          itemCount: 0,
          position: 1,
          createdAt: "2026-07-21T08:00:00Z",
        },
      ],
      isLoading: false,
      error: null,
    });

    const onNavigate = vi.fn();
    render(
      <PatientProceduresPanel patientId="patient-1" onNavigate={onNavigate} />,
      { wrapper: Wrapper },
    );

    expect(screen.getByText("Progress at a glance")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Skin renewal/ })).toHaveAttribute(
      "aria-expanded",
      "true",
    );
    expect(screen.getByText("Five-step protocol")).toBeInTheDocument();
    expect(screen.getByText("4/5 events")).toBeInTheDocument();
    expect(await screen.findByText("Initial assessment")).toBeInTheDocument();
    expect(screen.getByText("Healing is on track")).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "Open full timeline" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "Add note or evidence" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "Manage reminders" }),
    ).toBeInTheDocument();
  });

  it("keeps multiple procedures comparable and expands the selected lane", async () => {
    hooks.usePatientProcedures.mockReturnValue({
      data: [
        {
          id: "procedure-1",
          projectId: "patient-1",
          name: "Skin renewal",
          eventCount: 5,
          committedEventCount: 4,
          eventItemCount: 2,
          newItems: 0,
          createdAt: "2026-07-20T08:00:00Z",
        },
        {
          id: "procedure-2",
          projectId: "patient-1",
          name: "Scar care",
          eventCount: 3,
          committedEventCount: 1,
          eventItemCount: 0,
          newItems: 0,
          createdAt: "2026-07-21T08:00:00Z",
        },
      ],
      isLoading: false,
      error: null,
    });
    hooks.usePatientProcedureTriage.mockReturnValue({
      data: {
        totalProcedures: 2,
        incompleteProcedures: 2,
        newEvidence: 0,
        pendingReminders: 0,
      },
    });
    hooks.usePatientProcedureEvents.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });

    render(
      <PatientProceduresPanel patientId="patient-1" onNavigate={vi.fn()} />,
      { wrapper: Wrapper },
    );

    const first = screen.getByRole("button", { name: /Skin renewal/ });
    const second = screen.getByRole("button", { name: /Scar care/ });
    expect(first).toHaveAttribute("aria-expanded", "true");
    expect(second).toHaveAttribute("aria-expanded", "false");

    fireEvent.click(second);

    expect(first).toHaveAttribute("aria-expanded", "false");
    expect(second).toHaveAttribute("aria-expanded", "true");
  });
});
