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
import { isRouteCurrent } from "./dashboardRouteMatching";

export interface DashboardShellItem {
  href: string;
  iconClassName: string;
  labelKey: MessageKey;
  badgeKey?: MessageKey;
  exactMatch?: boolean;
}

export interface DashboardShellProps {
  children: ReactNode;
  navItems: readonly DashboardShellItem[];
  footerItems?: readonly DashboardShellItem[];
  homeHref?: string;
}

export function DashboardShell({
  children,
  footerItems = [],
  homeHref = Routes.Home,
  navItems,
}: DashboardShellProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { displayProfile, isAuthenticated, isLoading } = useAuthSession();
  const { t } = useLocalizedText();

  const navigate = useCallback((href: string) => router.push(href), [router]);

  const title = useMemo(() => {
    const items = [...navItems, ...footerItems];
    const currentItem = items.find((item) => isRouteCurrent(pathname, item));

    if (currentItem && currentItem.href !== homeHref) {
      return t(currentItem.labelKey);
    }

    if (isLoading || !isAuthenticated) {
      return t("home.welcomeBack");
    }

    const displayName = displayProfile?.givenName ?? displayProfile?.displayLabel ?? "";

    return t("home.welcomeBackNamed", { name: displayName });
  }, [displayProfile, footerItems, homeHref, isAuthenticated, isLoading, navItems, pathname, t]);

  const sidebarIdentity = useMemo(() => {
    if (isLoading) {
      return {
        initials: "",
        label: t("common.states.loading"),
        subtitle: t("appShell.brand"),
      };
    }

    if (!isAuthenticated) {
      return {
        initials: t("appShell.brand").slice(0, 2).toUpperCase(),
        label: t("appShell.brand"),
        subtitle: t("appShell.userMenu.logIn"),
      };
    }

    return {
      initials: displayProfile.initials ?? getInitials(displayProfile.displayLabel),
      label: displayProfile.displayLabel,
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
          navItems={navItems}
          pathname={pathname}
        />
      }
      sidebarCollapsible="offcanvas"
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
