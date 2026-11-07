import React from "react";
import { cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { I18nProvider } from "@vitarish/localization";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { catalogs } from "../../../localization/messages";
import { AccountPage } from "../AccountPage";

const mocks = vi.hoisted(() => ({
  session: {
    isAuthenticated: true,
    isLoading: false,
    isError: false,
    user: { userId: "user-1" } as { userId: string } | null,
    refetch: vi.fn(),
  },
  changePassword: vi.fn(),
  setFirstPassword: vi.fn(),
  setTwoFactorEnabled: vi.fn(),
  beginEmailChange: vi.fn(),
  verifyEmailChange: vi.fn(),
  setEmailError: vi.fn(),
  emailState: {} as Record<string, unknown>,
  accountSignInSettingsProps: undefined as Record<string, unknown> | undefined,
  phoneUserId: undefined as string | undefined,
  emailUserId: undefined as string | undefined,
  assign: vi.fn(),
}));

vi.mock("@heroui/react", () => {
  const Button = ({ children, isDisabled, isPending, onPress, type = "button", ...props }: any) => (
    <button type={type} {...props} disabled={isDisabled || isPending} onClick={onPress}>{children}</button>
  );
  const Tooltip: any = ({ children }: any) => <>{children}</>;
  Tooltip.Content = () => null;
  const Alert: any = ({ children }: any) => <div role="alert">{children}</div>;
  Alert.Indicator = () => null;
  Alert.Content = ({ children }: any) => <div>{children}</div>;
  Alert.Title = ({ children }: any) => <p>{children}</p>;
  Alert.Description = ({ children }: any) => <div>{children}</div>;
  const TextField = ({ children }: any) => <div>{children}</div>;
  return {
    Alert,
    Button,
    Input: (props: any) => <input {...props} />,
    Label: ({ children }: any) => <label>{children}</label>,
    Surface: ({ children }: any) => <section>{children}</section>,
    TextField,
    Tooltip,
  };
});

vi.mock("@vitarish/ui", () => ({
  FaIcon: () => null,
  HeaderIcon: () => null,
  MobileNumberField: ({ label }: any) => <input aria-label={label} />,
  SettingsBackLink: ({ href, label }: any) => <a href={href}>{label}</a>,
  SettingsSurface: ({ children }: any) => <div>{children}</div>,
  StatusAlert: ({ message }: any) => <div role="alert">{message}</div>,
}));

vi.mock("@vitarish/auth", () => ({
  useAuthSession: () => mocks.session,
  OtpCodeInput: ({ value, onChange }: any) => <input aria-label="code" value={value} onChange={(event) => onChange(event.target.value)} />,
}));

vi.mock("@vitarish/account", () => ({
  AccountSignInSettings: (props: Record<string, unknown>) => {
    mocks.accountSignInSettingsProps = props;
    return <div data-testid="sign-in-settings" />;
  },
  useAccountPasswordChange: () => ({ changePassword: mocks.changePassword }),
  useAccountSignInSecurityActions: () => ({ setFirstPassword: mocks.setFirstPassword, setTwoFactorEnabled: mocks.setTwoFactorEnabled }),
  useAccountEmailChange: (userId: string) => {
    mocks.emailUserId = userId;
    return mocks.emailState;
  },
  useAccountPhoneChange: (userId: string) => {
    mocks.phoneUserId = userId;
    return {
      profile: null, step: "phone", phoneNumber: "", code: "", isLoading: false, error: null, success: false,
      setPhoneNumber: vi.fn(), setCode: vi.fn(), setError: vi.fn(), reset: vi.fn(), beginPhoneChange: vi.fn(), verifyPhoneChange: vi.fn(),
    };
  },
}));

function renderAccountPage() {
  return render(
    <I18nProvider locale="en" messages={catalogs.en}>
      <AccountPage signInHref="https://auth.example.test/login" />
    </I18nProvider>,
  );
}

describe("platform home account area", () => {
  afterEach(cleanup);

  beforeEach(() => {
    mocks.session = { isAuthenticated: true, isLoading: false, isError: false, user: { userId: "user-1" }, refetch: vi.fn() };
    mocks.accountSignInSettingsProps = undefined;
    mocks.emailUserId = undefined;
    mocks.phoneUserId = undefined;
    mocks.beginEmailChange.mockReset().mockResolvedValue(undefined);
    mocks.verifyEmailChange.mockReset().mockResolvedValue(undefined);
    mocks.setEmailError.mockReset();
    mocks.emailState = {
      profile: { email: "ali@example.test" },
      step: "email",
      email: "new@example.test",
      code: "",
      isLoading: false,
      error: null,
      success: false,
      setEmail: vi.fn(),
      setCode: vi.fn(),
      setError: mocks.setEmailError,
      reset: vi.fn(),
      beginEmailChange: mocks.beginEmailChange,
      verifyEmailChange: mocks.verifyEmailChange,
    };
    Object.defineProperty(window, "location", { configurable: true, value: { ...window.location, assign: mocks.assign } });
    mocks.assign.mockReset();
  });

  it("sends a person who is not signed in to the sign-in host instead of showing account settings", () => {
    mocks.session = { ...mocks.session, isAuthenticated: false, user: null };
    renderAccountPage();

    expect(mocks.assign).toHaveBeenCalledWith("https://auth.example.test/login");
    expect(screen.queryByTestId("sign-in-settings")).toBeNull();
  });

  it("does not redirect while the session lookup failed, and offers a retry", () => {
    const refetch = vi.fn().mockResolvedValue(undefined);
    mocks.session = { ...mocks.session, isAuthenticated: false, isError: true, user: null, refetch };
    renderAccountPage();

    fireEvent.click(screen.getByRole("button", { name: "Try again" }));

    expect(mocks.assign).not.toHaveBeenCalled();
    expect(refetch).toHaveBeenCalledTimes(1);
  });

  it("gives the password, email and phone sections the session user and the shared sign-in actions", () => {
    renderAccountPage();

    expect(mocks.accountSignInSettingsProps).toMatchObject({
      changePassword: mocks.changePassword,
      setFirstPassword: mocks.setFirstPassword,
      setTwoFactorEnabled: mocks.setTwoFactorEnabled,
    });
    expect(mocks.emailUserId).toBe("user-1");
    expect(mocks.phoneUserId).toBe("user-1");
    expect(screen.getByRole("heading", { name: "Change Email" })).not.toBeNull();
    expect(screen.getByRole("heading", { name: "Change Phone Number" })).not.toBeNull();
    expect(screen.getByRole("link", { name: "Back to home" }).getAttribute("href")).toBe("/home");
  });

  it("starts an email change with the typed address and shows a localized failure instead of leaking the raw error", async () => {
    mocks.beginEmailChange.mockRejectedValueOnce({ status: 403 });
    renderAccountPage();

    const emailSection = screen.getByRole("heading", { name: "Change Email" }).closest("section") as HTMLElement;
    fireEvent.click(within(emailSection).getByRole("button", { name: "Send Code" }));

    await waitFor(() => expect(mocks.beginEmailChange).toHaveBeenCalledWith("new@example.test"));
    expect(mocks.setEmailError).toHaveBeenCalledWith("You do not have permission to perform this action.");
  });
});
