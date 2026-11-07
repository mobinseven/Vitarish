import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ApiError } from "@vitarish/api-client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { http, HttpResponse } from "msw";
import { authenticationErrorCodes, getAuthenticationSessionGetQueryKey, type AuthenticationSessionDto } from "@vitarish/auth";
import { I18nProvider } from "@vitarish/localization";
import { Routes } from "@/app/routes";
import AccountPasswordPage from "@/app/account/password/page";
import { enMessages } from "@/localization/messages/en";
import { server } from "@/__tests__/server";

const changePassword = vi.fn();
const setFirstPassword = vi.fn();
const setTwoFactorEnabled = vi.fn();

vi.mock("@vitarish/account", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@vitarish/account")>();
  return {
    ...actual,
    useAccountPasswordChange: () => ({ changePassword }),
    useAccountSignInSecurityActions: () => ({
      setFirstPassword,
      setTwoFactorEnabled,
    }),
  };
});

vi.mock("@/components/account/AccountSurface", async () => {
  const { createElement } = await import("react");
  return {
    AccountSurface: ({ children }: { children: React.ReactNode }) => (
      createElement("main", null, children)
    ),
    AccountBackLink: () => createElement("a", { href: Routes.Account.Index }, "Back"),
  };
});

vi.mock("@vitarish/ui", async () => {
  const { createElement } = await import("react");
  return {
    HeaderIcon: () => createElement("span", { "aria-hidden": true }),
    StatusAlert: ({ message }: { message: string }) =>
      createElement("div", { role: "alert" }, message),
  };
});

describe("AccountPasswordPage", () => {
  it("leaves the password policy to the server and shows its rejection", async () => {
    const session: AuthenticationSessionDto = {
      isAuthenticated: true,
      user: {
        userId: "account-1",
        displayName: null,
        givenName: null,
        familyName: null,
        email: null,
        phoneNumber: null,
        hasPassword: true,
        twoFactorEnabled: false,
        twoFactorChannel: null,
      },
      tenantAccess: { canAccessTenantOwner: true, canAccessTenantUser: false },
      canAccessPlatformAdmin: false,
    };
    const sessionPath = getAuthenticationSessionGetQueryKey()[0];
    server.use(
      http.get(
        new RegExp(`${sessionPath.replaceAll("/", "\\/")}(?:\\?|$)`),
        () => HttpResponse.json(session),
      ),
    );
    changePassword.mockRejectedValueOnce(
      new ApiError({
        status: 400,
        title: "Invalid input",
        code: authenticationErrorCodes.invalidInput,
      }),
    );
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    render(
      <QueryClientProvider client={queryClient}>
        <I18nProvider locale="en" messages={enMessages}>
          <AccountPasswordPage />
        </I18nProvider>
      </QueryClientProvider>,
    );

    const currentPassword = await screen.findByLabelText(
      enMessages.account.password.currentPassword,
      { exact: true },
    );
    const newPassword = screen.getByLabelText(
      enMessages.account.password.newPassword,
      { exact: true },
    );
    fireEvent.change(currentPassword, { target: { value: "Current-Pass1!" } });
    fireEvent.change(newPassword, { target: { value: "short" } });
    fireEvent.change(
      screen.getByLabelText(enMessages.account.password.confirmNewPassword, {
        exact: true,
      }),
      { target: { value: "short" } },
    );
    fireEvent.submit(newPassword.closest("form")!);

    await waitFor(() => expect(changePassword).toHaveBeenCalledWith("Current-Pass1!", "short"));
    expect(await screen.findByText(enMessages.account.password.policyRejected)).toBeVisible();
  });
});
