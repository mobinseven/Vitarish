import userEvent from "@testing-library/user-event";
import { screen } from "@testing-library/react";
import { parseDate } from "@internationalized/date";
import { describe, expect, it, vi } from "vitest";
import { renderWithLocalization } from "../localization/renderWithLocalization";

const mocks = vi.hoisted(() => ({
  push: vi.fn(),
  appointments: vi.fn(),
}));

vi.mock("next/navigation", () => ({ useRouter: () => ({ push: mocks.push }) }));
vi.mock("@vitarish/clinic", async () => {
  const actual =
    await vi.importActual<typeof import("@vitarish/clinic")>(
      "@vitarish/clinic",
    );
  return {
    ...actual,
    useClinicListDashboardAppointments: mocks.appointments,
  };
});

import { HomeDashboard } from "@/features/home/components/HomeDashboard";

describe("HomeDashboard", () => {
  it("renders server-owned appointments and opens the patient profile", async () => {
    mocks.appointments.mockReturnValue({
      data: [
        {
          reservationId: "reservation-1",
          status: 1,
          date: "2026-08-27",
          patientId: "12345678-1234-1234-1234-123456789012",
          patientDisplayName: "Sara Ahmadi",
          serviceId: null,
          serviceDisplayName: "Consultation",
          startTime: "09:30:00",
          timeZone: "UTC",
        },
      ],
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });

    const user = userEvent.setup();
    renderWithLocalization(<HomeDashboard />, { locale: "en" });

    expect(mocks.appointments).toHaveBeenCalled();
    expect(screen.getByRole("heading", { name: "Appointment review" })).toBeInTheDocument();
    expect(screen.getAllByRole("button", { name: "View profile", hidden: true })).toHaveLength(1);
    expect(screen.getAllByRole("button", { name: "New prescription", hidden: true })).toHaveLength(1);
    expect(screen.getAllByRole("button", { name: "New note", hidden: true })).toHaveLength(1);
    expect(screen.getByText("Confirmed")).toBeInTheDocument();
    expect(screen.queryByText("Time")).not.toBeInTheDocument();
    expect(screen.queryByText("Schedule is current")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "View profile" }));
    expect(mocks.push).toHaveBeenCalledWith("/patients/12345678-1234-1234-1234-123456789012");
  });

  it("opens a month view and moves the appointment query by one day", async () => {
    mocks.appointments.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });

    const user = userEvent.setup();
    renderWithLocalization(<HomeDashboard />, { locale: "en" });

    const initialRange = mocks.appointments.mock.calls.at(-1)?.[0];

    await user.click(screen.getByRole("button", { name: "Previous" }));

    const previousRange = mocks.appointments.mock.calls.at(-1)?.[0];
    expect(previousRange.dateFrom).toBe(
      parseDate(initialRange.dateFrom).subtract({ days: 1 }).toString(),
    );
    expect(previousRange.dateTo).toBe(previousRange.dateFrom);

    await user.click(screen.getByRole("button", { name: "Next" }));

    const nextRange = mocks.appointments.mock.calls.at(-1)?.[0];
    expect(nextRange.dateFrom).toBe(initialRange.dateFrom);
    expect(nextRange.dateTo).toBe(initialRange.dateTo);

    await user.click(screen.getByRole("button", { name: /Open date picker/ }));

    expect(screen.getByRole("grid")).toBeInTheDocument();
    expect(screen.getAllByRole("gridcell").length).toBeGreaterThan(7);
    expect(screen.getByRole("button", { name: "Previous month" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Next month" })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Select year" }));

    expect(screen.getAllByRole("button", { name: /^\d{4}$/ }).length).toBeGreaterThan(0);
  });

  it("filters appointments by status and service and clears the selections", async () => {
    mocks.appointments.mockReturnValue({
      data: [
        {
          reservationId: "reservation-confirmed",
          status: 1,
          date: "2026-08-27",
          patientId: "12345678-1234-1234-1234-123456789012",
          patientDisplayName: "Sara Ahmadi",
          serviceId: "service-consultation",
          serviceDisplayName: "Consultation",
          startTime: "09:30:00",
          timeZone: "UTC",
        },
        {
          reservationId: "reservation-pending",
          status: 0,
          date: "2026-08-27",
          patientId: "22345678-1234-1234-1234-123456789012",
          patientDisplayName: "Reza Karimi",
          serviceId: "service-follow-up",
          serviceDisplayName: "Follow-up visit",
          startTime: "10:30:00",
          timeZone: "UTC",
        },
        {
          reservationId: "reservation-completed",
          status: 7,
          date: "2026-08-27",
          patientId: "32345678-1234-1234-1234-123456789012",
          patientDisplayName: "Mina Hosseini",
          serviceId: "service-follow-up",
          serviceDisplayName: "Follow-up visit",
          startTime: "11:30:00",
          timeZone: "UTC",
        },
      ],
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });

    const user = userEvent.setup();
    renderWithLocalization(<HomeDashboard />, { locale: "en" });

    await user.click(screen.getByRole("button", { name: "Filters" }));
    await user.click(screen.getByRole("button", { name: /Status/ }));
    await user.click(screen.getByRole("option", { name: "Confirmed" }));
    await user.keyboard("{Escape}");
    await user.keyboard("{Escape}");

    expect(screen.getAllByRole("button", { name: "New prescription", hidden: true })).toHaveLength(1);
    await user.click(screen.getByRole("button", { name: "Filters" }));
    await user.click(screen.getByRole("button", { name: /Service/ }));
    await user.click(screen.getByRole("option", { name: "Consultation" }));
    await user.keyboard("{Escape}");
    await user.keyboard("{Escape}");

    expect(screen.getAllByRole("button", { name: "New prescription", hidden: true })).toHaveLength(1);

    const filterButton = screen.getByRole("button", { name: "Filters", hidden: true });
    if (filterButton.getAttribute("aria-expanded") !== "true") {
      await user.click(filterButton);
    }
    expect(screen.getByRole("button", { name: "Clear all filters", hidden: true })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Clear all filters", hidden: true }));
    await user.keyboard("{Escape}");

    expect(screen.getAllByRole("button", { name: "New prescription", hidden: true })).toHaveLength(3);
  });

  it("uses culture-friendly calendar navigation icons in RTL", async () => {
    mocks.appointments.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });

    const user = userEvent.setup();
    renderWithLocalization(<HomeDashboard />, { locale: "fa" });

    await user.click(screen.getByRole("button", { name: /باز کردن انتخابگر تاریخ/ }));

    expect(screen.getByRole("button", { name: "ماه قبل" }).querySelector("i"))
      .toHaveClass("fa-chevron-right");
    expect(screen.getByRole("button", { name: "ماه بعد" }).querySelector("i"))
      .toHaveClass("fa-chevron-left");
  });
});
