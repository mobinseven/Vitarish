import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import { InlinePhoneSignIn } from "../../components/InlinePhoneSignIn";
import type { AuthNavigationOptions } from "../../hooks/navigation";

vi.mock("@vitarish/ui", () => ({
  MobileNumberField: () => <input aria-label="phone" />,
  StatusAlert: ({ message }: { message: string }) => <div>{message}</div>,
}));

let phoneLoginOptions: AuthNavigationOptions | undefined;
vi.mock("../../hooks/usePhoneLogin", () => ({
  usePhoneLogin: (options: AuthNavigationOptions) => {
    phoneLoginOptions = options;
    return {
      step: "phone",
      phoneNumber: "",
      begin: vi.fn(),
      complete: vi.fn(),
      resend: vi.fn(),
      reset: vi.fn(),
      isLoading: false,
      error: null,
      errorMessage: null,
      statusCode: null,
      remainingAttempts: null,
      expiresAt: null,
    };
  },
}));

afterEach(() => {
  cleanup();
  phoneLoginOptions = undefined;
});

describe("InlinePhoneSignIn", () => {
  it("continues the conversation instead of leaving the page when the code is accepted", () => {
    const onSignedIn = vi.fn();
    const before = window.location.href;
    render(<InlinePhoneSignIn title="Your phone number is enough." onSignedIn={onSignedIn} />);

    expect(screen.getByText("Your phone number is enough.")).toBeVisible();
    phoneLoginOptions?.navigate?.("/after-sign-in");

    expect(onSignedIn).toHaveBeenCalledTimes(1);
    expect(window.location.href).toBe(before);
  });
});
