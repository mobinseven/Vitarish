import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { HomeDashboard } from "@/features/home/components/HomeDashboard";
import { renderWithLocalization } from "../localization/renderWithLocalization";

describe("HomeDashboard", () => {
  it("renders primary, secondary, and tertiary stub buttons", () => {
    renderWithLocalization(<HomeDashboard />, { locale: "en" });

    expect(screen.getByRole("button", { name: "Primary" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Secondary" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Tertiary" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Info" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Highlight" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Support" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Emphasis" })).toBeInTheDocument();
  });
});
