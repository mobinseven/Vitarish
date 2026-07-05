import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
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
    t: (key: string) => {
      const translations: Record<string, string> = {
        "appShell.brand": "Vitarish",
        "appShell.navigation.home": "Home",
        "appShell.navigation.knowledge": "Knowledge",
        "appShell.theme.light": "Switch to light mode",
        "appShell.theme.dark": "Switch to dark mode",
        "appShell.userMenu.logIn": "Log In",
        "appShell.userMenu.account": "Account",
        "appShell.userMenu.signOut": "Log Out",
        "appShell.menu.open": "Open menu",
        "appShell.search.label": "Search",
        "appShell.search.placeholder": "Search...",
      };
      return translations[key] ?? key;
    },
  }),
}));

describe("Dashboard Shell Components", () => {
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
