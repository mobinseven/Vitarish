import { render, type RenderOptions } from "@testing-library/react";
import type { ReactElement } from "react";
import {
  LocalizationProvider,
  type MessageCatalog,
} from "@vitarish/web-localization";

const messages: MessageCatalog = {
  auth: {
    login: {
      usernameOrEmailLabel: "Username or email",
      usernameOrEmailOptionalLabel: "Username or email (optional)",
      usernameOrEmailPlaceholder: "Enter your username or email",
      passwordLabel: "Password",
      passwordPlaceholder: "Enter your password",
      signInAction: "Sign in",
      otherMethods: "Other methods",
    },
    otp: {
      codeLabel: "Code",
      codePlaceholder: "{length}-digit code",
      codeExpired: "Code has expired",
      verifyAction: "Verify",
      resendAction: "Resend code",
      differentDestinationAction: "Use a different destination",
      codeSentTo: "Code sent to {destination}",
      attemptsRemaining: "{count} attempts remaining",
      emailLabel: "Email",
      emailPlaceholder: "you@example.com",
      phoneLabel: "Phone number",
      phonePlaceholder: "+1 555 0100",
      sendAction: "Send code",
      differentEmailAction: "Use a different email",
      differentPhoneAction: "Use a different phone",
      emailCodeResent: "A new code was sent to your email address.",
      phoneCodeResent: "A new code was sent to your phone number.",
    },
    passkey: { signIn: "Sign in with passkey" },
  },
};

export function renderWithLocalization(
  ui: ReactElement,
  options?: RenderOptions,
) {
  return render(ui, {
    ...options,
    wrapper: ({ children }) => (
      <LocalizationProvider locale="en" messages={messages}>
        {children}
      </LocalizationProvider>
    ),
  });
}
