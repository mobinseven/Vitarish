import userEvent from "@testing-library/user-event";
import { screen } from "@testing-library/react";
import { parseDate } from "@internationalized/date";
import { afterEach, describe, expect, it, vi } from "vitest";
import { renderWithLocalization } from "../localization/renderWithLocalization";

const mocks = vi.hoisted(() => ({
  push: vi.fn(),
  appointments: vi.fn(),
  surgeries: vi.fn(),
}));

vi.mock("next/navigation", () => ({ useRouter: () => ({ push: mocks.push }) }));
// The logo prompt has its own tests in @vitarish/journey; this test covers the day panels.
vi.mock("@vitarish/journey", () => ({ TenantLogoPrompt: () => null }));
vi.mock("@vitarish/clinic", async () => {
  const actual =
    await vi.importActual<typeof import("@vitarish/clinic")>(
      "@vitarish/clinic",
    );
  return {
    ...actual,
    useCanPlanSurgeries: () => true,
    useClinicListDashboardAppointments: mocks.appointments,
    useSurgeryCalendar: mocks.surgeries,
  };
});

import { HomeDashboard } from "@/features/home/components/HomeDashboard";

const patientId = "12345678-1234-1234-1234-123456789012";

function answerVisits(data: unknown[]) {
  mocks.appointments.mockReturnValue({ data, isLoading: false, error: null, refetch: vi.fn() });
}

function answerSurgeries(data: unknown[]) {
  mocks.surgeries.mockReturnValue({ data, isLoading: false, error: null, refetch: vi.fn() });
}

function plannedSurgery(id: string, patientDisplayName: string, scheduledAt: Date) {
  return {
    id,
    patientId,
    patientDisplayName,
    catalogId: "catalog-1",
    name: "Knee arthroscopy",
    status: "Planned",
    scheduledAt: scheduledAt.toISOString(),
    team: [],
    tools: [],
    personsToNotify: [],
    contacts: [],
    reminderPolicies: [],
    concurrencyToken: `token-${id}`,
    createdAt: scheduledAt.toISOString(),
  };
}

describe("HomeDashboard", () => {
  afterEach(() => {
    mocks.push.mockReset();
    mocks.appointments.mockReset();
    mocks.surgeries.mockReset();
  });

  it("shows the day's visits and opens the patient or a new note from a row", async () => {
    answerVisits([
      {
        reservationId: "reservation-1",
        status: 1,
        date: "2026-08-27",
        patientId,
        patientDisplayName: "Sara Ahmadi",
        serviceId: null,
        serviceDisplayName: "Consultation",
        startTime: "09:30:00",
        timeZone: "UTC",
        patientTags: ["VIP"],
      },
    ]);
    answerSurgeries([]);
    const user = userEvent.setup();
    renderWithLocalization(<HomeDashboard />, { locale: "en" });

    expect(screen.getByRole("heading", { level: 1, name: "Dashboard" })).toBeVisible();
    expect(screen.getByText("Confirmed")).toBeVisible();

    await user.click(screen.getByRole("button", { name: "Open Sara Ahmadi's profile" }));
    expect(mocks.push).toHaveBeenLastCalledWith(`/patients/${patientId}`);

    await user.click(screen.getByRole("button", { name: "New note for Sara Ahmadi" }));
    expect(mocks.push).toHaveBeenLastCalledWith(`/patients/${patientId}?action=newVisit`);

    await user.click(screen.getByRole("button", { name: "See all visits" }));
    expect(mocks.push).toHaveBeenLastCalledWith("/appointments");
  });

  it("moves both panels to the day chosen in the header", async () => {
    answerVisits([]);
    answerSurgeries([]);
    const user = userEvent.setup();
    renderWithLocalization(<HomeDashboard />, { locale: "en" });

    const initialVisits = mocks.appointments.mock.calls.at(-1)?.[0];
    const initialSurgeries = mocks.surgeries.mock.calls.at(-1)?.[0];

    await user.click(screen.getByRole("button", { name: "Previous day" }));

    const previousVisits = mocks.appointments.mock.calls.at(-1)?.[0];
    expect(previousVisits).toEqual({
      dateFrom: parseDate(initialVisits.dateFrom).subtract({ days: 1 }).toString(),
      dateTo: parseDate(initialVisits.dateFrom).subtract({ days: 1 }).toString(),
    });
    const previousSurgeries = mocks.surgeries.mock.calls.at(-1)?.[0];
    expect(Date.parse(previousSurgeries.from)).toBeLessThan(Date.parse(initialSurgeries.from));
  });

  it("names the next planned surgery when the chosen day has none", async () => {
    const inThreeDays = new Date();
    inThreeDays.setDate(inThreeDays.getDate() + 3);
    inThreeDays.setHours(9, 0, 0, 0);
    answerVisits([]);
    answerSurgeries([plannedSurgery("surgery-next", "Reza Karimi", inThreeDays)]);
    const user = userEvent.setup();
    renderWithLocalization(<HomeDashboard />, { locale: "en" });

    expect(screen.getByText("No surgeries on this day.")).toBeVisible();
    expect(screen.getByRole("heading", { name: "Next surgery" })).toBeVisible();

    await user.click(screen.getByRole("button", { name: /^Reza Karimi, Knee arthroscopy/ }));
    expect(mocks.push).toHaveBeenLastCalledWith("/surgeries/surgery-next");
  });
});
