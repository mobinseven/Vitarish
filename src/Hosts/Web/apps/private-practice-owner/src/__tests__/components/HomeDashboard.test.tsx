import { screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { HomeDashboard } from "@/features/home/components/HomeDashboard";
import { renderWithLocalization } from "../localization/renderWithLocalization";

vi.mock("@vitarish/auth-web", () => ({
  useAuthSession: () => ({
    user: {
      displayName: "Raw Name",
      email: "raw@example.com",
      phoneNumber: null,
    },
    displayProfile: {
      displayLabel: "Avery Stone",
      givenName: "Avery",
      familyName: "Stone",
      initials: "AS",
      contactLabel: "avery@example.com",
    },
    isLoading: false,
  }),
}));

describe("HomeDashboard", () => {
  it("greets the signed-in user and links to the Knowledge demo", () => {
    renderWithLocalization(<HomeDashboard />, { locale: "en" });

    expect(
      screen.getByRole("heading", { name: /welcome back, avery/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("link", { name: /open knowledge/i }),
    ).toHaveAttribute("href", "/knowledge");
    expect(
      screen.getByRole("link", { name: /start with knowledge/i }),
    ).toHaveAttribute("href", "/knowledge");
  });
});
