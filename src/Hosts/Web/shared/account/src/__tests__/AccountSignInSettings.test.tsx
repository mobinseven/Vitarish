import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { I18nProvider } from "@vitarish/localization";
import { ApiError } from "@vitarish/api-client";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AccountSignInSettings } from "../components/AccountSignInSettings";

const authState = vi.hoisted(() => ({
  isAuthenticated: true,
  isError: false,
  isLoading: false,
  user: {
    userId: "account-1",
    hasPassword: false,
    twoFactorEnabled: false,
    twoFactorChannel: null,
  },
  refetch: vi.fn(),
}));

vi.mock("@vitarish/auth", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@vitarish/auth")>();
  return { ...actual, useAuthSession: () => authState };
});

vi.mock("@vitarish/ui", async () => {
  const { createElement } = await import("react");
  return {
    HeaderIcon: () => createElement("span", { "aria-hidden": true }),
    StatusAlert: ({
      message,
      type = "error",
    }: {
      message: string;
      type?: "error" | "success";
    }) =>
      createElement(
        "div",
        { role: type === "error" ? "alert" : "status" },
        message,
      ),
  };
});

const messages = {
  common: { states: { loading: "Loading" } },
  account: {
    password: {
      title: "Password",
      currentPassword: "Current password",
      newPassword: "New password",
      confirmNewPassword: "Confirm new password",
      changeAction: "Change password",
      setFirstAction: "Set a password",
      setFirstDescription: "Set a password to sign in with one.",
      changeDescription: "Change the password used to sign in.",
      mismatch: "Passwords do not match.",
      policyRejected: "The password does not meet the security policy.",
      currentPasswordRejected: "The current password is incorrect.",
      changeFailed: "The password could not be changed.",
      changeSucceeded: "Password changed successfully.",
      firstSetSucceeded: "Password set successfully.",
    },
    signInSecurity: {
      title: "Two-step sign-in",
      description: "Receive a one-time code after your password.",
      toggleLabel: "Two-step sign-in",
      enableAction: "Turn on two-step sign-in",
      disableAction: "Turn off two-step sign-in",
      enabled: "On",
      disabled: "Off",
      channel: "Code delivery",
      channelPhone: "Text message to your confirmed phone number",
      channelEmail: "Email to your confirmed email address",
      noChannel: "Confirm a phone number or email address first.",
      needsPassword: "Set a password before turning this on.",
      updated: "Two-step sign-in settings updated.",
      updateFailed: "Two-step sign-in settings could not be updated.",
      preconditionRejected: "A password and confirmed contact are required.",
      conflict: "These settings changed in another session.",
      freshAuthenticationRequired: "Sign in again before changing these settings.",
      unauthorized: "Your session ended. Sign in again.",
      forbidden: "Your account cannot change this setting.",
      unavailable: "Account sign-in settings could not be loaded.",
      reload: "Reload account sign-in settings",
    },
  },
};

const passwordSet = vi.fn<(...args: [string]) => Promise<void>>();
const passwordChange = vi.fn<(...args: [string, string]) => Promise<void>>();
const twoStepChange = vi.fn<(...args: [boolean]) => Promise<void>>();

function renderSettings() {
  return render(
    <I18nProvider locale="en" messages={messages}>
      <AccountSignInSettings
        setFirstPassword={passwordSet}
        changePassword={passwordChange}
        setTwoFactorEnabled={twoStepChange}
      />
    </I18nProvider>,
  );
}

function setPasswordFields(newPassword: string, confirmation = newPassword) {
  fireEvent.change(screen.getByLabelText("New password"), {
    target: { value: newPassword },
  });
  fireEvent.change(screen.getByLabelText("Confirm new password"), {
    target: { value: confirmation },
  });
  fireEvent.submit(screen.getByLabelText("New password").closest("form")!);
}

