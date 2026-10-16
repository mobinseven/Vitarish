import type { ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { PatientProcedureEventCommitDialog } from "../components/patients/procedures/PatientProcedureEventCommitDialog";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";

const sea = vi.hoisted(() => ({
  commitEvent: vi.fn(),
  uncommitEvent: vi.fn(),
}));

vi.mock("@vitarish/sea", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@vitarish/sea")>()),
  commitEvent: sea.commitEvent,
  uncommitEvent: sea.uncommitEvent,
}));

const events = clinicMessageCatalogs.en.clinic.procedures.events;

function renderDialog(isCommitted: boolean, onClose = vi.fn()) {
  const client = new QueryClient({ defaultOptions: { mutations: { retry: false } } });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={client}>
      <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>
        {children}
      </LocalizationProvider>
    </QueryClientProvider>
  );
  render(
    <PatientProcedureEventCommitDialog
      eventId="event-1"
      eventTitle="Impressions"
      isCommitted={isCommitted}
      onClose={onClose}
    />,
    { wrapper },
  );
  return onClose;
}

beforeEach(() => {
  sea.commitEvent.mockReset();
  sea.uncommitEvent.mockReset();
});
afterEach(cleanup);

describe("PatientProcedureEventCommitDialog", () => {
  it("marks the step complete only once the clinician confirms", async () => {
    sea.commitEvent.mockResolvedValue({ succeeded: true, entityId: "event-1" });
    const onClose = renderDialog(false);

    expect(screen.getByText(events.commitConfirmation.commitMessage)).toBeVisible();
    expect(sea.commitEvent).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole("button", { name: events.commitConfirmation.confirmCommit }));

    await waitFor(() => expect(onClose).toHaveBeenCalled());
    expect(sea.commitEvent).toHaveBeenCalledWith("event-1");
    expect(sea.uncommitEvent).not.toHaveBeenCalled();
  });

  it("reopens a completed step instead of committing it again", async () => {
    sea.uncommitEvent.mockResolvedValue({ succeeded: true, entityId: "event-1" });
    const onClose = renderDialog(true);

    fireEvent.click(screen.getByRole("button", { name: events.commitConfirmation.confirmReopen }));

    await waitFor(() => expect(onClose).toHaveBeenCalled());
    expect(sea.uncommitEvent).toHaveBeenCalledWith("event-1");
    expect(sea.commitEvent).not.toHaveBeenCalled();
  });

  it("stays open and reports the failure when the server rejects the change", async () => {
    sea.commitEvent.mockRejectedValue(new Error("unavailable"));
    const onClose = renderDialog(false);

    fireEvent.click(screen.getByRole("button", { name: events.commitConfirmation.confirmCommit }));

    expect(await screen.findByText(events.commitConfirmation.failed)).toBeVisible();
    expect(onClose).not.toHaveBeenCalled();
  });
});
