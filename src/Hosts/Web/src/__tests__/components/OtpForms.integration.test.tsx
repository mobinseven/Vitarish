import { render, screen } from "@testing-library/react";
import { PhoneOtpForm } from "@/components/auth/PhoneOtpForm";
import { EmailOtpForm } from "@/components/auth/EmailOtpForm";

const phoneLogin = vi.fn();
const emailLogin = vi.fn();
const otpStep = vi.fn((props: { destinationLabel: string; onResend: () => void }) => (
  <div data-testid="shared-otp-step">
    Shared OTP for {props.destinationLabel}
    <button type="button" onClick={props.onResend}>Resend Code</button>
  </div>
));

vi.mock("@/hooks/auth/usePhoneLogin", () => ({
  usePhoneLogin: () => phoneLogin(),
}));

vi.mock("@/hooks/auth/useEmailLogin", () => ({
  useEmailLogin: () => emailLogin(),
}));

vi.mock("@/components/auth/OtpVerificationStep", () => ({
  OtpVerificationStep: (props: { destinationLabel: string; onResend: () => void }) => otpStep(props),
}));

describe("phone and email OTP forms", () => {
  beforeEach(() => {
    otpStep.mockClear();
    phoneLogin.mockReturnValue({
      step: "code",
      phoneNumber: "+15551234567",
      begin: vi.fn(),
      complete: vi.fn(),
      resend: vi.fn(),
      reset: vi.fn(),
      isLoading: false,
      errorMessage: null,
      statusMessage: null,
      remainingAttempts: 4,
      expiresAt: new Date(Date.now() + 5 * 60 * 1000).toISOString(),
    });
    emailLogin.mockReturnValue({
      step: "code",
      email: "user@example.test",
      begin: vi.fn(),
      complete: vi.fn(),
      resend: vi.fn(),
      reset: vi.fn(),
      isLoading: false,
      errorMessage: null,
      statusMessage: null,
      remainingAttempts: 4,
      expiresAt: new Date(Date.now() + 5 * 60 * 1000).toISOString(),
    });
  });

  it("renders the shared OTP step for phone verification", () => {
    render(<PhoneOtpForm />);

    expect(screen.getByTestId("shared-otp-step")).toHaveTextContent("+15551234567");
    expect(otpStep).toHaveBeenCalledWith(expect.objectContaining({ destinationLabel: "+15551234567" }));
  });

  it("renders the shared OTP step for email verification", () => {
    render(<EmailOtpForm />);

    expect(screen.getByTestId("shared-otp-step")).toHaveTextContent("user@example.test");
    expect(otpStep).toHaveBeenCalledWith(expect.objectContaining({ destinationLabel: "user@example.test" }));
  });
});
