import type { ReactNode } from "react";
import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { PatientOverviewCard } from "../components/patients/overview/PatientOverviewCard";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";

const messages = {
  ...clinicMessageCatalogs.en,
  common: {
    actions: { cancel: "Cancel", save: "Save", delete: "Delete" },
    values: { notAvailable: "Not available", unknown: "Unknown" },
    states: { loading: "Loading" },
  },
};

function Wrapper({ children }: { children: ReactNode }) {
  return (
    <LocalizationProvider locale="en" messages={messages}>
      {children}
    </LocalizationProvider>
  );
}

describe("PatientOverviewCard", () => {
  it("renders a compact native HeroUI patient card and its actions", () => {
    const onBack = vi.fn();
    const onCheckIn = vi.fn();
    const onEdit = vi.fn();
    const { container } = render(
      <PatientOverviewCard
        patient={{
          id: "patient-1",
          firstName: "Ada",
          lastName: "Lovelace",
          identificationCode: "P-100",
          dateOfBirth: "1815-12-10",
          gender: "Female",
          phone: "+44 20 1234 5678",
          email: "ada@example.test",
          insuranceProvider: "Analytical Mutual",
          allergies: "Pollen",
        }}
        onBack={onBack}
        onCheckIn={onCheckIn}
        onEdit={onEdit}
      />,
      { wrapper: Wrapper },
    );

    expect(container.querySelector('[data-slot="card"]')).toBeInTheDocument();
    expect(container.querySelector('[data-slot="avatar-fallback"]')).toHaveTextContent("AL");
    expect(screen.getByRole("heading", { name: "Ada Lovelace", level: 1 })).toBeInTheDocument();
    expect(container.querySelector('[data-slot="card-description"]')).toHaveTextContent(
      "Identification Code: P-100",
    );
    expect(screen.getByText("ada@example.test")).toBeInTheDocument();
    expect(screen.queryByText("Clinical Summary")).not.toBeInTheDocument();
    expect(screen.queryByText("System")).not.toBeInTheDocument();
    expect(screen.queryByText("Not available")).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Back to Patients" }));
    fireEvent.click(screen.getByRole("button", { name: "Edit" }));
    fireEvent.click(screen.getByRole("button", { name: "Check in" }));

    expect(onBack).toHaveBeenCalledOnce();
    expect(onEdit).toHaveBeenCalledOnce();
    expect(onCheckIn).toHaveBeenCalledOnce();
  });
});
