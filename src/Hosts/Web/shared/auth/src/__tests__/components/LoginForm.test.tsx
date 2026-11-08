import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode } from "react";
import { http, HttpResponse } from "msw";
import { server } from "../server";
import { LoginForm } from "../../components/LoginForm";
import {
  authenticationLoginPassword,
  authenticationLoginPasswordOtpComplete,
  authenticationLoginPasswordOtpResend,
} from "../../api/generated/authentication/authentication";
import { generatedOperationRequestPattern } from "../generatedOperationRequestPattern";
import { LocalizationProvider } from "@vitarish/localization";

vi.mock("@vitarish/ui", () => ({
  StatusAlert: ({ message }: { message: string }) => <div>{message}</div>,
}));

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

function wrapper({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>
      <LocalizationProvider
        locale="en"
        messages={{
          auth: {
            login: {
              usernameOrEmailLabel: "Username or email",
              usernameOrEmailPlaceholder: "Enter",
              passwordLabel: "Password",
              passwordPlaceholder: "Enter",
              signInAction: "Sign in",
              otpLabel: "Verification code",
              otpVerifyAction: "Verify code",
            },
            otp: {
              codeLabel: "Verification code",
              codePlaceholder: "Enter {length}-digit code",
              codeSentLabel: "We sent a verification code to",
              verifyAction: "Verify code",
              resendAction: "Resend code",
              differentDestinationAction: "Use a different destination",
              attemptsRemaining: "{count} attempts remaining",
              codeExpired: "Code expired; request a new code",
            },
            challenge: {
              expiresIn: "Expires in {time}",
              expired: "Code expired; request a new code",
            },
            passwordLoginOtp: {
              emailStepDescription: "Enter the code sent to your email to finish signing in.",
              destinationFallback: "your verified contact method",
              returnToPasswordAction: "Return to password sign-in",
              codeResent: "A new sign-in code was sent.",
            },
            accountStatus: {
              disabled: "This account is disabled. Contact a platform administrator.",
              noAccess: "This account has no access level. Contact a platform administrator.",
            },
            recovery: { forgotPasswordAction: "Forgot password?" },
          },
        }}
      >
        {children}
      </LocalizationProvider>
    </QueryClientProvider>
  );
}

