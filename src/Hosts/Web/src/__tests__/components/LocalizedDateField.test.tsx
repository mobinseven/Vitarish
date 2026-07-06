import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { LocalizedDateField } from "@/components/forms/LocalizedDateField";
import { I18nProvider } from "react-aria-components";

function renderWithI18n(ui: React.ReactNode) {
  return render(<I18nProvider locale="en-US-u-ca-gregory">{ui}</I18nProvider>);
}

describe("LocalizedDateField", () => {
  it("renders the label", () => {
    renderWithI18n(
      <LocalizedDateField label="Date of birth" value="" onChange={vi.fn()} />
    );
    expect(screen.getByText("Date of birth")).toBeDefined();
  });

  it("renders with a pre-filled ISO date value", () => {
    renderWithI18n(
      <LocalizedDateField label="Test date" value="2026-07-06" onChange={vi.fn()} />
    );
    expect(screen.getByText("Test date")).toBeDefined();
  });

  it("does not invoke onChange on mount", () => {
    const onChange = vi.fn();
    renderWithI18n(
      <LocalizedDateField label="Test date" value="2026-07-06" onChange={onChange} />
    );
    expect(onChange).not.toHaveBeenCalled();
  });

  it("renders error when provided", () => {
    renderWithI18n(
      <LocalizedDateField label="Test date" value="" onChange={vi.fn()} error="Required field" />
    );
    expect(screen.getByText("Required field")).toBeDefined();
  });

  it("renders description when provided and no error", () => {
    renderWithI18n(
      <LocalizedDateField label="Test date" value="" onChange={vi.fn()} description="Help text" />
    );
    expect(screen.getByText("Help text")).toBeDefined();
  });

  it("hides description when error is present", () => {
    renderWithI18n(
      <LocalizedDateField
        label="Test date"
        value=""
        onChange={vi.fn()}
        description="Help text"
        error="Required"
      />
    );
    expect(screen.queryByText("Help text")).toBeNull();
    expect(screen.getByText("Required")).toBeDefined();
  });

  it("does not expose DateValue in public props", () => {
    const onChange = vi.fn();
    renderWithI18n(
      <LocalizedDateField label="Test date" value="" onChange={onChange} />
    );
    expect(typeof onChange.mock.calls[0]?.[0]).not.toBe("object");
  });
});
