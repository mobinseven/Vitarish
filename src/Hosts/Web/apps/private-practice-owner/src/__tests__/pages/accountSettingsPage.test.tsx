import { render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import AccountSettingsPage from "@/app/account/page";
import { Routes } from "@/app/routes";

const authSession = vi.hoisted(() => ({ isAuthenticated: true }));

vi.mock("@vitarish/auth", () => ({
  useAuthSession: () => ({
    isAuthenticated: authSession.isAuthenticated,
    displayProfile: { displayLabel: "Test User", initials: "TU" },
  }),
}));

vi.mock("@/components/account/AccountSurface", async () => {
  const { StandaloneDocumentPage } = await import("@vitarish/ui");
  return {
    AccountSurface: ({ children }: { children: React.ReactNode }) => (
      <StandaloneDocumentPage>{children}</StandaloneDocumentPage>
    ),
  };
});

vi.mock("@/localization", () => ({
  useLocalizedText: () => ({
    locale: "en",
    t: (key: string) => ({
      "account.settings": "Account settings",
      "account.workspaces.title": "Your workspaces",
      "account.workspaces.caption": "Open the clinics you belong to.",
      "account.navigation.security": "Security",
      "account.captions.security": "Review sign-in activity.",
    } as Record<string, string>)[key] ?? key,
  }),
}));

afterEach(() => {
  authSession.isAuthenticated = true;
});

describe("AccountSettingsPage workspaces link", () => {
  it("links a signed-in person to their workspaces so they can get back to their clinic", () => {
    render(<AccountSettingsPage />);

    expect(screen.getByRole("link", { name: /Your workspaces/ })).toHaveAttribute("href", Routes.SignInContinue);
  });

  it("does not offer the workspaces link to a signed-out visitor", () => {
    authSession.isAuthenticated = false;
    render(<AccountSettingsPage />);

    expect(screen.queryByRole("link", { name: /Your workspaces/ })).not.toBeInTheDocument();
  });
});
