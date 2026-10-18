import type { ReactNode } from "react";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  AssistantActionReview,
  OperationsAssistantActionReview,
} from "../features/clinic-assistant/AssistantActions";
import type { AssistantAction } from "../features/clinic-assistant/hooks";
import { LocalizationProvider, clinicMessageCatalogs, type SupportedLocale } from "../localization";

const confirm = vi.fn();
const confirmOperation = vi.fn();
vi.mock("../features/clinic-assistant/hooks", () => ({
  useConfirmAssistantAction: () => ({ mutateAsync: confirm, isPending: false }),
  useRejectAssistantAction: () => ({ mutateAsync: vi.fn(), isPending: false }),
  useAssistantAction: vi.fn(),
  useConfirmOperationsAssistantAction: () => ({ mutateAsync: confirmOperation, isPending: false }),
  useRejectOperationsAssistantAction: () => ({ mutateAsync: vi.fn(), isPending: false }),
  useOperationsAssistantAction: vi.fn(),
}));

const common = {
  actions: { cancel: "Cancel", save: "Save", delete: "Delete" },
  values: { notAvailable: "Not available", unknown: "Unknown" },
  states: { loading: "Loading" },
};

function wrapper(locale: SupportedLocale) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <LocalizationProvider locale={locale} messages={{ ...clinicMessageCatalogs[locale], common }}>
        {children}
      </LocalizationProvider>
    );
  };
}

const plan = {
  name: "Knee arthroscopy",
  typeName: "General surgery",
  status: "Planned",
  scheduledAt: "2030-01-12T09:00:00Z",
  durationMinutes: 90,
  location: "OR 2",
  team: [{ displayName: "Dr. Sara Amini", role: "Surgeon" }],
  tools: ["Scalpel"],
  reminders: [{ name: "Reminder", minutesBefore: 1440, subject: "Tomorrow", textBody: "Your surgery is tomorrow." }],
};

function surgeryAction(actionType: string, reviewPayload: Record<string, unknown>): AssistantAction {
  return {
    actionId: `action-${actionType}`,
    agentRunId: "run-1",
    conversationId: "conversation-1",
    capability: "Surgeries",
    actionType,
    status: "Proposed",
    reviewPayload: { actionType, ...reviewPayload },
    expiresAt: "2030-01-10T08:30:00Z",
    completionPayload: null,
    failure: null,
  };
}

const surgeries = clinicMessageCatalogs.en.clinic.surgeries;

describe("surgery review card", () => {
  beforeEach(() => {
    confirm.mockReset();
    confirmOperation.mockReset();
  });
  afterEach(cleanup);

  it("shows only what an edit changes, as before and after, and confirms it", async () => {
    confirm.mockResolvedValue({ status: "Completed" });
    render(
      <AssistantActionReview
        patientId="patient-1"
        action={surgeryAction("update_patient_surgery", {
          previousSurgery: plan,
          surgery: { ...plan, location: "OR 1", status: "Completed" },
        })}
      />,
      { wrapper: wrapper("en") },
    );

    expect(screen.getByText(surgeries.actions.edit)).toBeVisible();
    expect(screen.getByText("OR 2 → OR 1")).toBeVisible();
    expect(screen.getByText("Planned → Completed")).toBeVisible();
    // Unchanged fields stay out of the review.
    expect(screen.queryByText("Knee arthroscopy")).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: surgeries.assistant.confirm }));
    await waitFor(() => expect(confirm).toHaveBeenCalledWith("action-update_patient_surgery"));
  });

  it("warns that a practice cancellation notifies the patient and team, and names the patient", async () => {
    confirmOperation.mockResolvedValue({ status: "Completed" });
    render(
      <OperationsAssistantActionReview
        action={surgeryAction("cancel_surgery", {
          patient: { displayName: "Ada Lovelace" },
          surgery: plan,
          notifiesPatientAndTeam: true,
        })}
      />,
      { wrapper: wrapper("en") },
    );

    expect(screen.getByText(surgeries.assistant.notifiesPatientAndTeam)).toBeVisible();
    expect(screen.getByText("Ada Lovelace")).toBeVisible();
    expect(screen.getByText(surgeries.actions.cancel)).toBeVisible();
    expect(screen.getByText("Dr. Sara Amini (Surgeon)")).toBeVisible();
    expect(screen.getByText("1 days before: Tomorrow")).toBeVisible();

    fireEvent.click(screen.getByRole("button", { name: clinicMessageCatalogs.en.clinic.operationsAssistant.confirm }));
    await waitFor(() => expect(confirmOperation).toHaveBeenCalledWith("action-cancel_surgery"));
  });

  it("reviews a deleted surgery type by what it removes", () => {
    render(
      <OperationsAssistantActionReview
        action={surgeryAction("delete_surgery_type", {
          previousSurgeryType: { name: "Rhinoplasty", tools: ["Osteotome"], defaultTeamRoles: ["Surgeon"], reminders: [] },
        })}
      />,
      { wrapper: wrapper("en") },
    );

    expect(screen.getByText(surgeries.assistant.actions.deleteType)).toBeVisible();
    expect(screen.getByText("Rhinoplasty")).toBeVisible();
    expect(screen.getByText("Osteotome")).toBeVisible();
  });

  it("points a change along the reading direction in Persian", () => {
    render(
      <OperationsAssistantActionReview
        action={surgeryAction("update_surgery_cancellation_message", {
          previousCancellationMessage: { subject: "قبلی", textBody: "متن" },
          cancellationMessage: { subject: "جدید", textBody: "متن" },
        })}
      />,
      { wrapper: wrapper("fa") },
    );

    expect(screen.getByText("قبلی ← جدید")).toBeVisible();
  });
});
