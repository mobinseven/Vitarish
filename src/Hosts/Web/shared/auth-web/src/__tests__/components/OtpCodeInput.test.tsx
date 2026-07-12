import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, fireEvent, screen } from "@testing-library/react";
import { OtpCodeInput } from "../../components/OtpCodeInput";
import { renderWithLocalization } from "../localization/renderWithLocalization";

describe("OtpCodeInput", () => {
  afterEach(() => cleanup());
  it("keeps browser OTP autofill, numeric keyboard, and expected length attributes", () => {
    renderWithLocalization(<OtpCodeInput value="" onChange={() => {}} />);

    const input = screen.getByLabelText(/code/i);

    expect(input).toHaveAttribute("autocomplete", "one-time-code");
    expect(input).toHaveAttribute("inputmode", "numeric");
    expect(input).toHaveAttribute("maxlength", "6");
    expect(input).toHaveAttribute("pattern", "[0-9]{6}");
  });

  it("delegates normalization for mixed pasted content", async () => {
    const onChange = vi.fn();
    renderWithLocalization(<OtpCodeInput value="" onChange={onChange} />);

    const input = screen.getByLabelText(/code/i);
    fireEvent.change(input, { target: { value: "a1 ۲-٣b456789" } });

    expect(onChange).toHaveBeenCalledWith("123456");
  });

  it("uses the provided expected length", async () => {
    const onChange = vi.fn();
    renderWithLocalization(<OtpCodeInput value="" onChange={onChange} expectedLength={4} />);

    const input = screen.getByLabelText(/code/i);
    fireEvent.change(input, { target: { value: "123456" } });

    expect(input).toHaveAttribute("maxlength", "4");
    expect(input).toHaveAttribute("pattern", "[0-9]{4}");
    expect(onChange).toHaveBeenCalledWith("1234");
  });
});
