import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { DashboardShell, type DashboardShellItem } from "@/components/shell/DashboardShell";
import { ThemeSwitch } from "@/components/shell/ThemeSwitch";
import { UserMenu } from "@/components/shell/UserMenu";
import { SearchInput } from "@/components/shell/SearchInput";

vi.mock("next-themes", () => ({
  useTheme: () => ({
    setTheme: vi.fn(),
    resolvedTheme: "light",
  }),
}));

vi.mock("@/hooks/auth/useAuthSession", () => ({
  useAuthSession: () => ({
    isAuthenticated: true,
    isLoading: false,
    displayProfile: {
      displayLabel: "Test User",
      initials: "TU",
    },
  }),
}));

vi.mock("@/hooks/auth/useLogout", () => ({
  useLogout: () => ({
    logout: vi.fn(),
    isLoading: false,
  }),
}));

vi.mock("@/localization", () => ({
  useLocalizedText: () => ({
    t: (key: string, parameters?: Record<string, string>) => {
      const translations: Record<string, string> = {
        "appShell.brand": "Vitarish",
        "appShell.navigation.home": "Home",
        "appShell.navigation.knowledge": "Knowledge",
        "appShell.navigation.dashboardMenu": "Dashboard navigation",
        "appShell.navigation.accountMenu": "Account",
        "appShell.actions.notifications": "Notifications",
        "appShell.actions.invite": "Invite",
        "appShell.theme.light": "Switch to light mode",
        "appShell.theme.dark": "Switch to dark mode",
        "appShell.userMenu.logIn": "Log In",
        "appShell.userMenu.account": "Account",
        "appShell.userMenu.signOut": "Log Out",
        "appShell.menu.open": "Open menu",
        "appShell.search.label": "Search",
        "appShell.search.placeholder": "Search...",
        "home.welcomeBack": "Welcome back",
      };

      if (key === "home.welcomeBackNamed") {
        return `Welcome back, ${parameters?.name}`;
      }

      return translations[key] ?? key;
    },
  }),
}));

const shellNavItems = [
  { href: "/", iconClassName: "fa-solid fa-house", labelKey: "appShell.navigation.home", exactMatch: true },
] as const satisfies readonly DashboardShellItem[];

const shellFooterItems = [
  { href: "/account", iconClassName: "fa-solid fa-circle-user", labelKey: "appShell.userMenu.account" },
] as const satisfies readonly DashboardShellItem[];

describe("Dashboard Shell Components", () => {
  describe("DashboardShell", () => {
    it("renders the template-style sidebar, navbar actions, and content", () => {
      render(
        <DashboardShell footerItems={shellFooterItems} homeHref="/" navItems={shellNavItems}>
          <div>Dashboard content</div>
        </DashboardShell>
      );

      expect(screen.getByRole("heading", { name: "Welcome back, Test User" })).toBeInTheDocument();
      expect(screen.getAllByText("Home").length).toBeGreaterThan(0);
      expect(screen.getAllByText("Account").length).toBeGreaterThan(0);
      expect(screen.getByRole("button", { name: "Search" })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: "Notifications" })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: "Invite" })).toBeInTheDocument();
      expect(screen.getByText("Dashboard content")).toBeInTheDocument();
    });
  });

  describe("ThemeSwitch", () => {
    it("renders with accessible label", () => {
      render(<ThemeSwitch />);
      const button = screen.getByRole("button");
      expect(button).toBeInTheDocument();
      expect(button).toHaveAttribute("aria-label");
    });
  });

  describe("UserMenu", () => {
    it("renders user display label when authenticated", () => {
      render(<UserMenu />);
      expect(screen.getByText("Test User")).toBeInTheDocument();
    });
  });

  describe("SearchInput", () => {
    it("renders search input with label", () => {
      render(<SearchInput />);
      const input = screen.getByRole("searchbox");
      expect(input).toBeInTheDocument();
      expect(input).toHaveAttribute("aria-label", "Search");
    });

    it("renders placeholder text", () => {
      render(<SearchInput />);
      const input = screen.getByRole("searchbox");
      expect(input).toHaveAttribute("placeholder", "Search...");
    });
  });
});

describe("Localization Keys", () => {
  it("ThemeSwitch uses localized accessible labels", () => {
    render(<ThemeSwitch />);
    const button = screen.getByRole("button");
    const label = button.getAttribute("aria-label");
    expect(label).toMatch(/Switch to (light|dark) mode/);
  });

  it("SearchInput uses localized placeholder", () => {
    render(<SearchInput />);
    const input = screen.getByRole("searchbox");
    expect(input.getAttribute("placeholder")).toBeTruthy();
  });
});
