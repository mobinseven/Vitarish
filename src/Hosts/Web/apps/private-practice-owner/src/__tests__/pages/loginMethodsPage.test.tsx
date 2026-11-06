import { screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { signInReturnUrlQueryKey } from "@vitarish/journey/sign-in-host";
import { Routes } from "@/app/routes";
import { enMessages } from "@/localization/messages/en";
import { renderWithLocalization } from "@/__tests__/localization/renderWithLocalization";

const mocks = vi.hoisted(() => ({
  search: "",
  useAuthenticationProfile: vi.fn(),
}));

vi.mock("next/navigation", () => ({
  useSearchParams: () => new URLSearchParams(mocks.search),
}));

vi.mock("@vitarish/auth", () => ({
  useAuthenticationProfile: mocks.useAuthenticationProfile,
}));

import { LoginMethodsPage } from "@/app/login/LoginMethodsPage";

afterEach(() => {
  vi.clearAllMocks();
  mocks.search = "";
});

describe("LoginMethodsPage return intent", () => {
  it("carries a safe relative return path to the selected sign-in method", () => {
    mocks.search = `${signInReturnUrlQueryKey}=%2Fstudio`;
    mocks.useAuthenticationProfile.mockReturnValue({
      data: { allowPasswordlessPhoneLogin: true },
    });

    renderWithLocalization(<LoginMethodsPage onboardingUrl={null} />, { messages: enMessages });

    const phoneLink = screen.getByRole("link", {
      name: new RegExp(`^${enMessages.auth.login.phoneMethod}`),
    });
    const expectedQuery = new URLSearchParams({
      [signInReturnUrlQueryKey]: "/studio",
    }).toString();
    expect(phoneLink).toHaveAttribute("href", `${Routes.Login.Phone}?${expectedQuery}`);
  });

  it("drops an external return target before linking to a sign-in method", () => {
    mocks.search = `${signInReturnUrlQueryKey}=${encodeURIComponent("//evil.example")}`;
    mocks.useAuthenticationProfile.mockReturnValue({
      data: { allowPasswordlessPhoneLogin: true },
    });

    renderWithLocalization(<LoginMethodsPage onboardingUrl={null} />, { messages: enMessages });

    const phoneLink = screen.getByRole("link", {
      name: new RegExp(`^${enMessages.auth.login.phoneMethod}`),
    });
    expect(phoneLink).toHaveAttribute("href", Routes.Login.Phone);
  });
});
