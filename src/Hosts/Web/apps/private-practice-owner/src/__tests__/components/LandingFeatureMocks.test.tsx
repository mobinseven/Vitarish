import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { LandingFeatureMock } from "@/features/landing/LandingFeatureMocks";
import { renderWithLocalization } from "../localization/renderWithLocalization";

describe("LandingFeatureMock", () => {
  it("confirms the prepared surgery plan and can reset it", async () => {
    const user = userEvent.setup();
    renderWithLocalization(<LandingFeatureMock feature="assistant" />, { locale: "en" });

    expect(screen.getByText("For your review")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Confirm and schedule" }));

    expect(screen.getByText("Scheduled")).toBeInTheDocument();
    expect(screen.getByText("The surgery is on the schedule.")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Confirm and schedule" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Try again" }));
    expect(screen.getByText("For your review")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Confirm and schedule" })).toBeInTheDocument();
  });

  it("confirms a pending booking, then checks the patient in", async () => {
    const user = userEvent.setup();
    renderWithLocalization(<LandingFeatureMock feature="bookings" />, { locale: "en" });

    expect(screen.getAllByText("Pending")).toHaveLength(2);
    await user.click(screen.getAllByRole("button", { name: "Confirm" })[0]);
    expect(screen.getAllByText("Pending")).toHaveLength(1);
    expect(screen.getAllByText("Confirmed")).toHaveLength(2);

    await user.click(screen.getAllByRole("button", { name: "Check in" })[0]);
    expect(screen.getByText("Checked in")).toBeInTheDocument();
    expect(screen.getAllByText("Confirmed")).toHaveLength(1);
  });

  it("shows sample times in the Persian locale's digits", () => {
    renderWithLocalization(<LandingFeatureMock feature="bookings" />, { locale: "fa" });

    expect(screen.getByText(/۹:۰۰/)).toBeInTheDocument();
  });
});
