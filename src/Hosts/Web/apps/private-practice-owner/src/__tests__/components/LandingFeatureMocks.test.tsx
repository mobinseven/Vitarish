import { screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { LandingFeatureMock } from "@/features/landing/LandingFeatureMocks";
import { renderWithLocalization } from "../localization/renderWithLocalization";

describe("LandingFeatureMock", () => {
  it("confirms a pending appointment in place", async () => {
    const user = userEvent.setup();
    renderWithLocalization(<LandingFeatureMock feature="appointments" />, { locale: "en" });

    expect(screen.getAllByText("Pending")).toHaveLength(2);
    await user.click(screen.getAllByRole("button", { name: "Confirm" })[0]);

    expect(screen.getAllByText("Pending")).toHaveLength(1);
    expect(screen.getAllByText("Confirmed")).toHaveLength(2);
  });

  it("moves a patient from expected to arrived when checked in", async () => {
    const user = userEvent.setup();
    renderWithLocalization(<LandingFeatureMock feature="checkin" />, { locale: "en" });
    const expected = screen.getByRole("region", { name: "Expected" });
    const arrived = screen.getByRole("region", { name: "Arrived" });

    await user.click(within(expected).getAllByRole("button", { name: "Check in" })[0]);

    expect(within(expected).queryByText("Patient A")).not.toBeInTheDocument();
    expect(within(arrived).getByText("Patient A")).toBeInTheDocument();
  });

  it("changes nothing when the AI suggestion is dismissed, and can show it again", async () => {
    const user = userEvent.setup();
    renderWithLocalization(<LandingFeatureMock feature="ai" />, { locale: "en" });

    await user.click(screen.getByRole("button", { name: "Dismiss" }));
    expect(screen.getByText("Suggestion discarded. Nothing was changed.")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Show again" }));
    expect(screen.getByRole("button", { name: "Use summary" })).toBeInTheDocument();
  });

  it("shows the template body for the template that was picked", async () => {
    const user = userEvent.setup();
    renderWithLocalization(<LandingFeatureMock feature="templates" />, { locale: "en" });

    await user.click(screen.getByRole("button", { name: "Insurance claim" }));

    expect(screen.getByText(/every submission stays traceable/)).toBeInTheDocument();
  });
});
