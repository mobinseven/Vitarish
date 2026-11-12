import type { ReactNode } from "react";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ManageActionCard, StaffInviteActionCard } from "../features/clinic-assistant/ManageActionCard";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";

function Wrapper({ children }: { children: ReactNode }) {
  return <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>{children}</LocalizationProvider>;
}

const reviewPayload = {
  operation: "create_shift",
  payload: { name: "Monday morning", dayOfWeek: 1, startTime: "09:00", totalSeats: 4, isActive: true, nested: { ignored: true } },
};

afterEach(cleanup);

describe("ManageActionCard", () => {
  it("shows the operation label and the primitive fields", () => {
    render(<ManageActionCard reviewPayload={reviewPayload} />, { wrapper: Wrapper });

    expect(screen.getByText("Create shift")).toBeInTheDocument();
    expect(screen.getByText("Name")).toBeInTheDocument();
    expect(screen.getByText("Monday morning")).toBeInTheDocument();
    expect(screen.getByText("Capacity")).toBeInTheDocument();
    expect(screen.getByText("4")).toBeInTheDocument();
    expect(screen.getByText("09:00")).toBeInTheDocument();
    expect(screen.queryByText("nested")).toBeNull();
  });

  it("falls back to the raw operation name when it has no label", () => {
    render(<ManageActionCard reviewPayload={{ operation: "mystery_op", payload: {} }} />, { wrapper: Wrapper });
    expect(screen.getByText("mystery_op")).toBeInTheDocument();
  });

  it("offers no edit without onRevise", () => {
    render(<ManageActionCard reviewPayload={reviewPayload} />, { wrapper: Wrapper });
    expect(screen.queryByRole("button", { name: /Edit/ })).toBeNull();
  });

  it("sends the revised arguments keeping the operation and parsing numbers", async () => {
    const onRevise = vi.fn().mockResolvedValue(undefined);
    render(<ManageActionCard reviewPayload={reviewPayload} onRevise={onRevise} />, { wrapper: Wrapper });

    fireEvent.click(screen.getByRole("button", { name: /Edit/ }));
    fireEvent.change(screen.getByLabelText("Capacity"), { target: { value: "6" } });
    fireEvent.change(screen.getByLabelText("Name"), { target: { value: "Monday late" } });
    fireEvent.click(screen.getByRole("button", { name: /Save changes/ }));

    await waitFor(() => expect(onRevise).toHaveBeenCalledTimes(1));
    expect(onRevise).toHaveBeenCalledWith({
      operation: "create_shift",
      payload: { ...reviewPayload.payload, name: "Monday late", totalSeats: 6, dayOfWeek: 1 },
    });
    expect(typeof onRevise.mock.calls[0]![0].payload.totalSeats).toBe("number");
    await waitFor(() => expect(screen.queryByRole("button", { name: /Save changes/ })).toBeNull());
  });

  it("shows an error and does not revise when a number is invalid", async () => {
    const onRevise = vi.fn();
    render(<ManageActionCard reviewPayload={reviewPayload} onRevise={onRevise} />, { wrapper: Wrapper });

    fireEvent.click(screen.getByRole("button", { name: /Edit/ }));
    fireEvent.change(screen.getByLabelText("Capacity"), { target: { value: "many" } });
    fireEvent.click(screen.getByRole("button", { name: /Save changes/ }));

    expect(await screen.findByRole("alert")).toHaveTextContent(clinicMessageCatalogs.en.clinic.operationsAssistant.manage.editError);
    expect(onRevise).not.toHaveBeenCalled();
  });

  it("keeps the form open with an error when saving fails", async () => {
    const onRevise = vi.fn().mockRejectedValue(new Error("offline"));
    render(<ManageActionCard reviewPayload={reviewPayload} onRevise={onRevise} />, { wrapper: Wrapper });

    fireEvent.click(screen.getByRole("button", { name: /Edit/ }));
    fireEvent.click(screen.getByRole("button", { name: /Save changes/ }));

    expect(await screen.findByRole("alert")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Save changes/ })).toBeInTheDocument();
  });
});

describe("StaffInviteActionCard", () => {
  it("shows the full contact and the Staff role", () => {
    render(
      <StaffInviteActionCard
        reviewPayload={{ firstName: "Sara", lastName: "Rad", phoneNumber: "+989121234567", email: "sara@example.test" }}
      />,
      { wrapper: Wrapper },
    );

    expect(screen.getByText("Sara")).toBeInTheDocument();
    expect(screen.getByText("Rad")).toBeInTheDocument();
    expect(screen.getByText("+989121234567")).toBeInTheDocument();
    expect(screen.getByText("sara@example.test")).toBeInTheDocument();
    expect(screen.getByText("Role")).toBeInTheDocument();
    expect(screen.getByText("Staff")).toBeInTheDocument();
  });
});
