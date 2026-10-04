import { cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@vitarish/localization";
import SecuritySettingsPage from "@/app/settings/security/page";
import { getMessages } from "@/localization/messages";

const state = vi.hoisted(() => ({
  sessionRefetch: vi.fn(),
  authenticationRefetch: vi.fn(),
  sessionPut: vi.fn(),
  authenticationPut: vi.fn(),
  sessionPolicy: {
    maximumConcurrentSessions: 1,
    concurrentSessionsFunctionallyRequired: false,
    defaultInactivityTimeoutMinutes: 30,
    allowedSessionStartCidrs: [] as string[],
    version: 4,
  },
  authenticationPolicy: {
    minimumPasswordLength: 12,
    requirePasswordLowercase: true,
    requirePasswordUppercase: true,
    requirePasswordNumber: true,
    requirePasswordSpecialCharacter: true,
    failedAttemptThreshold: 5,
    failedAttemptDenialDurationSeconds: 900,
    failedAttemptResponse: "Deny",
    enabledAuthenticationMechanisms: ["Password", "Phone"],
    permittedPreAuthenticationActions: ["PasswordLogin", "PasswordPlusPhoneOtp"],
    availableAuthenticationMechanisms: ["Password", "Phone"],
    requiredAuthenticationMechanisms: ["Password"],
    availablePreAuthenticationActions: ["PasswordLogin", "PasswordPlusPhoneOtp"],
    requiredPreAuthenticationActions: ["PasswordLogin"],
    availableFailedAttemptResponses: ["Deny", "Lockout"],
    mandatoryPasswordMinimumLength: 8,
    version: 4,
  },
}));

vi.mock("@vitarish/auth", () => ({
  usePlatformAuthorization: () => ({ hasAccess: true, isLoading: false }),
  useAuthenticationSessionPolicyGet: () => ({ data: state.sessionPolicy, isLoading: false, isError: false, refetch: state.sessionRefetch }),
  useAuthenticationAuthenticationPolicyGet: () => ({ data: state.authenticationPolicy, isLoading: false, isError: false, refetch: state.authenticationRefetch }),
  useAuthenticationSessionPolicyPut: () => ({ mutateAsync: state.sessionPut, isPending: false, isError: false, error: undefined }),
  useAuthenticationAuthenticationPolicyPut: () => ({ mutateAsync: state.authenticationPut, isPending: false, isError: false, error: undefined }),
}));

function renderPage(locale: "en" | "fa" = "en") {
  return render(
    <I18nProvider locale={locale} messages={getMessages(locale)}>
      <SecuritySettingsPage />
    </I18nProvider>,
  );
}

function sectionOf(heading: string): HTMLElement {
  const section = screen.getByRole("heading", { name: heading }).closest("section");
  if (!section) throw new Error(`Missing section for ${heading}.`);
  return section;
}

describe("SecuritySettingsPage", () => {
  beforeEach(() => {
    state.sessionRefetch.mockReset().mockResolvedValue(undefined);
    state.authenticationRefetch.mockReset().mockResolvedValue(undefined);
    state.sessionPut.mockReset().mockResolvedValue({ ...state.sessionPolicy, version: 5 });
    state.authenticationPut.mockReset().mockResolvedValue({ ...state.authenticationPolicy, version: 5 });
  });

  afterEach(() => cleanup());

  it("uses the shared policy version for the other form after a save without discarding its edits", async () => {
    renderPage();

    const maximumSessions = screen.getByLabelText("Maximum concurrent sessions per user") as HTMLInputElement;
    fireEvent.change(maximumSessions, { target: { value: "3" } });
    fireEvent.click(screen.getByLabelText("Concurrent sessions are required for this deployment"));

    fireEvent.click(screen.getByRole("button", { name: "Save authentication policy" }));
    await waitFor(() => expect(state.authenticationPut).toHaveBeenCalledTimes(1));
    expect(state.authenticationPut.mock.calls[0][0].data.expectedVersion).toBe(4);

    expect(maximumSessions).toHaveValue(3);
    expect(screen.getByLabelText("Concurrent sessions are required for this deployment")).toBeChecked();

    fireEvent.click(screen.getByRole("button", { name: "Save policy" }));
    await waitFor(() => expect(state.sessionPut).toHaveBeenCalledTimes(1));
    expect(state.sessionPut.mock.calls[0][0].data).toMatchObject({
      maximumConcurrentSessions: 3,
      concurrentSessionsFunctionallyRequired: true,
      expectedVersion: 5,
    });
  });

  it("shows each form's save result inside its own section", async () => {
    renderPage();

    fireEvent.click(screen.getByRole("button", { name: "Save authentication policy" }));
    await waitFor(() => expect(within(sectionOf("Authentication policy")).getByRole("status")).toHaveTextContent("Security policy saved."));
    expect(within(sectionOf("Session policy")).queryByRole("status")).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "Save policy" }));
    await waitFor(() => expect(within(sectionOf("Session policy")).getByRole("status")).toHaveTextContent("Security policy saved."));
  });

  it("renders localized authentication mechanism and pre-authentication action labels", () => {
    renderPage();

    expect(screen.getByText("Phone code")).toBeInTheDocument();
    expect(screen.getByText("Password + phone code")).toBeInTheDocument();
    expect(screen.queryByText("Phone")).toBeNull();
    expect(screen.queryByText("PasswordPlusPhoneOtp")).toBeNull();
  });

  it("renders the Persian authentication mechanism and pre-authentication action labels", () => {
    renderPage("fa");

    expect(screen.getByText("کد تلفن")).toBeInTheDocument();
    expect(screen.getByText("گذرواژه + کد یکبارمصرف تلفن")).toBeInTheDocument();
    expect(screen.queryByText("PasswordPlusPhoneOtp")).toBeNull();
  });
});
