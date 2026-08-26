import userEvent from "@testing-library/user-event";
import { screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { renderWithLocalization } from "../localization/renderWithLocalization";

const mocks = vi.hoisted(() => ({
  push: vi.fn(),
  reservations: vi.fn(),
  surgeries: vi.fn(),
  insurance: vi.fn(),
}));

vi.mock("next/navigation", () => ({ useRouter: () => ({ push: mocks.push }) }));
vi.mock("@vitarish/clinic", async () => {
  const actual = await vi.importActual<typeof import("@vitarish/clinic")>("@vitarish/clinic");
  return { ...actual, useOwnerReservations: mocks.reservations, useClinicListSurgeries: mocks.surgeries, useInsuranceProviderSetup: mocks.insurance };
});

import { HomeDashboard } from "@/features/home/components/HomeDashboard";

describe("HomeDashboard", () => {
  it("renders server-owned records and routes dashboard actions", async () => {
    mocks.reservations.mockReturnValue({ data: [{ id: "reservation-1", status: 2, date: "2026-08-27T09:30:00Z" }], isLoading: false, error: null });
    mocks.surgeries.mockReturnValue({ data: [{ id: "surgery-1", patientId: "patient-1", name: "Appendectomy", scheduledAt: "2026-08-28T10:00:00Z" }], isLoading: false, error: null });
    mocks.insurance.mockReturnValue({ context: { isLoading: false, error: null }, credentials: { data: [{ credentialId: "credential-1", providerKey: "TAMIN", status: 1 }], isLoading: false, error: null } });

    const user = userEvent.setup();
    renderWithLocalization(<HomeDashboard />, { locale: "en" });

    expect(mocks.reservations).toHaveBeenCalled();
    expect(mocks.surgeries).toHaveBeenCalled();
    expect(mocks.insurance).toHaveBeenCalled();
    expect(screen.getByText(/Reservation reservat/)).toBeInTheDocument();
    expect(screen.getByText("Appendectomy")).toBeInTheDocument();
    expect(screen.getByText("TAMIN")).toBeInTheDocument();
    expect(screen.queryByText("Maya Ellis")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Add appointment" }));
    expect(mocks.push).toHaveBeenCalledWith("/appointments");
  });
});
