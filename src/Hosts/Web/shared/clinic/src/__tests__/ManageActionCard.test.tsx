import type { ReactNode } from "react";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ClinicProfileActionCard, ManageActionCard, StaffInviteActionCard, StaffRemovalActionCard } from "../features/clinic-assistant/ManageActionCard";
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

  it("edits the names and only the contact the invitation uses, and saves exactly those arguments", async () => {
    const onRevise = vi.fn().mockResolvedValue(undefined);
    render(
      <StaffInviteActionCard reviewPayload={{ firstName: "Sara", lastName: "Rad", phoneNumber: "+989121234567" }} onRevise={onRevise} />,
      { wrapper: Wrapper },
    );

    fireEvent.click(screen.getByRole("button", { name: /Edit/ }));
    expect(screen.getByLabelText("First name")).toHaveValue("Sara");
    expect(screen.getByLabelText("Last name")).toHaveValue("Rad");
    expect(screen.getByLabelText("Phone")).toHaveValue("+989121234567");
    expect(screen.queryByLabelText("Email")).toBeNull();
    expect(screen.queryByLabelText("Role")).toBeNull();

    fireEvent.change(screen.getByLabelText("First name"), { target: { value: "Sarah" } });
    fireEvent.change(screen.getByLabelText("Phone"), { target: { value: "+989129999999" } });
    fireEvent.click(screen.getByRole("button", { name: /Save changes/ }));

    await waitFor(() => expect(onRevise).toHaveBeenCalledTimes(1));
    expect(onRevise).toHaveBeenCalledWith({ firstName: "Sarah", lastName: "Rad", phoneNumber: "+989129999999" });
    expect(Object.keys(onRevise.mock.calls[0]![0]).sort()).toEqual(["firstName", "lastName", "phoneNumber"]);
    await waitFor(() => expect(screen.queryByRole("button", { name: /Save changes/ })).toBeNull());
  });

  it("revises an email invitation with the email, not a phone number", async () => {
    const onRevise = vi.fn().mockResolvedValue(undefined);
    render(
      <StaffInviteActionCard reviewPayload={{ firstName: "Sara", lastName: "Rad", email: "sara@example.test" }} onRevise={onRevise} />,
      { wrapper: Wrapper },
    );

    fireEvent.click(screen.getByRole("button", { name: /Edit/ }));
    expect(screen.queryByLabelText("Phone")).toBeNull();
    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "new@example.test" } });
    fireEvent.click(screen.getByRole("button", { name: /Save changes/ }));

    await waitFor(() => expect(onRevise).toHaveBeenCalledTimes(1));
    expect(onRevise).toHaveBeenCalledWith({ firstName: "Sara", lastName: "Rad", email: "new@example.test" });
  });

  it("keeps the form and the typed values with an error when saving fails", async () => {
    const onRevise = vi.fn().mockRejectedValue(new Error("offline"));
    render(
      <StaffInviteActionCard reviewPayload={{ firstName: "Sara", lastName: "Rad", phoneNumber: "+989121234567" }} onRevise={onRevise} />,
      { wrapper: Wrapper },
    );

    fireEvent.click(screen.getByRole("button", { name: /Edit/ }));
    fireEvent.change(screen.getByLabelText("Last name"), { target: { value: "Radmehr" } });
    fireEvent.click(screen.getByRole("button", { name: /Save changes/ }));

    expect(await screen.findByRole("alert")).toHaveTextContent(clinicMessageCatalogs.en.clinic.operationsAssistant.manage.editError);
    expect(screen.getByLabelText("Last name")).toHaveValue("Radmehr");
    expect(screen.getByRole("button", { name: /Save changes/ })).toBeInTheDocument();
  });

  it("offers no edit without onRevise", () => {
    render(<StaffInviteActionCard reviewPayload={{ firstName: "Sara", lastName: "Rad", phoneNumber: "+989121234567" }} />, { wrapper: Wrapper });
    expect(screen.queryByRole("button", { name: /Edit/ })).toBeNull();
  });
});

describe("StaffRemovalActionCard", () => {
  it("names the member and shows only the masked contact", () => {
    render(<StaffRemovalActionCard reviewPayload={{ userId: "staff-1", name: "Sara Karimi", contact: "+98•••••4567" }} />, { wrapper: Wrapper });

    expect(screen.getByText("Sara Karimi")).toBeInTheDocument();
    expect(screen.getByText("+98•••••4567")).toBeInTheDocument();
    expect(screen.queryByText("staff-1")).toBeNull();
  });
});

describe("ClinicProfileActionCard", () => {
  it("shows only the parts of the profile that change", () => {
    render(
      <ClinicProfileActionCard reviewPayload={{ address: { city: "Tehran", street: "Valiasr 10" }, phones: ["021 1234", "021 5678"] }} />,
      { wrapper: Wrapper },
    );

    expect(screen.getByText("Address")).toBeInTheDocument();
    expect(screen.getByText("Tehran, Valiasr 10")).toBeInTheDocument();
    expect(screen.getByText("021 1234, 021 5678")).toBeInTheDocument();
    expect(screen.queryByText("Website")).toBeNull();
    expect(screen.queryByText("Description")).toBeNull();
  });
});
