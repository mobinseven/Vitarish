import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import {
  DashboardFrame,
  type DashboardNavigationGroup,
} from "@vitarish/web-ui";
import {
  DashboardShell,
  type DashboardShellNavGroup,
} from "@/components/shell/DashboardShell";
import { UserMenu } from "@/components/shell/UserMenu";
import { SearchInput } from "@/components/shell/SearchInput";
import {
  dashboardFooterItems,
  mainDashboardNavGroups,
} from "@/components/shell/dashboardShellNavigation";

const routerPush = vi.hoisted(() => vi.fn());
const tenantAccess = vi.hoisted(() => ({ value: true }));

vi.mock("next/navigation", () => ({
  usePathname: () => "/patients/example",
  useRouter: () => ({
    back: vi.fn(),
    forward: vi.fn(),
    prefetch: vi.fn(),
    push: routerPush,
    refresh: vi.fn(),
    replace: vi.fn(),
  }),
}));

vi.mock("next-themes", () => ({
  useTheme: () => ({
    setTheme: vi.fn(),
    theme: "light",
    resolvedTheme: "light",
  }),
}));

vi.mock("@vitarish/auth-web", () => ({
  useAuthSession: () => ({
    isAuthenticated: true,
    isLoading: false,
    displayProfile: {
      displayLabel: "Test User",
      initials: "TU",
    },
  }),
  useLogout: () => ({
    logout: vi.fn(),
    isLoading: false,
  }),
  useTenantAuthorization: () => ({
    hasAccess: tenantAccess.value,
  }),
}));

vi.mock("@vitarish/clinic-web", () => ({
  OperationsAgentAside: ({ isOpen }: { isOpen: boolean }) => (
    <div data-testid="operations-agent" data-open={String(isOpen)} />
  ),
}));

