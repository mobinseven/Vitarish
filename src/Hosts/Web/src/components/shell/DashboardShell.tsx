"use client";

import type { ReactNode } from "react";
import { useCallback, useMemo } from "react";
import { AppLayout } from "@heroui-pro/react";
import { usePathname, useRouter } from "next/navigation";
import { Routes } from "@/app/routes";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { useLocalizedText, type MessageKey } from "@/localization";
import { DashboardNavbar } from "./DashboardNavbar";
import { DashboardSidebar } from "./DashboardSidebar";
import { findCurrentRouteItem } from "./dashboardRouteMatching";

export interface DashboardShellItem {
  href: string;
  iconClassName: string;
  labelKey: MessageKey;
  badgeKey?: MessageKey;
  children?: readonly DashboardShellItem[];
  exactMatch?: boolean;
  titleLabelKey?: MessageKey;
}

export interface DashboardShellNavGroup {
  labelKey: MessageKey;
  items: readonly DashboardShellItem[];
}

export interface DashboardShellProps {
  children: ReactNode;
  navGroups: readonly DashboardShellNavGroup[];
  footerItems?: readonly DashboardShellItem[];
  homeHref?: string;
}

export function DashboardShell({
  children,
  footerItems = [],
  homeHref = Routes.Home,
  navGroups,
}: DashboardShellProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { displayProfile, isAuthenticated, isLoading } = useAuthSession();
  const { t } = useLocalizedText();

  const navigate = useCallback((href: string) => router.push(href), [router]);

  const title = useMemo(() => {
    const allItems = [...navGroups.flatMap((g) => g.items), ...footerItems];
    const currentItem = findCurrentRouteItem(pathname, allItems);

    if (currentItem && currentItem.href !== homeHref) {
      return t(currentItem.titleLabelKey ?? currentItem.labelKey);
    }

    if (isLoading || !isAuthenticated) {
      return t("home.welcomeBack");
    }

    const displayName = displayProfile?.givenName ?? displayProfile?.displayLabel ?? "";

    return t("home.welcomeBackNamed", { name: displayName });
  }, [displayProfile, footerItems, homeHref, isAuthenticated, isLoading, navGroups, pathname, t]);

  const sidebarIdentity = useMemo(() => {
    if (isLoading) {
      return {
        accountHref: Routes.Account.Index,
        initials: "",
        label: t("common.states.loading"),
        subtitle: t("appShell.brand"),
      };
    }

    if (!isAuthenticated) {
      return {
        accountHref: Routes.Account.Index,
        initials: t("appShell.brand").slice(0, 2).toUpperCase(),
        label: t("appShell.brand"),
        subtitle: t("appShell.userMenu.logIn"),
      };
    }

    return {
      accountHref: Routes.Account.Index,
      initials: displayProfile?.initials ?? getInitials(displayProfile?.displayLabel ?? ""),
      label: displayProfile?.displayLabel ?? "",
      subtitle: t("appShell.userMenu.account"),
    };
  }, [displayProfile, isAuthenticated, isLoading, t]);

  return (
    <AppLayout
      navigate={navigate}
      navbar={<DashboardNavbar title={title} />}
      sidebar={
        <DashboardSidebar
          footerItems={footerItems}
          identity={sidebarIdentity}
          navGroups={navGroups}
          onNavigate={navigate}
          pathname={pathname}
        />
      }
      sidebarCollapsible="icon"
    >
      {children}
    </AppLayout>
  );
}

function getInitials(label: string) {
  return label
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join("")
    .toUpperCase();
}