describe("LoginForm", () => {
  afterEach(() => cleanup());
  beforeEach(() => {
    cleanup();
    queryClient.clear();
  });

  it("should render username and password fields", () => {
    render(<LoginForm forgotPasswordHref="/login/forgot-password" />, { wrapper });

    expect(screen.getByLabelText(/username or email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /sign in/i }),
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /forgot password/i })).toHaveAttribute(
      "href",
      "/login/forgot-password",
    );
  });

  it("preserves the app continuation when opening password recovery", () => {
    render(<LoginForm forgotPasswordHref="/login/forgot-password" returnUrlSearchParams={new URLSearchParams({ returnUrl: "/continue?request=opaque" })} />, { wrapper });
    const destination = new URL(screen.getByRole("link", { name: /forgot password/i }).getAttribute("href")!, "https://auth.example.test");
    expect(destination.pathname).toBe("/login/forgot-password");
    expect(destination.searchParams.get("returnUrl")).toBe("/continue?request=opaque");
  });

  it("should render with correct input types", () => {
    render(<LoginForm />, { wrapper });

    const passwordInput = screen.getByLabelText(/password/i);
    expect(passwordInput).toHaveAttribute("type", "password");
  });

  it("uses a POST fallback so credentials cannot appear in the URL before hydration", () => {
    render(<LoginForm />, { wrapper });

    expect(screen.getByRole("button", { name: /sign in/i }).closest("form")).toHaveAttribute("method", "post");
  });

  it("shows the localized disabled-account message after a verified sign-in", async () => {
    server.use(
      http.post(
        generatedOperationRequestPattern(authenticationLoginPassword),
        () =>
          HttpResponse.json(
            {
              status: 403,
              title: "AccountDisabled",
              detail: "The account is disabled.",
              code: "account_disabled",
            },
            { status: 403 },
          ),
      ),
    );
    render(<LoginForm />, { wrapper });

    fireEvent.change(screen.getByLabelText(/username or email/i), { target: { value: "disabled@test.com" } });
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: "password123" } });
    await waitFor(() => expect(screen.getByRole("button", { name: /sign in/i })).toBeEnabled());
    fireEvent.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(screen.getByText("This account is disabled. Contact a platform administrator.")).toBeInTheDocument();
    });
  });

  it("shows the localized no-access message after a verified sign-in", async () => {
    server.use(
      http.post(
        /\/api\/modules\/authentication\/login\/password(?:\?|$)/,
        () =>
          HttpResponse.json(
            {
              status: 403,
              title: "NoAccess",
              detail: "The account has no access level.",
              code: "no_access",
            },
            { status: 403 },
          ),
      ),
    );
    render(<LoginForm />, { wrapper });

    fireEvent.change(screen.getByLabelText(/username or email/i), { target: { value: "noaccess@test.com" } });
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: "password123" } });
    await waitFor(() => expect(screen.getByRole("button", { name: /sign in/i })).toBeEnabled());
    fireEvent.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(screen.getByText("This account has no access level. Contact a platform administrator.")).toBeInTheDocument();
    });
  });

  it("uses the masked email target and shared resend/verify step after password sign-in", async () => {
    const maskedDestination = "s***@example.test";
    let resendBody: Record<string, unknown> | undefined;
    let verifyBody: Record<string, unknown> | undefined;
    server.use(
      http.post(
        generatedOperationRequestPattern(authenticationLoginPassword),
        () =>
          HttpResponse.json(
            {
              status: 409,
              title: "AdditionalStepRequired",
              detail: "Additional authentication required.",
              code: "additional_step_required",
              challengeId: "email-password-challenge",
              usesEmailSecondFactor: true,
              maskedDestination,
              challengeExpiresAt: new Date(Date.now() + 5 * 60 * 1000).toISOString(),
              remainingAttempts: 4,
            },
            { status: 409 },
          ),
      ),
      http.post(
        generatedOperationRequestPattern(authenticationLoginPasswordOtpResend),
        async ({ request }) => {
          resendBody = (await request.json()) as Record<string, unknown>;
          return HttpResponse.json({
            challengeId: "email-password-challenge",
            expiresAt: new Date(Date.now() + 5 * 60 * 1000).toISOString(),
            remainingAttempts: 4,
          });
        },
      ),
      http.post(
        generatedOperationRequestPattern(authenticationLoginPasswordOtpComplete),
        async ({ request }) => {
          verifyBody = (await request.json()) as Record<string, unknown>;
          return HttpResponse.json({
            requiresProfileCompletion: false,
          });
        },
      ),
    );

    render(<LoginForm />, { wrapper });
    fireEvent.change(screen.getByLabelText(/username or email/i), { target: { value: "security@example.test" } });
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: "password123" } });
    await waitFor(() => expect(screen.getByRole("button", { name: /sign in/i })).toBeEnabled());
    fireEvent.click(screen.getByRole("button", { name: /sign in/i }));

    expect(await screen.findByText("Enter the code sent to your email to finish signing in.")).toBeInTheDocument();
    expect(screen.getByText(maskedDestination)).toHaveAttribute("dir", "ltr");
    expect(screen.queryByText("security@example.test")).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: /resend code/i }));
    await waitFor(() => expect(resendBody).toEqual({ challengeId: "email-password-challenge" }));
    expect(screen.getByText(maskedDestination)).toBeInTheDocument();
    expect(screen.getByText("A new sign-in code was sent.")).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText("Verification code"), { target: { value: "123456" } });
    fireEvent.click(screen.getByRole("button", { name: /verify code/i }));
    await waitFor(() => expect(verifyBody).toEqual({
      challengeId: "email-password-challenge",
      code: "123456",
      returnUrl: null,
    }));
  });
});
