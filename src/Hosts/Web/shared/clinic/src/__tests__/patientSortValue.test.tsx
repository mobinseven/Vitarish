import type { ReactNode } from "react";
import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { PatientRow } from "../components/patients/find-or-create/PatientRow";
import {
  formatDate,
  LocalizationProvider,
  clinicMessageCatalogs,
} from "../localization";
import type { PatientViewDto } from "../api/generated/clinic/model";

afterEach(() => {
  cleanup();
});

function Wrapper({ children }: { children: ReactNode }) {
  return (
    <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>
      {children}
    </LocalizationProvider>
  );
}

const basePatient: PatientViewDto = {
  id: "patient-1",
  firstName: "Ada",
  lastName: "Lovelace",
  identificationCode: "P-100",
};

describe("PatientRow sorted value", () => {
  it("shows the localized date for the property the list is sorted by", () => {
    const nextSurgeryAt = "2026-03-05T09:30:00.000Z";

    render(
      <PatientRow
        patient={{ ...basePatient, nextSurgeryAt }}
        subtitleField="NextSurgery"
        onSelect={() => {}}
      />,
      { wrapper: Wrapper },
    );

    const line = screen.getByText(/Next surgery:/);
    expect(line).toHaveTextContent(formatDate(nextSurgeryAt, "en"));
  });

  it("omits the value line when the sorted property has no value", () => {
    render(
      <PatientRow
        patient={basePatient}
        subtitleField="LastSurgery"
        onSelect={() => {}}
      />,
      { wrapper: Wrapper },
    );

    expect(screen.getByText("Ada Lovelace")).toBeInTheDocument();
    expect(screen.queryByText(/Last surgery/)).not.toBeInTheDocument();
  });

  it("keeps the identification code line for the default name sort", () => {
    render(
      <PatientRow
        patient={basePatient}
        subtitleField="Name"
        onSelect={() => {}}
      />,
      { wrapper: Wrapper },
    );

    expect(screen.getByText("Identification Code: P-100")).toBeInTheDocument();
  });
});
