import type { ReactNode } from "react";
import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  OperationsAssistantActionReview,
  AssistantActionReview,
} from "../features/clinic-assistant/AssistantActions";
import type { AssistantAction } from "../features/clinic-assistant/hooks";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";

const confirm = vi.fn();
const reject = vi.fn();
const confirmOperation = vi.fn();
const rejectOperation = vi.fn();
vi.mock("../features/clinic-assistant/hooks", () => ({
  useConfirmAssistantAction: () => ({
    mutateAsync: confirm,
    isPending: false,
  }),
  useRejectAssistantAction: () => ({
    mutateAsync: reject,
    isPending: false,
  }),
  useAssistantAction: vi.fn(),
  useConfirmOperationsAssistantAction: () => ({
    mutateAsync: confirmOperation,
    isPending: false,
  }),
  useRejectOperationsAssistantAction: () => ({
    mutateAsync: rejectOperation,
    isPending: false,
  }),
  useOperationsAssistantAction: vi.fn(),
}));
vi.mock("@vitarish/ui", () => ({
  FaIcon: ({ icon }: { icon: string }) => (
    <span data-icon={icon} aria-hidden="true" />
  ),
}));

const common = {
  actions: { cancel: "Cancel", save: "Save", delete: "Delete" },
  values: { notAvailable: "Not available", unknown: "Unknown" },
  states: { loading: "Loading" },
};
function Wrapper({ children }: { children: ReactNode }) {
  return (
    <LocalizationProvider
      locale="en"
      messages={{ ...clinicMessageCatalogs.en, common }}
    >
      {children}
    </LocalizationProvider>
  );
}
function action(
  actionId: string,
  capability: string,
  actionType: string,
): AssistantAction {
  return {
    actionId,
    agentRunId: "run-1",
    conversationId: "conversation-1",
    capability,
    actionType,
    status: "Proposed",
    reviewPayload: actionType.includes("appointment")
      ? { slot: { service: actionId } }
      : { templateName: actionId, items: [] },
    expiresAt: "2026-08-01T12:00:00Z",
    completionPayload: null,
    failure: null,
  };
}

describe("clinic-assistant independent actions", () => {
  beforeEach(() => {
    confirm.mockReset();
    reject.mockReset();
    confirmOperation.mockReset();
    rejectOperation.mockReset();
  });

  it("renders and decides an operations proposal from authoritative review data", async () => {
    confirmOperation.mockResolvedValue({
      actionId: "shift-1",
      status: "Completed",
    });
    render(
      <OperationsAssistantActionReview
        action={{
          actionId: "shift-1",
          agentRunId: "run-operations",
          conversationId: "conversation-operations",
          capability: "Operations",
          actionType: "create_shift",
          status: "Proposed",
          reviewPayload: {
            actionType: "create_shift",
            shift: {
              serviceName: "General consultation",
              serviceProviderName: "Dr Test",
              startTime: "09:00",
              endTime: "10:00",
              timeZone: "Asia/Tehran",
            },
          },
          expiresAt: "2026-08-01T12:00:00Z",
          completionPayload: null,
          failure: null,
        }}
      />,
      { wrapper: Wrapper },
    );

    expect(screen.getByText("Create shift")).toBeInTheDocument();
    expect(screen.getByText("General consultation")).toBeInTheDocument();
    expect(screen.getByText("Dr Test")).toBeInTheDocument();
    expect(screen.getByText("09:00–10:00")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Confirm action" }));

    await waitFor(() =>
      expect(confirmOperation).toHaveBeenCalledWith("shift-1"),
    );
    expect(confirm).not.toHaveBeenCalled();
  });
  afterEach(cleanup);

  it("keeps a proposal actionable when confirmation transport fails", async () => {
    confirm.mockRejectedValue(new Error("offline"));
    render(
      <AssistantActionReview
        patientId="patient-1"
        action={action("appointment-1", "Appointments", "create_appointment")}
      />,
      { wrapper: Wrapper },
    );
    fireEvent.click(
      screen.getByRole("button", { name: "Confirm appointment" }),
    );
    expect(await screen.findByRole("alert")).toHaveTextContent(
      "The action could not be updated",
    );
    expect(screen.getByRole("status")).toHaveTextContent("Proposed");
    expect(
      screen.getByRole("button", { name: "Confirm appointment" }),
    ).toBeEnabled();
  });

  it("confirms or rejects each backend action identifier independently", async () => {
    confirm.mockResolvedValue({
      actionId: "appointment-1",
      status: "Completed",
    });
    reject.mockResolvedValue({
      actionId: "prescription-1",
      status: "Rejected",
    });
    render(
      <div>
        <AssistantActionReview
          patientId="patient-1"
          action={action("appointment-1", "Appointments", "create_appointment")}
        />
        <AssistantActionReview
          patientId="patient-1"
          action={action(
            "prescription-1",
            "Prescriptions",
            "create_prescription",
          )}
        />
      </div>,
      { wrapper: Wrapper },
    );
    fireEvent.click(
      screen.getByRole("button", { name: "Confirm appointment" }),
    );
    fireEvent.click(screen.getAllByRole("button", { name: "Reject" })[1]);
    await waitFor(() => {
      expect(confirm).toHaveBeenCalledWith("appointment-1");
      expect(reject).toHaveBeenCalledWith("prescription-1");
    });
  });

  it("preserves completed and expired cards without stale confirmation controls", () => {
    const completed = {
      ...action("appointment-1", "Appointments", "create_appointment"),
      status: "Completed" as const,
      completionPayload: { appointmentId: "reservation-42" },
    };
    const expired = {
      ...action("prescription-1", "Prescriptions", "create_prescription"),
      status: "Expired" as const,
    };
    render(
      <div>
        <AssistantActionReview patientId="patient-1" action={completed} />
        <AssistantActionReview patientId="patient-1" action={expired} />
      </div>,
      { wrapper: Wrapper },
    );
    expect(screen.getByText("Completion: reservation-42")).toBeInTheDocument();
    expect(
      screen.getAllByRole("status").map((status) => status.textContent),
    ).toEqual(
      expect.arrayContaining([
        expect.stringContaining("Completed"),
        expect.stringContaining("Expired"),
      ]),
    );
    expect(
      screen.queryByRole("button", { name: /Confirm/ }),
    ).not.toBeInTheDocument();
  });
});
