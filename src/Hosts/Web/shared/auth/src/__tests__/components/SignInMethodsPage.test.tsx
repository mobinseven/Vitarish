import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider } from "@vitarish/localization";
import { SignInMethodsPage } from "../../components/SignInMethodsPage";
import { signInReturnUrlQueryKey } from "../../centralHost";
import { authMessageCatalogs } from "../../localization/catalogs";
import { AuthenticationWebRoutes } from "../../routes";

const mocks = vi.hoisted(() => ({
  search: "",
  profile: vi.fn(),
}));

vi.mock("../../api/generated/authentication/authentication", () => ({
  useAuthenticationProfileGet: mocks.profile,
}));

// The method labels are app-owned `auth.login` text; the notice is the shared catalog's.
const appLoginLabels = {
  brandTitle: "Vitarish",
  accountSubtitle: "Sign in to your account",
  passwordMethod: "Sign in with Password",
  passwordMethodDescription: "Use your username or email and password",
  phoneMethod: "Sign in with Phone",
  phoneMethodDescription: "Get a one-time code by text message",
  emailMethod: "Sign in with Email",
  emailMethodDescription: "Get a one-time code by email",
  passkeyMethod: "Sign in with Passkey",
  passkeyMethodDescription: "Use your fingerprint, face, or device screen lock",
};

function renderPage() {
  return render(
    <LocalizationProvider
      locale="en"
      messages={{ auth: { login: appLoginLabels, ...authMessageCatalogs.en.auth } }}
    >
      <SignInMethodsPage searchParams={new URLSearchParams(mocks.search)} />
    </LocalizationProvider>,
  );
}

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  mocks.search = "";
});

describe("SignInMethodsPage", () => {
  it("carries a safe relative return path to the selected sign-in method", () => {
    mocks.search = `${signInReturnUrlQueryKey}=%2Fstudio`;
    mocks.profile.mockReturnValue({ data: { allowPasswordlessPhoneLogin: true } });

    renderPage();

    const expectedQuery = new URLSearchParams({ [signInReturnUrlQueryKey]: "/studio" }).toString();
    expect(screen.getByRole("link", { name: new RegExp(`^${appLoginLabels.phoneMethod}`) }))
      .toHaveAttribute("href", `${AuthenticationWebRoutes.SignInPhone}?${expectedQuery}`);
  });

  it.each([
    ["an external host", "//evil.example"],
    ["a control character smuggling a host", "/\t/evil.example"],
    ["a backslash host", "/\\evil.example"],
  ])("drops %s as a return target before linking to a sign-in method", (_name, target) => {
    mocks.search = `${signInReturnUrlQueryKey}=${encodeURIComponent(target)}`;
    mocks.profile.mockReturnValue({ data: { allowPasswordlessPhoneLogin: true } });

    renderPage();

    expect(screen.getByRole("link", { name: new RegExp(`^${appLoginLabels.phoneMethod}`) }))
      .toHaveAttribute("href", AuthenticationWebRoutes.SignInPhone);
  });

  it("offers only the methods the account policy enables", () => {
    mocks.profile.mockReturnValue({ data: { allowPasskeyLogin: true } });

    renderPage();

    expect(screen.getByRole("link", { name: new RegExp(`^${appLoginLabels.passwordMethod}`) })).toBeVisible();
    expect(screen.getByRole("link", { name: new RegExp(`^${appLoginLabels.passkeyMethod}`) })).toBeVisible();
    expect(screen.queryByRole("link", { name: new RegExp(`^${appLoginLabels.phoneMethod}`) })).toBeNull();
    expect(screen.queryByRole("link", { name: new RegExp(`^${appLoginLabels.emailMethod}`) })).toBeNull();
  });

  it("explains that phone or email sign-in creates an account instead of offering any app's onboarding", () => {
    mocks.profile.mockReturnValue({ data: { allowPasswordlessPhoneLogin: true } });

    renderPage();

    expect(screen.getByText(authMessageCatalogs.en.auth.signInMethods.accountCreationNotice)).toBeVisible();
    expect(screen.getAllByRole("link").map((link) => link.getAttribute("href")))
      .toEqual([AuthenticationWebRoutes.SignInPassword, AuthenticationWebRoutes.SignInPhone]);
  });
});
