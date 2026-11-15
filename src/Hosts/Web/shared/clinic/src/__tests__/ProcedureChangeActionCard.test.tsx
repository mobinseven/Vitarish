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
  useReviseOperationsAssistantAction: () => ({ mutateAsync: vi.fn(), isPending: false }),
  operationsAssistantActionQueryKey: (actionId: string) => ["operations-action", actionId],
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

function procedureAction(actionType: string, change: Record<string, unknown>): AssistantAction {
  return {
    actionId: `action-${actionType}`,
    agentRunId: "run-1",
    conversationId: "conversation-1",
    capability: "ProcedureChanges",
    actionType,
    status: "Proposed",
    reviewPayload: { change, confirmation: { actionId: "a", operation: "x", payloadJson: "{}", expiresAt: "2030-01-10T08:30:00Z" } },
    expiresAt: "2030-01-10T08:30:00Z",
    completionPayload: null,
    failure: null,
  };
}

const text = clinicMessageCatalogs.en.clinic.clinicAssistant.procedureChange;

describe("procedure change review card", () => {
  beforeEach(() => {
    confirm.mockReset();
    confirmOperation.mockReset();
  });
  afterEach(cleanup);

  it("names the step by position and title, shows the exact note to be written, and confirms it on the patient page", async () => {
    confirm.mockResolvedValue({ status: "Completed" });
    render(
      <AssistantActionReview
        patientId="patient-1"
        action={procedureAction("procedure_step_change", {
          kind: "add_note",
          procedureName: "Implant treatment",
          stepTitle: "Fixture placement",
          stepPosition: 2,
          text: "Torque 35 Ncm, no complications.",
        })}
      />,
      { wrapper: wrapper("en") },
    );

    expect(screen.getByText(text.title.add_note)).toBeVisible();
    expect(screen.getByText("Implant treatment")).toBeVisible();
    expect(screen.getByText("Step 2: Fixture placement")).toBeVisible();
    expect(screen.getByText("Torque 35 Ncm, no complications.")).toBeVisible();

    fireEvent.click(screen.getByRole("button", { name: text.confirm }));
    await waitFor(() => expect(confirm).toHaveBeenCalledWith("action-procedure_step_change"));
  });

  it("shows a reminder's time and channel in Persian in the practice workspace", async () => {
    confirmOperation.mockResolvedValue({ status: "Completed" });
    const fa = clinicMessageCatalogs.fa.clinic.clinicAssistant.procedureChange;
    render(
      <OperationsAssistantActionReview
        action={procedureAction("operations_procedure_step_change", {
          kind: "add_reminder",
          procedureName: "درمان ایمپلنت",
          stepTitle: "کنترل ترمیم",
          stepPosition: 3,
          text: "تماس با بیمار",
          remindAt: "2030-01-12T09:00:00Z",
          channel: "sms",
        })}
      />,
      { wrapper: wrapper("fa") },
    );

    expect(screen.getByText(fa.title.add_reminder)).toBeVisible();
    expect(screen.getByText(fa.channels.sms)).toBeVisible();
    expect(screen.getByText(fa.fields.remindAt)).toBeVisible();
    expect(screen.getByText("تماس با بیمار")).toBeVisible();
  });

  it("refuses to offer confirmation for a change it cannot describe", () => {
    render(
      <AssistantActionReview patientId="patient-1" action={procedureAction("procedure_step_change", { kind: "delete_everything" })} />,
      { wrapper: wrapper("en") },
    );

    expect(screen.getByText(clinicMessageCatalogs.en.clinic.clinicAssistant.actionUnavailable)).toBeVisible();
    expect(screen.queryByRole("button", { name: text.confirm })).toBeNull();
  });
});
