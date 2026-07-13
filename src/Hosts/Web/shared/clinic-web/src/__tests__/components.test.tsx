import type { ReactNode } from "react";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider } from "../localization";
import { clinicMessageCatalogs } from "../localization";
import { ServiceList } from "../components/services/ServiceList";
import { ServiceEditor } from "../components/services/ServiceEditor";
import { ServiceTypeEditor } from "../components/service-types/ServiceTypeEditor";
import { ServiceProviderEditor } from "../components/service-providers/ServiceProviderEditor";
import { ShiftEditor } from "../components/appointments/shifts/ShiftEditor";
import { ServiceDeleteDialog } from "../components/services/ServiceDeleteDialog";
import { PatientProfileForm } from "../components/patients/profile/PatientProfileForm";
import { emptyFormData } from "../models/patients/profile/PatientProfileForm";

const messages = {
  ...clinicMessageCatalogs.en,
  common: {
    actions: { cancel: "Cancel", save: "Save", delete: "Delete" },
    values: { notAvailable: "Not available", unknown: "Unknown" },
    states: { loading: "Loading" },
  },
};

afterEach(cleanup);

function Wrapper({ children }: { children: ReactNode }) {
  return <LocalizationProvider locale="en" messages={messages}>{children}</LocalizationProvider>;
}

describe("clinic display components", () => {
  it("renders service presentation models and delegates delete", () => {
    const onDelete = vi.fn();
    render(<ServiceList items={[{ id: "s-1", name: "Consultation", serviceTypeName: "Visit" }]} onDelete={onDelete} />, { wrapper: Wrapper });
    expect(screen.getByText("Consultation")).toBeInTheDocument();
    expect(screen.getByText("Visit")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Delete" }));
    expect(onDelete).toHaveBeenCalledWith("s-1");
  });
});

describe("clinic editors", () => {
  it("validates and submits the patient editor", () => {
    const onSubmit = vi.fn();
    render(<PatientProfileForm initialData={emptyFormData()} onSubmit={onSubmit} onCancel={vi.fn()} />, { wrapper: Wrapper });
    const form = screen.getByRole("button", { name: "Save Patient" }).closest("form");
    expect(form).not.toBeNull();
    fireEvent.submit(form!);
    expect(screen.getByText("First name is required.")).toBeInTheDocument();
    expect(screen.getByText("Last name is required.")).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("First Name"), { target: { value: "Ada" } });
    fireEvent.change(screen.getByLabelText("Last Name"), { target: { value: "Lovelace" } });
    fireEvent.submit(form!);
    expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ firstName: "Ada", lastName: "Lovelace" }));
  });

  it.each([
    ["service", ServiceEditor],
    ["service type", ServiceTypeEditor],
    ["service provider", ServiceProviderEditor],
  ] as const)("submits localized %s editor values", (_name, Editor) => {
    const onSubmit = vi.fn();
    render(<Editor initialValue={{ name: "Initial", description: "Description" }} isPending={false} onSubmit={onSubmit} onCancel={vi.fn()} />, { wrapper: Wrapper });
    fireEvent.change(screen.getByLabelText("Name"), { target: { value: "Updated" } });
    fireEvent.click(screen.getByRole("button", { name: "Save" }));
    expect(onSubmit).toHaveBeenCalledWith({ name: "Updated", description: "Description" });
  });

  it("submits shift values through the shift editor", () => {
    const onSubmit = vi.fn();
    render(<ShiftEditor initialValue={{ dayOfWeek: 1, startTime: "09:00", endTime: "10:00", totalSeats: 10, timeZone: "UTC" }} isPending={false} onSubmit={onSubmit} onCancel={vi.fn()} />, { wrapper: Wrapper });
    fireEvent.click(screen.getByRole("button", { name: "Save" }));
    expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ dayOfWeek: 1, startTime: "09:00", endTime: "10:00", totalSeats: 10, timeZone: "UTC" }));
  });
});

describe("clinic delete dialogs", () => {
  it("exposes cancel and confirm actions", () => {
    const onCancel = vi.fn();
    const onConfirm = vi.fn();
    render(<ServiceDeleteDialog isOpen isPending={false} onCancel={onCancel} onConfirm={onConfirm} />, { wrapper: Wrapper });
    expect(screen.getByRole("dialog")).toHaveAccessibleName("Delete Service");
    fireEvent.click(screen.getByRole("button", { name: "Delete" }));
    expect(onConfirm).toHaveBeenCalledOnce();
    fireEvent.click(screen.getByRole("button", { name: "Cancel" }));
    expect(onCancel).toHaveBeenCalledOnce();
  });

  it("disables cancellation while deletion is pending", () => {
    render(<ServiceDeleteDialog isOpen isPending onCancel={vi.fn()} onConfirm={vi.fn()} />, { wrapper: Wrapper });
    expect(screen.getByRole("button", { name: "Cancel" })).toBeDisabled();
  });
});
