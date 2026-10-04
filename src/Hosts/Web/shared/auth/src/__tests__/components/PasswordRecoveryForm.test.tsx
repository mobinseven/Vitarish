import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { LocalizationProvider } from "@vitarish/localization";
import { PasswordRecoveryForm } from "../../components/PasswordRecoveryForm";

const mutations = vi.hoisted(() => ({
  begin: vi.fn(),
  complete: vi.fn(),
}));

vi.mock("../../api/generated/authentication/authentication", () => ({
  useAuthenticationPasswordRecoveryBegin: () => ({ mutateAsync: mutations.begin, isPending: false }),
  useAuthenticationPasswordRecoveryComplete: () => ({ mutateAsync: mutations.complete, isPending: false }),
}));

vi.mock("@vitarish/ui", () => ({
  FaIcon: () => null,
  StatusAlert: ({ message }: { message: string }) => <p role="alert">{message}</p>,
  MobileNumberField: ({ label, value, onChange }: { label: string; value: string; onChange: (next: string) => void }) => (
    <label>
      {label}
      <input value={value} onChange={(event) => onChange(event.target.value)} />
    </label>
  ),
}));

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

function wrapper({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>
      <LocalizationProvider
        locale="en"
        messages={{
          auth: {
            recovery: {
              title: "Reset your password",
              subtitle: "Verify your contact",
              instructions: "Choose where to receive a verification code.",
              channelLabel: "Recovery method",
              emailChannel: "Email",
              smsChannel: "Text message",
              emailLabel: "Email address",
              phoneLabel: "Phone number",
              sendCodeAction: "Send verification code",
              codeSentNotice: "If an eligible account exists, a verification code has been sent.",
              codeLabel: "Verification code",
              newPasswordLabel: "New password",
              confirmPasswordLabel: "Confirm new password",
              resetPasswordAction: "Reset password",
              passwordMismatch: "The passwords do not match.",
              passwordPolicyError: "Choose a password that meets your account's security policy.",
              invalidCode: "The verification code is invalid. Check it and try again.",
              codeExpired: "The verification code has expired. Start over to request a new one.",
              tooManyAttempts: "Too many attempts. Wait before trying again.",
              requestFailed: "Password recovery could not be completed. Please try again.",
              success: "Your password has been reset.",
              backToLogin: "Back to sign in",
              startOverAction: "Start over",
            },
          },
        }}
      >
        {children}
      </LocalizationProvider>
    </QueryClientProvider>
  );
}

describe("PasswordRecoveryForm", () => {
  beforeEach(() => {
    cleanup();
    queryClient.clear();
    mutations.begin.mockReset().mockResolvedValue({ challengeId: "challenge-id" });
    mutations.complete.mockReset().mockResolvedValue({ succeeded: true });
  });
  afterEach(() => cleanup());

  it("keeps a generic confirmation and completes email recovery in one submission", async () => {
    render(<PasswordRecoveryForm />, { wrapper });
    fireEvent.change(screen.getByLabelText("Email address"), { target: { value: "owner@example.test" } });
    fireEvent.click(screen.getByRole("button", { name: "Send verification code" }));

    await screen.findByText("If an eligible account exists, a verification code has been sent.");
    expect(mutations.begin).toHaveBeenCalledWith({
      data: { channel: 0, contact: "owner@example.test" },
    });

    fireEvent.change(screen.getByLabelText("Verification code"), { target: { value: "123456" } });
    fireEvent.change(screen.getByLabelText("New password"), { target: { value: "New-password-2!" } });
    fireEvent.change(screen.getByLabelText("Confirm new password"), { target: { value: "New-password-2!" } });
    fireEvent.click(screen.getByRole("button", { name: "Reset password" }));

    await screen.findByText("Your password has been reset.");
    expect(mutations.complete).toHaveBeenCalledWith({
      data: { challengeId: "challenge-id", code: "123456", newPassword: "New-password-2!" },
    });
  });

  it("sends SMS recovery and rejects mismatched passwords before completion", async () => {
    render(<PasswordRecoveryForm />, { wrapper });
    fireEvent.click(screen.getByRole("button", { name: "Text message" }));
    fireEvent.change(screen.getByLabelText("Phone number"), { target: { value: "+15555550123" } });
    fireEvent.click(screen.getByRole("button", { name: "Send verification code" }));

    await waitFor(() => expect(mutations.begin).toHaveBeenCalledWith({
      data: { channel: 1, contact: "+15555550123" },
    }));
    fireEvent.change(screen.getByLabelText("Verification code"), { target: { value: "654321" } });
    fireEvent.change(screen.getByLabelText("New password"), { target: { value: "New-password-2!" } });
    fireEvent.change(screen.getByLabelText("Confirm new password"), { target: { value: "Different-password-3!" } });
    fireEvent.click(screen.getByRole("button", { name: "Reset password" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("The passwords do not match.");
    expect(mutations.complete).not.toHaveBeenCalled();
  });
});
