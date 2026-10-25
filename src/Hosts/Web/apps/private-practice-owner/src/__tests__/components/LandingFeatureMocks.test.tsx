import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { LandingFeatureMock } from "@/features/landing/LandingFeatureMocks";
import { renderWithLocalization } from "../localization/renderWithLocalization";

describe("LandingFeatureMock", () => {
  it("books the opening the patient picks and can replay the conversation", async () => {
    const user = userEvent.setup();
    renderWithLocalization(<LandingFeatureMock feature="assistant" />, { locale: "en" });

    await user.click(screen.getByRole("button", { name: "Thursday 4:00 PM" }));

    expect(screen.getByText("Booked")).toBeInTheDocument();
    expect(screen.getByText(/booked with Dr\. Rahimi on Thursday 4:00 PM/)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Tuesday 10:30 AM" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Start over" }));
    expect(screen.queryByText("Booked")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Tuesday 10:30 AM" })).toBeInTheDocument();
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
