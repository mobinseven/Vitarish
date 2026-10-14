import type { ReactNode } from "react";
import { cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { getLocalTimeZone } from "@internationalized/date";
import { surgeryDayKey } from "../models/surgeries/surgeryCalendarRange";
import type { Surgery } from "../models/surgeries/surgeryModels";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";

const hooks = vi.hoisted(() => ({
  useSurgeryCalendar: vi.fn(),
  useClinicStaff: vi.fn(),
  useCanPlanSurgeries: vi.fn(),
}));

vi.mock("../hooks/surgeries/useSurgeryCalendar", () => ({
  useSurgeryCalendar: hooks.useSurgeryCalendar,
}));
vi.mock("../hooks/surgeries/useClinicStaff", () => ({
  useClinicStaff: hooks.useClinicStaff,
}));
vi.mock("../hooks/surgeries/useCanPlanSurgeries", () => ({
  useCanPlanSurgeries: hooks.useCanPlanSurgeries,
}));

import { SurgeryCalendarPage } from "../components/surgeries/SurgeryCalendarPage";

function message(path: string): string {
  const value = path
    .split(".")
    .reduce<unknown>(
      (current, part) => (current && typeof current === "object" ? (current as Record<string, unknown>)[part] : undefined),
      clinicMessageCatalogs.en,
    );
  return typeof value === "string" ? value : path;
}

const timeZone = getLocalTimeZone();
const todayAtNine = new Date();
todayAtNine.setHours(9, 0, 0, 0);
const tomorrowAtTen = new Date();
tomorrowAtTen.setDate(tomorrowAtTen.getDate() + 1);
tomorrowAtTen.setHours(10, 0, 0, 0);

const surgeries: Surgery[] = [
  {
    id: "surgery-a",
    patientId: "patient-1",
    patientDisplayName: "Alice",
    catalogId: "catalog-1",
    name: "Knee arthroscopy",
    status: "Planned",
    scheduledAt: todayAtNine.toISOString(),
    location: "Operating room 1",
    team: [],
    tools: [],
    reminderPolicies: [],
    concurrencyToken: "token-a",
    createdAt: todayAtNine.toISOString(),
  },
  {
    id: "surgery-b",
    patientId: "patient-2",
    patientDisplayName: "Bob",
    catalogId: "catalog-1",
    name: "Cataract surgery",
    status: "Planned",
    scheduledAt: tomorrowAtTen.toISOString(),
    location: "Operating room 2",
    team: [],
    tools: [],
    reminderPolicies: [],
    concurrencyToken: "token-b",
    createdAt: todayAtNine.toISOString(),
  },
  {
    id: "surgery-c",
    patientId: "patient-3",
    patientDisplayName: "Carol",
    catalogId: "catalog-1",
    name: "Appendectomy",
    status: "Cancelled",
    scheduledAt: new Date(todayAtNine.getTime() + 60 * 60 * 1000).toISOString(),
    location: "Operating room 1",
    team: [],
    tools: [],
    reminderPolicies: [],
    concurrencyToken: "token-c",
    createdAt: todayAtNine.toISOString(),
  },
];

function Wrapper({ children }: { children: ReactNode }) {
  return (
    <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>
      {children}
    </LocalizationProvider>
  );
}

describe("SurgeryCalendarPage", () => {
  beforeEach(() => {
    hooks.useSurgeryCalendar.mockReturnValue({
      data: surgeries,
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });
    hooks.useClinicStaff.mockReturnValue({ data: [], isLoading: false, error: null });
    hooks.useCanPlanSurgeries.mockReturnValue(true);
  });

  afterEach(cleanup);

  it("lists a selected day's surgeries, applies the status filter, and opens a surgery", () => {
    const onNavigate = vi.fn();
    render(<SurgeryCalendarPage onNavigate={onNavigate} />, { wrapper: Wrapper });

    const list = () => screen.getByRole("region", { name: message("clinic.surgeries.calendar.listLabel") });
    const todayKey = surgeryDayKey(surgeries[0].scheduledAt, timeZone);

    expect(within(list()).getByText("Alice")).toBeInTheDocument();
    expect(within(list()).getByText("Bob")).toBeInTheDocument();
    expect(within(list()).getByText("Carol")).toBeInTheDocument();

    fireEvent.click(screen.getByTestId(`surgery-day-${todayKey}`));

    expect(within(list()).getByText("Alice")).toBeInTheDocument();
    expect(within(list()).getByText("Carol")).toBeInTheDocument();
    expect(within(list()).queryByText("Bob")).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("radio", { name: message("clinic.surgeries.status.cancelled") }));

    expect(within(list()).getByText("Carol")).toBeInTheDocument();
    expect(within(list()).queryByText("Alice")).not.toBeInTheDocument();

    fireEvent.click(within(list()).getByRole("button", { name: /Carol/ }));

    expect(onNavigate).toHaveBeenCalledWith({ kind: "surgeryDetail", surgeryId: "surgery-c" });
  });
});
