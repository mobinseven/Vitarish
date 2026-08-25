import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { OtpVerificationStep } from "../../components/OtpVerificationStep";
import { renderWithLocalization } from "../localization/renderWithLocalization";

vi.mock("@vitarish/ui", () => ({
  StatusAlert: ({
    message,
    type,
    className,
  }: {
    message: string;
    type?: string;
    className?: string;
  }) => (
    <div className={className} data-type={type}>
      {message}
    </div>
  ),
}));

let capturedHandler: ((code: string) => void) | null = null;

vi.mock("../../hooks/useOtpCodeCapture", () => ({
  useOtpCodeCapture: vi.fn(({ onCode }) => {
    capturedHandler = onCode;
  }),
}));

describe("OtpVerificationStep", () => {
  afterEach(() => cleanup());

  beforeEach(() => {
    capturedHandler = null;
  });

  function renderStep(
    overrides: Partial<React.ComponentProps<typeof OtpVerificationStep>> = {},
  ) {
    return renderWithLocalization(
      <OtpVerificationStep
        destinationLabel="user@example.test"
        code=""
        onCodeChange={vi.fn()}
        onVerify={vi.fn()}
        onReset={vi.fn()}
        onResend={vi.fn()}
        isLoading={false}
        expiresAt={new Date(Date.now() + 5 * 60 * 1000).toISOString()}
        {...overrides}
      />,
    );
  }

  it("renders shared destination, attempts, expiry, resend, and reset controls", () => {
    renderStep({
      remainingAttempts: 3,
      changeDestinationLabel: "Use a different email",
    });

    expect(
      screen.getByText(/code sent to user@example.test/i),
    ).toBeInTheDocument();
    expect(screen.getByText(/3 attempts remaining/i)).toBeInTheDocument();
    expect(screen.getByText(/expires in/i)).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /resend code/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /use a different email/i }),
    ).toBeInTheDocument();
  });

  it("enables verify only for complete manual codes", async () => {
    const { rerender } = renderStep({ code: "12345" });
    expect(screen.getByRole("button", { name: /verify/i })).toBeDisabled();

    rerender(
      <OtpVerificationStep
        destinationLabel="user@example.test"
        code="123456"
        onCodeChange={vi.fn()}
        onVerify={vi.fn()}
        onReset={vi.fn()}
        onResend={vi.fn()}
        isLoading={false}
        expiresAt={new Date(Date.now() + 5 * 60 * 1000).toISOString()}
      />,
    );

    expect(screen.getByRole("button", { name: /verify/i })).toBeEnabled();
  });

  it("auto-submits a captured code once", () => {
    const onCodeChange = vi.fn();
    const onVerify = vi.fn();
    renderStep({ onCodeChange, onVerify });

    capturedHandler?.("123456");
    capturedHandler?.("123456");

    expect(onCodeChange).toHaveBeenCalledWith("123456");
    expect(onVerify).toHaveBeenCalledTimes(1);
    expect(onVerify).toHaveBeenCalledWith("123456");
  });

  it("does not auto-submit manual entry", async () => {
    const onCodeChange = vi.fn();
    const onVerify = vi.fn();
    renderStep({ onCodeChange, onVerify });

    await userEvent.type(screen.getByLabelText(/code/i), "123456");

    expect(onCodeChange).toHaveBeenCalled();
    expect(onVerify).not.toHaveBeenCalled();
  });

  it("disables verification when expired", () => {
    renderStep({
      code: "123456",
      expiresAt: new Date(Date.now() - 1000).toISOString(),
    });

    expect(
      screen.getAllByText(/code expired; request a new code/i).length,
    ).toBeGreaterThan(0);
    expect(screen.getByRole("button", { name: /verify/i })).toBeDisabled();
  });

  it("calls resend and reset actions", async () => {
    const onResend = vi.fn();
    const onReset = vi.fn();
    renderStep({
      onResend,
      onReset,
      changeDestinationLabel: "Use a different number",
    });

    await userEvent.click(screen.getByRole("button", { name: /resend code/i }));
    await userEvent.click(
      screen.getByRole("button", { name: /use a different number/i }),
    );

    expect(onResend).toHaveBeenCalledTimes(1);
    expect(onReset).toHaveBeenCalledTimes(1);
  });

  it("shows resend success or failure through status messages", () => {
    const { rerender } = renderStep({ statusMessage: "A new code was sent." });
    expect(screen.getByText("A new code was sent.")).toBeInTheDocument();

    rerender(
      <OtpVerificationStep
        destinationLabel="user@example.test"
        code=""
        onCodeChange={vi.fn()}
        onVerify={vi.fn()}
        onReset={vi.fn()}
        onResend={vi.fn()}
        isLoading={false}
        errorMessage="Too many requests"
      />,
    );
    expect(screen.getAllByText("Too many requests").length).toBeGreaterThan(0);
  });
});
