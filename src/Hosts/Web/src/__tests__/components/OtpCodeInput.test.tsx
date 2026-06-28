import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { OtpCodeInput } from "@/components/auth/OtpCodeInput";

describe("OtpCodeInput", () => {
  it("should render with label", () => {
    render(<OtpCodeInput value="" onChange={() => {}} />);
    expect(screen.getByLabelText(/verification code/i)).toBeInTheDocument();
  });

  it("should call onChange with numeric input", async () => {
    const onChange = vi.fn();
    render(<OtpCodeInput value="" onChange={onChange} />);

    const input = screen.getByLabelText(/verification code/i);
    await userEvent.type(input, "123456");

    expect(onChange).toHaveBeenCalledTimes(6);
  });

  it("should filter non-numeric input", async () => {
    const onChange = vi.fn();
    render(<OtpCodeInput value="" onChange={onChange} />);

    const input = screen.getByLabelText(/verification code/i);
    await userEvent.type(input, "abc123");

    const numericCalls = onChange.mock.calls.filter(
      (call: unknown[]) => (call[0] as string).length > 0
    );
    expect(numericCalls).toHaveLength(3);
  });

  it("should handle Persian numerals", async () => {
    const onChange = vi.fn();
    render(<OtpCodeInput value="" onChange={onChange} />);

    const input = screen.getByLabelText(/verification code/i);
    await userEvent.type(input, "۱۲۳");

    expect(onChange).toHaveBeenCalledTimes(3);
  });
});