vi.mock("@/localization", () => ({
  useLocalizedText: () => ({
    t: (key: string, parameters?: Record<string, string>) => {
      const translations: Record<string, string> = {
        "appShell.brand": "Vitarish",
        "appShell.navigation.home": "Home",
        "appShell.navigation.up": "Up",
        "appShell.navigation.clinic": "Clinic",
        "appShell.navigation.knowledge": "Knowledge",
        "appShell.navigation.dashboardMenu": "Dashboard navigation",
        "appShell.navigation.mainGroup": "General",
        "appShell.navigation.clinicGroup": "Clinic",
        "appShell.navigation.staffGroup": "Staff",
        "appShell.actions.notifications": "Notifications",
        "appShell.actions.invite": "Invite",
        "appShell.operationsAssistant": "Operations assistant",
        "appShell.theme.light": "Light mode",
        "appShell.theme.dark": "Dark mode",
        "appShell.theme.system": "System preference",
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

vi.mock("@vitarish/web-localization", () => ({
  useLocalizedText: () => ({
    t: (key: string) =>
      ({
        "appShell.culture.label": "Language",
        "appShell.theme.light": "Switch to light mode",
        "appShell.theme.dark": "Switch to dark mode",
        "appShell.theme.system": "Switch to system mode",
      })[key] ?? key,
    locale: "en",
  }),
  setLocaleCookie: vi.fn(),
  supportedLocales: ["en", "fa"],
}));

vi.mock("@heroui-pro/react/sidebar", () => {
  // eslint-disable-next-line @typescript-eslint/no-require-imports
  const React = require("react");
  const MenuItem = React.forwardRef(
    ({ children, id, textValue, isCurrent, ...props }, ref) => (
      <li
        ref={ref}
        role="treeitem"
        aria-selected={isCurrent ? "true" : "false"}
        aria-current={isCurrent ? "true" : undefined}
        data-id={id}
        data-text={textValue}
        {...props}
      >
        {children}
      </li>
    ),
  );
  MenuItem.displayName = "Sidebar.MenuItem";

  return {
    Sidebar: Object.assign(
      ({ children, ...props }) => <aside {...props}>{children}</aside>,
      {
        Provider: ({ children }) => <div>{children}</div>,
        Header: ({ children }) => <div>{children}</div>,
        Content: ({ children }) => <div>{children}</div>,
        Footer: ({ children }) => <div>{children}</div>,
        Group: ({ children }) => <div>{children}</div>,
        GroupLabel: ({ children }) => <div>{children}</div>,
        Menu: ({ children }) => <ul role="tree">{children}</ul>,
        MenuSection: ({ children }) => <div>{children}</div>,
        MenuHeader: ({ children }) => <div>{children}</div>,
        MenuItem,
        MenuIcon: ({ children }) => <span>{children}</span>,
        MenuLabel: ({ children }) => (
          <span data-sidebar="label">{children}</span>
        ),
        MenuChip: ({ children }) => <span>{children}</span>,
        MenuActions: ({ children }) => <div>{children}</div>,
        MenuAction: ({ children, ...props }) => (
          <button {...props}>{children}</button>
        ),
        MenuTrigger: ({ children }) => <span>{children}</span>,
        MenuIndicator: ({ children }) => <span>{children}</span>,
        Submenu: ({ children }) => <ul role="group">{children}</ul>,
        Separator: () => <hr />,
        Rail: () => null,
        Trigger: ({ children }) => <button>{children}</button>,
        Mobile: ({ children }) => <div>{children}</div>,
        Tooltip: ({ children }) => <div>{children}</div>,
      },
    ),
  };
});

const shellNavGroups = [
  {
    labelKey: "appShell.navigation.mainGroup",
    items: [
      {
        href: "/",
        icon: "house",
        labelKey: "appShell.navigation.home",
        exactMatch: true,
      },
    ],
  },
] as const satisfies readonly DashboardShellNavGroup[];

const shellFooterItems = [
  {
    href: "/account",
    icon: "circle-user",
    labelKey: "appShell.userMenu.account",
  },
] as const;

const nestedNavigationGroups = [
  {
    label: "General",
    items: [
      { href: "/home", icon: "house", label: "Home", exactMatch: true },
      {
        href: "/clinic/patients",
        icon: "users",
        label: "Patients",
        exactMatch: true,
      },
      {
        href: "/knowledge",
        icon: "book-open",
        label: "Knowledge",
        exactMatch: true,
        children: [
          { href: "/knowledge/qa", icon: "comments", label: "Knowledge Q&A" },
        ],
      },
    ],
  },
] as const satisfies readonly DashboardNavigationGroup[];

describe("Dashboard Shell Components", () => {
  describe("DashboardShell", () => {
    it("renders the template-style sidebar, navbar actions, and content", () => {
      render(
        <DashboardShell
          footerItems={shellFooterItems}
          navGroups={shellNavGroups}
        >
          <div>Dashboard content</div>
        </DashboardShell>,
      );

      expect(screen.getAllByText("Home").length).toBeGreaterThan(0);
      expect(screen.getAllByText("Language").length).toBeGreaterThan(0);
      expect(
        screen.getAllByText("Switch to light mode").length,
      ).toBeGreaterThan(0);
      expect(screen.getByRole("button", { name: "Up" })).toBeInTheDocument();
      expect(screen.getByText("Dashboard content")).toBeInTheDocument();
    });

    it("navigates to the parent route when the Up button is pressed", () => {
      render(
        <DashboardShell
          footerItems={shellFooterItems}
          navGroups={shellNavGroups}
        >
          <div>Dashboard content</div>
        </DashboardShell>,
      );

      fireEvent.click(screen.getByRole("button", { name: "Up" }));

      expect(routerPush).toHaveBeenCalledWith("/patients");
    });

    it("composes the operations assistant only for an authorized clinic owner", () => {
      tenantAccess.value = true;
      const { rerender } = render(
        <DashboardShell
          clinicOperationsAssistant
          footerItems={shellFooterItems}
          navGroups={shellNavGroups}
        >
          <div>Clinic content</div>
        </DashboardShell>,
      );

      expect(screen.getAllByTestId("operations-agent").length).toBeGreaterThan(
        0,
      );

      tenantAccess.value = false;
      rerender(
        <DashboardShell
          clinicOperationsAssistant
          footerItems={shellFooterItems}
          navGroups={shellNavGroups}
        >
          <div>Clinic content</div>
        </DashboardShell>,
      );

      expect(screen.queryByTestId("operations-agent")).not.toBeInTheDocument();
      tenantAccess.value = true;
    });
  });

  describe("DashboardFrame", () => {
    it("expands the current app group when the route is inside that app", () => {
      render(
        <DashboardFrame
          contentMode="page"
          currentPath="/clinic/patients"
          footerItems={[]}
          identity={{
            accountHref: "/account",
            accountLabel: "Account",
            initials: "TU",
            label: "Test User",
            subtitle: "Account",
          }}
          navigate={vi.fn()}
          mobileNavigation="sidebar"
          navigationGroups={nestedNavigationGroups}
          navigationLabel="Dashboard navigation"
          skipLinkLabel="Skip to content"
          title="Current page"
        >
          <div>Dashboard content</div>
        </DashboardFrame>,
      );

      expect(screen.getAllByText("Patients").length).toBeGreaterThan(0);
    });

    it("provides account access via sidebar identity header instead of navigation menu", () => {
      const [mainGroup] = mainDashboardNavGroups;
      const accountItem = mainGroup.items.find(
        (item) => item.href === "/account",
      );

      expect(dashboardFooterItems.map((item) => item.href)).not.toContain(
        "/account",
      );
      expect(accountItem).toBeUndefined();
      expect(
        mainDashboardNavGroups.map((group) => group.labelKey),
      ).not.toContain("appShell.navigation.accountGroup");
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
  it("SearchInput uses localized placeholder", () => {
    render(<SearchInput />);
    const input = screen.getByRole("searchbox");
    expect(input.getAttribute("placeholder")).toBeTruthy();
  });
});
