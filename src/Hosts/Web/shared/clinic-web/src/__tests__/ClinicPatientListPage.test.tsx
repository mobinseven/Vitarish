import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";

const patientListQuery = vi.hoisted(() => ({
  data: { pages: [[]] },
  fetchNextPage: vi.fn(),
  hasNextPage: false,
  isFetchingNextPage: false,
  isLoading: false,
}));

vi.mock("../hooks/patients/usePatientList", () => ({
  usePatientList: () => patientListQuery,
}));

vi.mock("../hooks/patients/usePatientCount", () => ({
  usePatientCount: () => ({ data: 56 }),
}));

vi.mock("../components/patients/list/PatientsGrid", () => ({
  PatientsGrid: () => <div data-testid="patients-grid" />,
}));

import { ClinicPatientListPage } from "../components/patients/list/ClinicPatientListPage";

const messages = {
  ...clinicMessageCatalogs.en,
  common: {
    actions: { cancel: "Cancel", save: "Save", delete: "Delete" },
    values: { notAvailable: "Not available", unknown: "Unknown" },
    states: { loading: "Loading" },
  },
};

describe("ClinicPatientListPage mobile composition", () => {
  it("leaves the dashboard title to the shell and groups count, search, and create in the action bar", () => {
    const onNavigate = vi.fn();

    render(
      <LocalizationProvider locale="en" messages={messages}>
        <ClinicPatientListPage onNavigate={onNavigate} />
      </LocalizationProvider>,
    );

    expect(screen.queryByRole("heading", { name: "Patients" })).not.toBeInTheDocument();
    expect(screen.getByRole("toolbar", { name: "Patients" })).toBeInTheDocument();
    expect(screen.getByLabelText("56 patients found")).toHaveTextContent("56");
    expect(screen.getByRole("searchbox", { name: "Search patients" })).toBeInTheDocument();
    expect(screen.getByTestId("patients-grid")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "New Patient" }));

    expect(onNavigate).toHaveBeenCalledWith({ kind: "patientNew" });
  });
});
