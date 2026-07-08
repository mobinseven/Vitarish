import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { DashboardShell, type DashboardShellNavGroup } from "@/components/shell/DashboardShell";
import { DashboardSidebar } from "@/components/shell/DashboardSidebar";
import { ThemeSwitch } from "@/components/shell/ThemeSwitch";
import { UserMenu } from "@/components/shell/UserMenu";
import { SearchInput } from "@/components/shell/SearchInput";
import { dashboardFooterItems, mainDashboardNavGroups } from "@/components/shell/dashboardShellNavigation";

vi.mock("next-themes", () => ({
  useTheme: () => ({
    setTheme: vi.fn(),
    theme: "light",
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
        "appShell.navigation.clinic": "Clinic",
        "appShell.navigation.knowledge": "Knowledge",
        "appShell.navigation.dashboardMenu": "Dashboard navigation",
        "appShell.navigation.mainGroup": "General",
        "appShell.navigation.clinicGroup": "Clinic",
        "appShell.navigation.staffGroup": "Staff",
        "appShell.actions.notifications": "Notifications",
        "appShell.actions.invite": "Invite",
        "appShell.theme.light": "Switch to light mode",
        "appShell.theme.dark": "Switch to dark mode",
        "appShell.theme.system": "Switch to system mode",
        "appShell.culture.label": "Language",
        "appShell.userMenu.logIn": "Log In",
        "appShell.userMenu.account": "Account",
        "appShell.userMenu.signOut": "Log Out",
        "appShell.menu.open": "Open menu",
        "appShell.search.label": "Search",
        "appShell.search.placeholder": "Search...",
        "account.title": "Account",
        "account.navigation.name": "Name",
        "account.navigation.email": "Email",
        "account.navigation.phone": "Phone",
        "account.navigation.password": "Password",
        "account.navigation.passkeys": "Passkeys",
        "account.navigation.theme": "Theme",
        "account.navigation.language": "Language",
        "clinic.patients.title": "Patients",
        "knowledge.qa.title": "Knowledge Q&A",
        "home.welcomeBack": "Welcome back",
      };

      if (key === "home.welcomeBackNamed") {
        return `Welcome back, ${parameters?.name}`;
      }

      return translations[key] ?? key;
    },
    locale: "en",
  }),
  setLocaleCookie: vi.fn(),
  supportedLocales: ["en", "fa"],
}));

const shellNavGroups = [
  {
    labelKey: "appShell.navigation.mainGroup",
    items: [
      { href: "/", iconClassName: "fa-solid fa-house", labelKey: "appShell.navigation.home", exactMatch: true },
    ],
  },
] as const satisfies readonly DashboardShellNavGroup[];

const shellFooterItems = [
  { href: "/account", iconClassName: "fa-solid fa-circle-user", labelKey: "appShell.userMenu.account" },
] as const;

const nestedShellNavGroups = [
  {
    labelKey: "appShell.navigation.mainGroup",
    items: [
      { href: "/home", iconClassName: "fa-solid fa-house", labelKey: "appShell.navigation.home", exactMatch: true },
      {
        href: "/clinic",
        iconClassName: "fa-solid fa-house-medical",
        labelKey: "appShell.navigation.clinic",
        exactMatch: true,
        children: [
          { href: "/clinic/patients", iconClassName: "fa-solid fa-users", labelKey: "clinic.patients.title" },
        ],
      },
      {
        href: "/knowledge",
        iconClassName: "fa-solid fa-book-open",
        labelKey: "appShell.navigation.knowledge",
        exactMatch: true,
        children: [
          { href: "/knowledge/qa", iconClassName: "fa-solid fa-comments", labelKey: "knowledge.qa.title" },
        ],
      },
    ],
  },
] as const satisfies readonly DashboardShellNavGroup[];

describe("Dashboard Shell Components", () => {
  describe("DashboardShell", () => {
    it("renders the template-style sidebar, navbar actions, and content", () => {
      render(
        <DashboardShell footerItems={shellFooterItems} homeHref="/" navGroups={shellNavGroups}>
          <div>Dashboard content</div>
        </DashboardShell>
      );

      expect(screen.getByRole("heading", { name: "Welcome back, Test User" })).toBeInTheDocument();
      expect(screen.getAllByText("Home").length).toBeGreaterThan(0);
      expect(screen.getByRole("button", { name: "Search" })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: "Notifications" })).toBeInTheDocument();
      expect(screen.getByText("Dashboard content")).toBeInTheDocument();
    });
  });

  describe("DashboardSidebar", () => {
    it("expands the current app group when the route is inside that app", () => {
      render(
        <DashboardSidebar
          footerItems={[]}
          identity={{ accountHref: "/account", initials: "TU", label: "Test User", subtitle: "Account" }}
          navGroups={nestedShellNavGroups}
          pathname="/clinic/patients"
        />
      );

      expect(screen.getByText("Clinic")).toBeInTheDocument();
      expect(screen.getByText("Patients")).toBeInTheDocument();
    });

    it("provides account access via sidebar identity header instead of navigation menu", () => {
      const [mainGroup] = mainDashboardNavGroups;
      const accountItem = mainGroup.items.find((item) => item.href === "/account");

      expect(dashboardFooterItems.map((item) => item.href)).not.toContain("/account");
      expect(accountItem).toBeUndefined();
      expect(mainDashboardNavGroups.map((group) => group.labelKey)).not.toContain("appShell.navigation.accountGroup");
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
