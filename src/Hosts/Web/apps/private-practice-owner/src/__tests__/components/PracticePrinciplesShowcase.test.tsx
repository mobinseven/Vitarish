import { fireEvent, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { PracticePrinciplesShowcase } from "@/features/landing/PracticePrinciplesShowcase";
import { renderWithLocalization } from "../localization/renderWithLocalization";

describe("PracticePrinciplesShowcase", () => {
  it("starts with Gather expanded", () => {
    renderWithLocalization(<PracticePrinciplesShowcase />, { locale: "en" });

    expect(
      screen.getByText(
        "Patient updates, appointments, provider activity, and office tasks arrive in a shared operating view.",
      ),
    ).toBeVisible();
    expect(screen.getByLabelText("Product preview placeholder")).toBeInTheDocument();
  });

  it("keeps one principle expanded and updates the placeholder", () => {
    renderWithLocalization(<PracticePrinciplesShowcase />, { locale: "en" });

    fireEvent.click(screen.getByRole("button", { name: /understand/i }));

    expect(
      screen.getByText(
        "Vitarish keeps the clinical story legible, surfacing progress and gaps without making autonomous decisions.",
      ),
    ).toBeVisible();
    expect(
      screen.getByText(
        "Patient updates, appointments, provider activity, and office tasks arrive in a shared operating view.",
      ),
    ).not.toBeVisible();
  });

  it("supports keyboard activation of a principle", async () => {
    renderWithLocalization(<PracticePrinciplesShowcase />, { locale: "en" });
    const coordinate = screen.getByRole("button", { name: /coordinate/i });
    const user = userEvent.setup();

    await user.click(coordinate);

    expect(
      screen.getByText(
        "Assign the next meaningful action to the right provider or team member and keep the agenda current.",
      ),
    ).toBeVisible();
  });
});
