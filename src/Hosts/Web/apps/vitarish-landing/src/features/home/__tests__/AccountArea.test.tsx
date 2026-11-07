import React from "react";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { I18nProvider } from "@vitarish/localization";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { catalogs } from "../../../localization/messages";
import { AccountArea } from "../AccountArea";

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
  signInSettingsProps: undefined as Record<string, unknown> | undefined,
  emailUserId: undefined as string | undefined,
  phoneUserId: undefined as string | undefined,
  assign: vi.fn(),
}));

vi.mock("@heroui/react", () => {
  const Button = ({ children, isDisabled, onPress, ...props }: any) => (
    <button type="button" {...props} disabled={isDisabled} onClick={onPress}>{children}</button>
  );
  const Tooltip: any = ({ children }: any) => <>{children}</>;
  Tooltip.Content = () => null;
  const Alert: any = ({ children }: any) => <div role="alert">{children}</div>;
  Alert.Indicator = () => null;
  Alert.Content = ({ children }: any) => <div>{children}</div>;
  Alert.Title = ({ children }: any) => <p>{children}</p>;
  Alert.Description = ({ children }: any) => <div>{children}</div>;
  return { Alert, Button, Tooltip };
});

vi.mock("@vitarish/ui", () => ({
  FaIcon: () => null,
  HeaderIcon: () => null,
  SettingsBackLink: ({ href, label }: any) => <a href={href}>{label}</a>,
  SettingsSurface: ({ children }: any) => <div>{children}</div>,
}));

vi.mock("@vitarish/auth", () => ({ useAuthSession: () => mocks.session }));

vi.mock("@vitarish/account", () => ({
  AccountSignInSettings: (props: Record<string, unknown>) => {
    mocks.signInSettingsProps = props;
    return <div data-testid="sign-in-settings" />;
  },
  AccountEmailChange: ({ userId }: { userId: string }) => {
    mocks.emailUserId = userId;
    return <h2>Change Email</h2>;
  },
  AccountPhoneChange: ({ userId }: { userId: string }) => {
    mocks.phoneUserId = userId;
    return <h2>Change Phone Number</h2>;
  },
  useAccountPasswordChange: () => ({ changePassword: mocks.changePassword }),
  useAccountSignInSecurityActions: () => ({ setFirstPassword: mocks.setFirstPassword, setTwoFactorEnabled: mocks.setTwoFactorEnabled }),
}));

function renderAccountArea() {
  return render(
    <I18nProvider locale="en" messages={catalogs.en}>
      <AccountArea signInHref="https://auth.example.test/login" />
    </I18nProvider>,
  );
}

describe("platform home account area", () => {
  afterEach(cleanup);

  beforeEach(() => {
    mocks.session = { isAuthenticated: true, isLoading: false, isError: false, user: { userId: "user-1" }, refetch: vi.fn() };
    mocks.signInSettingsProps = undefined;
    mocks.emailUserId = undefined;
    mocks.phoneUserId = undefined;
    Object.defineProperty(window, "location", { configurable: true, value: { ...window.location, assign: mocks.assign } });
    mocks.assign.mockReset();
  });

  it("sends a person who is not signed in to the sign-in host instead of showing account settings", () => {
    mocks.session = { ...mocks.session, isAuthenticated: false, user: null };
    renderAccountArea();

    expect(mocks.assign).toHaveBeenCalledWith("https://auth.example.test/login");
    expect(screen.queryByTestId("sign-in-settings")).toBeNull();
  });

  it("does not redirect while the session lookup failed, and offers a retry", () => {
    const refetch = vi.fn().mockResolvedValue(undefined);
    mocks.session = { ...mocks.session, isAuthenticated: false, isError: true, user: null, refetch };
    renderAccountArea();

    fireEvent.click(screen.getByRole("button", { name: "Try again" }));

    expect(mocks.assign).not.toHaveBeenCalled();
    expect(refetch).toHaveBeenCalledTimes(1);
  });

  it("gives the password, email and phone sections the session user and the shared sign-in actions", () => {
    renderAccountArea();

    expect(mocks.signInSettingsProps).toMatchObject({
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
});
