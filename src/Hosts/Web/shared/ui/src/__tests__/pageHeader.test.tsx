import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { PageHeader } from "../index";

describe("PageHeader", () => {
  it("renders one h1 with the title, details and actions", () => {
    render(<PageHeader icon="clock" title="Shifts" details="Weekly hours" actions={<button type="button">New</button>} />);

    expect(screen.getAllByRole("heading", { level: 1 })).toHaveLength(1);
    expect(screen.getByRole("heading", { level: 1 }).textContent).toBe("Shifts");
    expect(screen.getByText("Weekly hours")).toBeTruthy();
    expect(screen.getByRole("button", { name: "New" })).toBeTruthy();
    expect(screen.queryByRole("navigation")).toBeNull();
  });

  it("offers the ancestors as a trail and a single back control to the parent", () => {
    const openSettings = vi.fn();
    const openShifts = vi.fn();
    render(
      <PageHeader
        icon="clock"
        title="Morning shift"
        back={{
          navigationLabel: "Page location",
          backLabel: "Back to Shifts",
          currentLabel: "Morning shift",
          crumbs: [
            { id: "settings", label: "Settings", onPress: openSettings },
            { id: "shifts", label: "Shifts", onPress: openShifts },
          ],
        }}
      />,
    );

    expect(screen.getByRole("navigation", { name: "Page location" })).toBeTruthy();
    fireEvent.click(screen.getByText("Settings"));
    expect(openSettings).toHaveBeenCalledTimes(1);

    fireEvent.click(screen.getByRole("button", { name: "Back to Shifts" }));
    expect(openShifts).toHaveBeenCalledTimes(1);
  });
});
