import type { ReactNode } from "react";
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { PatientOverviewHeader } from "../components/patients/overview/PatientOverviewHeader";
import { PatientProfileCard } from "../components/patients/overview/PatientProfileCard";
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

const patient = {
  id: "patient-1",
  firstName: "Ada",
  lastName: "Lovelace",
  identificationCode: "P-100",
  dateOfBirth: "1815-12-10",
  gender: "Female" as const,
  phone: "+44 20 1234 5678",
  email: "ada@example.test",
  insuranceProvider: "Analytical Mutual",
  allergies: "Pollen",
};

describe("PatientOverviewHeader", () => {
  it("renders the patient avatar, name, and summary details", () => {
    const { container } = render(
      <PatientOverviewHeader patient={patient} />,
      { wrapper: Wrapper },
    );

    expect(container.querySelector('[data-slot="avatar-fallback"]')).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Ada Lovelace", level: 1 })).toBeInTheDocument();
    expect(screen.getByText("Identification Code: P-100")).toBeInTheDocument();
    expect(container.querySelector(".vitarish-reveal-header")).toHaveAttribute(
      "data-positioning",
      "overlay",
    );
    expect(container.querySelector("[data-vitarish-reveal-header-boundary]")).toBeInTheDocument();
  });
});

describe("PatientProfileCard", () => {
  it("renders patient detail sections", () => {
    render(<PatientProfileCard patient={patient} />, { wrapper: Wrapper });

    expect(screen.getByText("ada@example.test")).toBeInTheDocument();
    expect(screen.getByText("Pollen")).toBeInTheDocument();
  });
});
