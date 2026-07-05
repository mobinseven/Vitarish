import { fireEvent, screen } from "@testing-library/react";
import { OtpCodeInput } from "@/components/auth/OtpCodeInput";
import { renderWithLocalization } from "../localization/renderWithLocalization";

describe("OtpCodeInput", () => {
  it("keeps browser OTP autofill, numeric keyboard, and expected length attributes", () => {
    renderWithLocalization(<OtpCodeInput value="" onChange={() => {}} />, { locale: "en" });

    const input = screen.getByLabelText(/verification code/i);

    expect(input).toHaveAttribute("autocomplete", "one-time-code");
    expect(input).toHaveAttribute("inputmode", "numeric");
    expect(input).toHaveAttribute("maxlength", "6");
    expect(input).toHaveAttribute("pattern", "[0-9]{6}");
  });

  it("delegates normalization for mixed pasted content", async () => {
    const onChange = vi.fn();
    renderWithLocalization(<OtpCodeInput value="" onChange={onChange} />, { locale: "en" });

    const input = screen.getByLabelText(/verification code/i);
    fireEvent.change(input, { target: { value: "a1 ۲-٣b456789" } });

    expect(onChange).toHaveBeenCalledWith("123456");
  });

  it("uses the provided expected length", async () => {
    const onChange = vi.fn();
    renderWithLocalization(<OtpCodeInput value="" onChange={onChange} expectedLength={4} />, { locale: "en" });

    const input = screen.getByLabelText(/verification code/i);
    fireEvent.change(input, { target: { value: "123456" } });

    expect(input).toHaveAttribute("maxlength", "4");
    expect(input).toHaveAttribute("pattern", "[0-9]{4}");
    expect(onChange).toHaveBeenCalledWith("1234");
  });
});
