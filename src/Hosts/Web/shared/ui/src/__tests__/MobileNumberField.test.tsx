import { type ComponentProps } from "react";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { MobileNumberField } from "../inputs/MobileNumberField";

afterEach(() => cleanup());

function renderField(overrides: Partial<ComponentProps<typeof MobileNumberField>> = {}) {
  const onChange = vi.fn();
  const props = {
    value: "",
    onChange,
    label: "Mobile number",
    hint: "Mobile number, for example 0912 345 6789.",
    ...overrides,
  };
  render(<MobileNumberField {...props} />);
  return { onChange, props };
}

describe("MobileNumberField", () => {
  it("shows the +98 prefix for empty and national input and wires the hint as its description", () => {
    renderField();
    const input = screen.getByRole("textbox", { name: "Mobile number" });

    expect(screen.getByText("+98")).toBeInTheDocument();
    expect(input).toHaveAccessibleDescription("Mobile number, for example 0912 345 6789.");
    expect(input).toHaveAttribute("inputmode", "tel");
    expect(input).toHaveAttribute("autocomplete", "tel-national");
    expect(input).toHaveAttribute("dir", "ltr");
  });

  it("sends local, Persian-digit, and spaced input exactly as typed apart from trimming", () => {
    const { onChange } = renderField();
    const input = screen.getByRole("textbox", { name: "Mobile number" });

    fireEvent.change(input, { target: { value: "0912 345 6789" } });
    expect(onChange).toHaveBeenLastCalledWith("0912 345 6789");

    fireEvent.change(input, { target: { value: "۰۹۱۲۳۴۵۶۷۸۹" } });
    expect(onChange).toHaveBeenLastCalledWith("۰۹۱۲۳۴۵۶۷۸۹");

    fireEvent.change(input, { target: { value: "  09123456789  " } });
    expect(onChange).toHaveBeenLastCalledWith("09123456789");
  });

  it("keeps pasted international input intact and hides the +98 prefix", () => {
    renderField({ value: "0098 912 345 6789" });
    expect(screen.getByRole("textbox", { name: "Mobile number" })).toHaveValue("0098 912 345 6789");
    expect(screen.queryByText("+98")).not.toBeInTheDocument();

    cleanup();
    renderField({ value: "+14155552671" });
    expect(screen.getByRole("textbox", { name: "Mobile number" })).toHaveValue("+14155552671");
    expect(screen.queryByText("+98")).not.toBeInTheDocument();
  });

  it("renders an existing Iranian E.164 number as the national part next to the prefix", () => {
    renderField({ value: "+989123456789" });

    expect(screen.getByRole("textbox", { name: "Mobile number" })).toHaveValue("9123456789");
    expect(screen.getByText("+98")).toBeInTheDocument();
  });

  it("announces a localized error and marks the input invalid", () => {
    renderField({ error: "Enter a valid mobile number." });
    const input = screen.getByRole("textbox", { name: "Mobile number" });

    expect(screen.getByRole("alert")).toHaveTextContent("Enter a valid mobile number.");
    expect(input).toHaveAttribute("aria-invalid", "true");
    expect(input).toHaveAccessibleDescription(
      "Mobile number, for example 0912 345 6789. Enter a valid mobile number.",
    );
  });
});
