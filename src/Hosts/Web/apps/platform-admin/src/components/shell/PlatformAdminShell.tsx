"use client";

import { type ReactNode } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useLocalizedText } from "@/localization";
import { useLogout } from "@vitarish/auth-web";
import type { UserDisplayProfile } from "@vitarish/auth-web";
import type { CurrentAuthenticationUserDto } from "@vitarish/auth-web";
import { Routes } from "@/app/routes";

interface NavigationItem {
  id: string;
  label: string;
  href: string;
  iconClassName: string;
}

interface NavigationGroup {
  label?: string;
  items: NavigationItem[];
}

interface PlatformAdminShellProps {
  user: CurrentAuthenticationUserDto | null;
  displayProfile: UserDisplayProfile;
  children: ReactNode;
}

export function PlatformAdminShell({ user, displayProfile, children }: PlatformAdminShellProps) {
  const { t } = useLocalizedText();
  const pathname = usePathname();
  const router = useRouter();
  const { logout } = useLogout();

  const mainNavigation: NavigationGroup = {
    items: [
      { id: "overview", label: t("platformAdmin.nav.overview"), href: Routes.Dashboard, iconClassName: "fa-solid fa-gauge" },
      { id: "users", label: t("platformAdmin.nav.users"), href: Routes.Users.Index, iconClassName: "fa-solid fa-users" },
      { id: "owners", label: t("platformAdmin.nav.owners"), href: Routes.Owners.Index, iconClassName: "fa-solid fa-user-tie" },
      { id: "tenants", label: t("platformAdmin.nav.tenants"), href: Routes.Tenants.Index, iconClassName: "fa-solid fa-building" },
    ],
  };

  const operationsNavigation: NavigationGroup = {
    label: t("platformAdmin.nav.operations"),
    items: [
      { id: "health", label: t("platformAdmin.nav.health"), href: Routes.Operations.Health, iconClassName: "fa-solid fa-heart-pulse" },
      { id: "search", label: t("platformAdmin.nav.search"), href: Routes.Operations.Search, iconClassName: "fa-solid fa-magnifying-glass" },
      { id: "communications", label: t("platformAdmin.nav.communications"), href: Routes.Operations.Communications, iconClassName: "fa-solid fa-envelope" },
    ],
  };

  const auditNavigation: NavigationGroup = {
    items: [
      { id: "audit", label: t("platformAdmin.nav.audit"), href: Routes.Audit, iconClassName: "fa-solid fa-clipboard-list" },
    ],
  };

  const navigationGroups = [mainNavigation, operationsNavigation, auditNavigation];

  function isActive(href: string) {
    if (href === "/") return pathname === "/";
    return pathname.startsWith(href);
  }

  return (
    <div className="flex min-h-screen">
      {/* Sidebar */}
      <aside className="hidden md:flex md:w-64 md:flex-col border-e border-separator bg-surface-secondary">
        <div className="flex h-14 items-center px-4 border-b border-separator">
          <span className="text-lg font-bold">{t("platformAdmin.shell.title")}</span>
        </div>
        <nav className="flex-1 overflow-y-auto py-2" aria-label={t("platformAdmin.nav.overview")}>
          {navigationGroups.map((group, gi) => (
            <div key={gi} className="mb-2">
              {group.label && (
                <p className="px-4 py-1 text-xs font-medium text-muted uppercase tracking-wide">{group.label}</p>
              )}
              <ul>
                {group.items.map((item) => (
                  <li key={item.id}>
                    <a
                      href={item.href}
                      aria-current={isActive(item.href) ? "page" : undefined}
                      className={`flex items-center gap-3 px-4 py-2 text-sm transition-colors ${
                        isActive(item.href)
                          ? "bg-accent/10 text-accent font-medium"
                          : "text-foreground hover:bg-default/50"
                      }`}
                    >
                      <i className={`${item.iconClassName} w-4 text-center`} aria-hidden="true" />
                      <span>{item.label}</span>
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </nav>
        <div className="border-t border-separator p-4">
          <div className="flex items-center justify-between">
            <span className="text-sm truncate">{displayProfile.displayLabel}</span>
            <button
              type="button"
              onClick={() => {
                logout();
                router.replace(Routes.Login.Index);
              }}
              className="text-xs text-muted hover:text-foreground"
              aria-label={t("auth.login.logoutAction")}
            >
              <i className="fa-solid fa-right-from-bracket" aria-hidden="true" />
            </button>
          </div>
        </div>
      </aside>

      {/* Main content */}
      <div className="flex flex-1 flex-col">
        {/* Top bar for mobile */}
        <header className="flex h-14 items-center gap-4 border-b border-separator px-4 md:hidden">
          <span className="text-lg font-bold">{t("platformAdmin.shell.title")}</span>
        </header>
        <main className="flex-1">
          {children}
        </main>
      </div>
    </div>
  );
}