describe("AccountSignInSettings", () => {
  afterEach(() => cleanup());

  beforeEach(async () => {
    const { TwoStepSignInChannelDto } = await import("@vitarish/auth");
    authState.isAuthenticated = true;
    authState.isError = false;
    authState.isLoading = false;
    authState.user = {
      userId: "account-1",
      hasPassword: false,
      twoFactorEnabled: false,
      twoFactorChannel: null,
    };
    Object.assign(authState.user, {
      twoFactorChannel: TwoStepSignInChannelDto.Email,
    });
    authState.refetch.mockReset().mockResolvedValue({});
    passwordSet.mockReset().mockResolvedValue(undefined);
    passwordChange.mockReset().mockResolvedValue(undefined);
    twoStepChange.mockReset().mockResolvedValue(undefined);
  });

  it("sets a first password without asking for the current password", async () => {
    renderSettings();

    expect(screen.queryByLabelText("Current password")).not.toBeInTheDocument();
    const setPasswordButton = screen.getByRole("button", {
      name: "Set a password",
    });
    expect(
      setPasswordButton.querySelector("i.fa-light.fa-key"),
    ).toBeInTheDocument();
    setPasswordFields("Strong-Password9!");

    await waitFor(() =>
      expect(passwordSet).toHaveBeenCalledWith("Strong-Password9!"),
    );
    expect(passwordChange).not.toHaveBeenCalled();
    expect(await screen.findByText("Password set successfully.")).toBeVisible();
  });

  it("keeps current-password verification for accounts that already have one", async () => {
    authState.user.hasPassword = true;
    renderSettings();

    expect(screen.getByLabelText("Current password")).toBeVisible();
    fireEvent.change(screen.getByLabelText("Current password"), {
      target: { value: "Current-Password9!" },
    });
    setPasswordFields("Next-Password9!");

    await waitFor(() =>
      expect(passwordChange).toHaveBeenCalledWith(
        "Current-Password9!",
        "Next-Password9!",
      ),
    );
    expect(passwordSet).not.toHaveBeenCalled();
  });

  it("does not send a mismatched first-password form", async () => {
    renderSettings();
    setPasswordFields("Strong-Password9!", "Different-Password9!");

    expect(await screen.findByText("Passwords do not match.")).toBeVisible();
    expect(passwordSet).not.toHaveBeenCalled();
  });

  it("does not show a saved notice when refreshing the security state fails", async () => {
    authState.refetch.mockResolvedValueOnce({ isError: true });
    renderSettings();
    setPasswordFields("Strong-Password9!");

    expect(await screen.findByText("Account sign-in settings could not be loaded.")).toBeVisible();
    expect(screen.queryByText("Password set successfully.")).not.toBeInTheDocument();
  });

  it("does not offer to turn on two-step sign-in before a password is set", () => {
    renderSettings();

    expect(screen.getByRole("switch", { name: "Two-step sign-in" })).toBeDisabled();
    expect(twoStepChange).not.toHaveBeenCalled();
  });

  it("does not offer to turn on two-step sign-in without a confirmed contact", () => {
    authState.user.hasPassword = true;
    authState.user.twoFactorChannel = null;
    renderSettings();

    expect(screen.getByRole("switch", { name: "Two-step sign-in" })).toBeDisabled();
  });

  it("shows the server-selected code channel and requests the two-step change", async () => {
    authState.user.hasPassword = true;
    renderSettings();

    expect(
      screen.getByText("Email to your confirmed email address"),
    ).toBeVisible();
    fireEvent.click(screen.getByRole("switch", { name: "Two-step sign-in" }));

    await waitFor(() => expect(twoStepChange).toHaveBeenCalledWith(true));
    expect(await screen.findByText("Two-step sign-in settings updated.")).toBeVisible();
  });

  it("distinguishes a wrong current password from a fresh-session requirement", async () => {
    const { authenticationErrorCodes } = await import("@vitarish/auth");
    authState.user.hasPassword = true;
    passwordChange.mockRejectedValueOnce(
      new ApiError({
        status: 401,
        code: authenticationErrorCodes.invalidCredentials,
      }),
    );
    renderSettings();

    fireEvent.change(screen.getByLabelText("Current password"), {
      target: { value: "Incorrect-Password9!" },
    });
    setPasswordFields("Next-Password9!");

    expect(
      await screen.findByText("The current password is incorrect."),
    ).toBeVisible();
    expect(
      screen.queryByText("Sign in again before changing these settings."),
    ).not.toBeInTheDocument();
  });

  it("asks passwordless accounts to sign in again when the session is not fresh", async () => {
    const { authenticationErrorCodes } = await import("@vitarish/auth");
    passwordSet.mockRejectedValueOnce(
      new ApiError({
        status: 401,
        code: authenticationErrorCodes.unauthorized,
      }),
    );
    renderSettings();
    setPasswordFields("Strong-Password9!");

    expect(
      await screen.findByText("Sign in again before changing these settings."),
    ).toBeVisible();
    expect(passwordChange).not.toHaveBeenCalled();
  });

  it("shows a phone channel when the session read model selects phone", async () => {
    const { TwoStepSignInChannelDto } = await import("@vitarish/auth");
    Object.assign(authState.user, {
      twoFactorChannel: TwoStepSignInChannelDto.Phone,
    });
    renderSettings();

    expect(
      screen.getByText("Text message to your confirmed phone number"),
    ).toBeVisible();
  });
});
