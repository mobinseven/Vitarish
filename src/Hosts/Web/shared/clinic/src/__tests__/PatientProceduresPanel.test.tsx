import type { ReactNode } from "react";
import {
  cleanup,
  fireEvent,
  render,
  screen,
  within,
} from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { PatientProceduresPanel } from "../components/patients/procedures/PatientProceduresPanel";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";

const hooks = vi.hoisted(() => ({
  usePatientProcedures: vi.fn(),
  usePatientProcedureEvents: vi.fn(),
  useProcedureEventAuthorNames: vi.fn(),
}));

vi.mock("../hooks/patients/procedures/usePatientProcedures", () => ({
  usePatientProcedures: hooks.usePatientProcedures,
}));
vi.mock("../hooks/patients/procedures/usePatientProcedureEvents", () => ({
  usePatientProcedureEvents: hooks.usePatientProcedureEvents,
}));
vi.mock("../hooks/patients/procedures/useProcedureEventAuthorNames", () => ({
  useProcedureEventAuthorNames: hooks.useProcedureEventAuthorNames,
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

const procedure = (
  id: string,
  name: string,
  eventCount: number,
  committedEventCount: number,
) => ({
  id,
  projectId: "patient-1",
  name,
  eventCount,
  committedEventCount,
  eventItemCount: 0,
  newItems: 0,
  createdAt: "2026-07-20T08:00:00Z",
});

describe("PatientProceduresPanel", () => {
  it("shows every procedure at once with its progress and newest update", () => {
    hooks.usePatientProcedures.mockReturnValue({
      data: [
        procedure("procedure-1", "Skin renewal", 5, 4),
        procedure("procedure-2", "Scar care", 3, 0),
      ],
      isLoading: false,
      error: null,
    });
    hooks.useProcedureEventAuthorNames.mockReturnValue(
      new Map([["user-ali", "Dr. Ali"]]),
    );
    hooks.usePatientProcedureEvents.mockImplementation(
      (procedureId: string) => ({
        data:
          procedureId === "procedure-1"
            ? [
                {
                  id: "event-1",
                  procedureId,
                  projectId: "patient-1",
                  title: "Initial assessment",
                  isCommitted: true,
                  attachments: [
                    {
                      id: "item-1",
                      eventId: "event-1",
                      type: "note",
                      content: "Older note",
                      authorId: "user-ali",
                      createdAt: "2026-07-22T08:00:00Z",
                    },
                    {
                      id: "item-2",
                      eventId: "event-1",
                      type: "note",
                      content: "Healing is on track",
                      authorId: "user-ali",
                      createdAt: "2026-07-24T08:00:00Z",
                    },
                  ],
                  itemCount: 2,
                  position: 0,
                  createdAt: "2026-07-20T08:00:00Z",
                },
              ]
            : [],
        isLoading: false,
        error: null,
      }),
    );

    render(
      <PatientProceduresPanel patientId="patient-1" onNavigate={vi.fn()} />,
      { wrapper: Wrapper },
    );

    const rows = within(
      screen.getByRole("list", { name: procedures.title }),
    ).getAllByRole("listitem");
    expect(rows).toHaveLength(2);
    expect(
      within(rows[0]).getByRole("heading", { name: "Skin renewal" }),
    ).toBeVisible();
    expect(within(rows[0]).getByText("4/5 events")).toBeVisible();
    expect(within(rows[0]).getByText("Healing is on track")).toBeVisible();
    expect(within(rows[0]).queryByText("Older note")).toBeNull();
    expect(
      within(rows[0]).getByText(/Dr\. Ali · Initial assessment/),
    ).toBeVisible();
    expect(
      within(rows[1]).getByRole("heading", { name: "Scar care" }),
    ).toBeVisible();
    expect(
      within(rows[1]).getByText(procedures.progressAtGlance.noActivity),
    ).toBeVisible();
  });

  it("opens the chosen procedure", () => {
    hooks.usePatientProcedures.mockReturnValue({
      data: [
        procedure("procedure-1", "Skin renewal", 5, 4),
        procedure("procedure-2", "Scar care", 3, 1),
      ],
      isLoading: false,
      error: null,
    });
    hooks.useProcedureEventAuthorNames.mockReturnValue(new Map());
    hooks.usePatientProcedureEvents.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });
    const onNavigate = vi.fn();

    render(
      <PatientProceduresPanel patientId="patient-1" onNavigate={onNavigate} />,
      { wrapper: Wrapper },
    );

    fireEvent.click(
      screen.getByRole("button", { name: `${procedures.open}: Scar care` }),
    );

    expect(onNavigate).toHaveBeenCalledWith({
      kind: "patientProcedureDetail",
      patientId: "patient-1",
      procedureId: "procedure-2",
    });
  });
});
