import userEvent from "@testing-library/user-event";
import { screen } from "@testing-library/react";
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
  it("renders server-owned appointments and routes to their reservation", async () => {
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
    expect(screen.getByText("Sara Ahmadi")).toBeInTheDocument();
    expect(screen.getByText("Confirmed")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "View file" }));
    expect(mocks.push).toHaveBeenCalledWith("/appointments/reservation-1");
  });
});
