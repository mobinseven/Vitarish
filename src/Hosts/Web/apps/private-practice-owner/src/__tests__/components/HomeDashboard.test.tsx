import userEvent from "@testing-library/user-event";
import { screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { renderWithLocalization } from "../localization/renderWithLocalization";

const mocks = vi.hoisted(() => ({
  push: vi.fn(),
  appointments: vi.fn(),
  surgeries: vi.fn(),
  credentials: vi.fn(),
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
    useClinicListDashboardSurgeries: mocks.surgeries,
    useInsuranceProviderCredentials: mocks.credentials,
  };
});

import { HomeDashboard } from "@/features/home/components/HomeDashboard";

describe("HomeDashboard", () => {
  it("renders server-owned records and routes dashboard actions", async () => {
    mocks.appointments.mockReturnValue({
      data: [
        {
          reservationId: "reservation-1",
          status: 2,
          date: "2026-08-27",
          patientId: null,
          patientDisplayName: null,
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
    mocks.surgeries.mockReturnValue({
      data: [
        {
          surgeryId: "surgery-1",
          patientId: "patient-1",
          patientDisplayName: null,
          surgeryName: "Appendectomy",
          status: "Planned",
          scheduledAt: "2026-08-28T10:00:00Z",
        },
      ],
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });
    mocks.credentials.mockReturnValue({
      context: { isLoading: false, error: null, refetch: vi.fn() },
      credentials: {
        data: [
          { credentialId: "credential-1", providerKey: "TAMIN", status: 1 },
        ],
        isLoading: false,
        error: null,
        refetch: vi.fn(),
      },
    });

    const user = userEvent.setup();
    renderWithLocalization(<HomeDashboard />, { locale: "en" });

    expect(mocks.appointments).toHaveBeenCalled();
    expect(mocks.surgeries).toHaveBeenCalled();
    expect(mocks.credentials).toHaveBeenCalled();
    expect(screen.getByText(/Reservation reservat/)).toBeInTheDocument();
    expect(screen.getByText("Appendectomy")).toBeInTheDocument();
    expect(screen.getByText("TAMIN")).toBeInTheDocument();
    expect(screen.queryByText("Maya Ellis")).not.toBeInTheDocument();
    expect(screen.getAllByRole("button", { name: "Previous" })).toHaveLength(2);
    expect(screen.getAllByRole("button", { name: "Next" })).toHaveLength(4);

    await user.click(screen.getByRole("button", { name: "Add appointment" }));
    expect(mocks.push).toHaveBeenCalledWith("/appointments");
  });
});
