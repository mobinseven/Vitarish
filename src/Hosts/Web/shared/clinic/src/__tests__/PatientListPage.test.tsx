import { act, render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";

const patientListQuery = vi.hoisted(() => ({
  data: { pages: [[]] },
  fetchNextPage: vi.fn(),
  hasNextPage: false,
  isFetchingNextPage: false,
  isLoading: false,
}));
const capturedPatientsGridProps = vi.hoisted(() => ({
  current: undefined as Record<string, unknown> | undefined,
}));

vi.mock("../hooks/patients/usePatientList", () => ({
  usePatientList: () => patientListQuery,
}));

vi.mock("../hooks/patients/usePatientCount", () => ({
  usePatientCount: () => ({ data: 56 }),
}));

vi.mock("../components/patients/list/PatientsGrid", () => ({
  PatientsGrid: (props: Record<string, unknown>) => {
    capturedPatientsGridProps.current = props;
    return <div data-testid="patients-grid" />;
  },
}));

import { PatientListPage } from "../components/patients/list/PatientListPage";

const messages = {
  ...clinicMessageCatalogs.en,
  common: {
    actions: { cancel: "Cancel", save: "Save", delete: "Delete" },
    values: { notAvailable: "Not available", unknown: "Unknown" },
    states: { loading: "Loading" },
  },
};

describe("PatientListPage mobile composition", () => {
  it("leaves toolbar rendering to the grid and supplies its search and navigation behavior", () => {
    const onNavigate = vi.fn();

    render(
      <LocalizationProvider locale="en" messages={messages}>
        <PatientListPage onNavigate={onNavigate} />
      </LocalizationProvider>,
    );

    expect(
      screen.queryByRole("heading", { name: "Patients" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("toolbar", { name: "Patients" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("searchbox", { name: "Search patients" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "New Patient" }),
    ).not.toBeInTheDocument();
    expect(screen.getByTestId("patients-grid")).toBeInTheDocument();
    expect(capturedPatientsGridProps.current?.totalCount).toBe(56);

    act(() => {
      (capturedPatientsGridProps.current?.onAddPatient as () => void)();
    });

    expect(onNavigate).toHaveBeenCalledWith({ kind: "patientNew" });
  });

  it("keeps the grid mounted while a search starts loading", () => {
    const view = render(
      <LocalizationProvider locale="en" messages={messages}>
        <PatientListPage onNavigate={vi.fn()} />
      </LocalizationProvider>,
    );

    patientListQuery.isLoading = true;
    act(() => {
      (capturedPatientsGridProps.current?.onSearch as (value: string) => void)(
        "Ada",
      );
    });

    expect(
      within(view.container).getByTestId("patients-grid"),
    ).toBeInTheDocument();
    expect(capturedPatientsGridProps.current?.isLoading).toBe(true);
  });
});
