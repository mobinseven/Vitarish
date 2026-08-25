import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import ProcedureTemplateManagementRoute from "@/app/(owner)/(clinic-workspace)/settings/procedure-templates/page";

vi.mock("@vitarish/clinic", () => ({
  ProcedureTemplateManagementPanel: () => (
    <div data-testid="procedure-template-management-panel" />
  ),
}));

describe("ProcedureTemplateManagementRoute", () => {
  it("composes the shared procedure-template management panel", () => {
    render(<ProcedureTemplateManagementRoute />);
    expect(
      screen.getByTestId("procedure-template-management-panel"),
    ).toBeInTheDocument();
  });
});
