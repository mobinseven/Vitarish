import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { LocalizedDateField } from "@/components/forms/LocalizedDateField";
import { useLocale } from "@react-aria/i18n";
import { renderWithLocalization } from "../localization/renderWithLocalization";

function ReactAriaLocaleProbe() {
  const { locale, direction } = useLocale();
  return <output aria-label="React Aria locale" data-locale={locale} data-direction={direction} />;
}

function getCalendarText() {
  const calendarGrid = screen.getByRole("grid");
  return calendarGrid.closest(".localized-date-field__popover")?.textContent ?? calendarGrid.textContent ?? "";
}

describe("LocalizedDateField", () => {
  it("bridges Persian app locale to React Aria", () => {
    renderWithLocalization(<ReactAriaLocaleProbe />, { locale: "fa" });
    expect(screen.getByLabelText("React Aria locale")).toHaveAttribute("data-locale", "fa-IR-u-ca-persian");
    expect(screen.getByLabelText("React Aria locale")).toHaveAttribute("data-direction", "rtl");
  });

  it("renders the label", () => {
    renderWithLocalization(<LocalizedDateField label="Date of birth" value="" onChange={vi.fn()} />, { locale: "en" });
    expect(screen.getByText("Date of birth")).toBeDefined();
  });

  it("renders with a pre-filled ISO date value", () => {
    renderWithLocalization(<LocalizedDateField label="Test date" value="2026-07-06" onChange={vi.fn()} />, { locale: "en" });
    expect(screen.getByText("Test date")).toBeDefined();
  });

  it("does not invoke onChange on mount", () => {
    const onChange = vi.fn();
    renderWithLocalization(<LocalizedDateField label="Test date" value="2026-07-06" onChange={onChange} />, { locale: "en" });
    expect(onChange).not.toHaveBeenCalled();
  });

  it("renders error when provided", () => {
    renderWithLocalization(<LocalizedDateField label="Test date" value="" onChange={vi.fn()} error="Required field" />, { locale: "en" });
    expect(screen.getByText("Required field")).toBeDefined();
  });

  it("renders description when provided and no error", () => {
    renderWithLocalization(<LocalizedDateField label="Test date" value="" onChange={vi.fn()} description="Help text" />, { locale: "en" });
    expect(screen.getByText("Help text")).toBeDefined();
  });

  it("hides description when error is present", () => {
    renderWithLocalization(
      <LocalizedDateField label="Test date" value="" onChange={vi.fn()} description="Help text" error="Required" />,
      { locale: "en" }
    );
    expect(screen.queryByText("Help text")).toBeNull();
    expect(screen.getByText("Required")).toBeDefined();
  });

  it("forwards caller className while preserving the default field width", () => {
    renderWithLocalization(<LocalizedDateField label="Test date" value="" onChange={vi.fn()} className="mt-2" />, { locale: "en" });
    const datePicker = document.querySelector(".w-full.mt-2");
    expect(datePicker).not.toBeNull();
    expect(datePicker).toHaveClass("w-full", "mt-2");
  });

  it("does not expose DateValue in public props", () => {
    const onChange = vi.fn();
    renderWithLocalization(<LocalizedDateField label="Test date" value="" onChange={onChange} />, { locale: "en" });
    expect(typeof onChange.mock.calls[0]?.[0]).not.toBe("object");
  });

  it("renders Persian calendar labels instead of English Gregorian labels for Persian locale", async () => {
    const user = userEvent.setup();
    renderWithLocalization(<LocalizedDateField label="Test date" value="2026-07-06" onChange={vi.fn()} />, { locale: "fa" });

    await user.click(screen.getByRole("button"));

    const calendarText = getCalendarText();
    expect(calendarText).toContain("تیر");
    expect(calendarText).not.toMatch(/July|Sun|Mon|Tue|Wed|Thu|Fri|Sat|Su|Mo|Tu|We|Th|Fr|Sa/i);
  });

  it("keeps the popover width independent from the full-width field", async () => {
    const user = userEvent.setup();
    renderWithLocalization(<LocalizedDateField label="Test date" value="2026-07-06" onChange={vi.fn()} />, { locale: "en" });

    await user.click(screen.getByRole("button"));

    const popover = document.querySelector(".localized-date-field__popover");
    expect(popover).not.toBeNull();
    expect(popover).toHaveClass("!min-w-0", "w-max", "max-w-[calc(100vw-2rem)]", "p-2");
  });

  it("does not hard-code Gregorian placeholder segments", () => {
    renderWithLocalization(<LocalizedDateField label="Test date" value="" onChange={vi.fn()} />, { locale: "fa" });

    expect(document.body.textContent ?? "").not.toMatch(/\b(yyyy|mm|dd)\b/i);
    expect(screen.queryByText(/^(yyyy|mm|dd)$/i)).toBeNull();
  });
});
