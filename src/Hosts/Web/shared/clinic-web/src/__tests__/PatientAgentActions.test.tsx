import type { ReactNode } from "react";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { PatientAgentActionReview } from "../features/patient-agent/PatientAgentActions";
import type { PatientAgentAction } from "../features/patient-agent/hooks";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";

const confirm = vi.fn();
const reject = vi.fn();
vi.mock("../features/patient-agent/hooks", () => ({
  useConfirmPatientAgentAction: () => ({ mutateAsync: confirm, isPending: false }),
  useRejectPatientAgentAction: () => ({ mutateAsync: reject, isPending: false }),
  usePatientAgentAction: vi.fn(),
}));
vi.mock("@vitarish/web-ui", () => ({
  FaIcon: ({ icon }: { icon: string }) => <span data-icon={icon} aria-hidden="true" />,
}));

const common = { actions: { cancel: "Cancel", save: "Save", delete: "Delete" }, values: { notAvailable: "Not available", unknown: "Unknown" }, states: { loading: "Loading" } };
function Wrapper({ children }: { children: ReactNode }) {
  return <LocalizationProvider locale="en" messages={{ ...clinicMessageCatalogs.en, common }}>{children}</LocalizationProvider>;
}
function action(actionId: string, capability: string, actionType: string): PatientAgentAction {
  return {
    actionId, agentRunId: "run-1", conversationId: "conversation-1", capability, actionType,
    status: "Proposed", reviewPayload: actionType.includes("appointment") ? { slot: { service: actionId } } : { templateName: actionId, items: [] },
    expiresAt: "2026-08-01T12:00:00Z", completionPayload: null, failure: null,
  };
}

describe("patient-agent independent actions", () => {
  beforeEach(() => { confirm.mockReset(); reject.mockReset(); });
  afterEach(cleanup);

  it("keeps a proposal actionable when confirmation transport fails", async () => {
    confirm.mockRejectedValue(new Error("offline"));
    render(<PatientAgentActionReview patientId="patient-1" action={action("appointment-1", "Appointments", "create_appointment")} />, { wrapper: Wrapper });
    fireEvent.click(screen.getByRole("button", { name: "Confirm appointment" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("The action could not be updated");
    expect(screen.getByRole("status")).toHaveTextContent("Proposed");
    expect(screen.getByRole("button", { name: "Confirm appointment" })).toBeEnabled();
  });

  it("confirms or rejects each backend action identifier independently", async () => {
    confirm.mockResolvedValue({ actionId: "appointment-1", status: "Completed" });
    reject.mockResolvedValue({ actionId: "prescription-1", status: "Rejected" });
    render(<div><PatientAgentActionReview patientId="patient-1" action={action("appointment-1", "Appointments", "create_appointment")} /><PatientAgentActionReview patientId="patient-1" action={action("prescription-1", "Prescriptions", "create_prescription")} /></div>, { wrapper: Wrapper });
    fireEvent.click(screen.getByRole("button", { name: "Confirm appointment" }));
    fireEvent.click(screen.getAllByRole("button", { name: "Reject" })[1]);
    await waitFor(() => {
      expect(confirm).toHaveBeenCalledWith("appointment-1");
      expect(reject).toHaveBeenCalledWith("prescription-1");
    });
  });

  it("preserves completed and expired cards without stale confirmation controls", () => {
    const completed = { ...action("appointment-1", "Appointments", "create_appointment"), status: "Completed" as const, completionPayload: { appointmentId: "reservation-42" } };
    const expired = { ...action("prescription-1", "Prescriptions", "create_prescription"), status: "Expired" as const };
    render(<div><PatientAgentActionReview patientId="patient-1" action={completed} /><PatientAgentActionReview patientId="patient-1" action={expired} /></div>, { wrapper: Wrapper });
    expect(screen.getByText("Completion: reservation-42")).toBeInTheDocument();
    expect(screen.getAllByRole("status").map((status) => status.textContent)).toEqual(expect.arrayContaining([expect.stringContaining("Completed"), expect.stringContaining("Expired")]));
    expect(screen.queryByRole("button", { name: /Confirm/ })).not.toBeInTheDocument();
  });
});
