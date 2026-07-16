"use client";

import type { ReactNode } from "react";
import { useCallback, useMemo } from "react";
import { Breadcrumbs } from "@heroui/react";
import {
  DashboardFrame,
  IconButton,
  type DashboardIdentity,
  type DashboardNavigationGroup,
  type DashboardNavigationItem,
} from "@vitarish/web-ui";
import { usePathname, useRouter } from "next/navigation";
import { Routes } from "@/app/routes";
import { useAuthSession } from "@vitarish/auth-web";
import { useLocalizedText, type MessageKey } from "@/localization";
import { CultureSwitcher } from "./CultureSwitcher";
import { ThemeSwitch } from "./ThemeSwitch";
import { isRouteBranchCurrent } from "./dashboardRouteMatching";

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

  const breadcrumbs = useMemo(() => {
    const allItems = [...navGroups.flatMap((g) => g.items), ...footerItems];
    const trail = findRouteBreadcrumbs(pathname, allItems).map((item) => ({
      href: item.href,
      label: t(item.titleLabelKey ?? item.labelKey),
    }));
    const homeCrumb = { href: homeHref, label: t("appShell.navigation.home") };
    const items = trail[0]?.href === homeHref ? trail : [homeCrumb, ...trail];

    return (
      <Breadcrumbs aria-label={t("appShell.navigation.dashboardMenu")} className="min-w-0 truncate">
        {items.map((item, index) => (
          <Breadcrumbs.Item
            key={`${item.href}-${index}`}
            onPress={index === items.length - 1 ? undefined : () => navigate(item.href)}
            aria-current={index === items.length - 1 ? "page" : undefined}
          >
            {item.label}
          </Breadcrumbs.Item>
        ))}
      </Breadcrumbs>
    );
  }, [footerItems, homeHref, navigate, navGroups, pathname, t]);

  const sidebarIdentity = useMemo<DashboardIdentity>(() => {
    if (isLoading) {
      return {
        accountHref: Routes.Account.Index,
        accountLabel: t("appShell.userMenu.account"),
        initials: "",
        label: t("common.states.loading"),
        subtitle: t("appShell.brand"),
      };
    }

    if (!isAuthenticated) {
      return {
        accountHref: Routes.Account.Index,
        accountLabel: t("appShell.userMenu.account"),
        initials: t("appShell.brand").slice(0, 2).toUpperCase(),
        label: t("appShell.brand"),
        subtitle: t("appShell.userMenu.logIn"),
      };
    }

    return {
      accountHref: Routes.Account.Index,
      accountLabel: t("appShell.userMenu.account"),
      initials: displayProfile?.initials ?? getInitials(displayProfile?.displayLabel ?? ""),
      label: displayProfile?.displayLabel ?? "",
      subtitle: t("appShell.userMenu.account"),
    };
  }, [displayProfile, isAuthenticated, isLoading, t]);

  const navigationGroups = useMemo(
    () => navGroups.map((group) => toNavigationGroup(group, t)),
    [navGroups, t]
  );
  const footerNavigationItems = useMemo(
    () => footerItems.map((item) => toNavigationItem(item, t)),
    [footerItems, t]
  );

  return (
    <DashboardFrame
      currentPath={pathname}
      footerItems={footerNavigationItems}
      footerLabel={t("appShell.userMenu.signOut")}
      identity={sidebarIdentity}
      navigate={navigate}
      navigationGroups={navigationGroups}
      navigationLabel={t("appShell.navigation.dashboardMenu")}
      title={breadcrumbs}
      toolbar={
        <>
          <CultureSwitcher />
          <ThemeSwitch />
        </>
      }
    >
      {children}
    </DashboardFrame>
  );
}

function findRouteBreadcrumbs(pathname: string, items: readonly DashboardShellItem[]): DashboardShellItem[] {
  for (const item of items) {
    if (item.children) {
      const childTrail = findRouteBreadcrumbs(pathname, item.children);
      if (childTrail.length > 0) {
        return [item, ...childTrail];
      }
    }

    if (isRouteBranchCurrent(pathname, item)) {
      return [item];
    }
  }

  return [];
}

function toNavigationGroup(group: DashboardShellNavGroup, t: (key: MessageKey) => string): DashboardNavigationGroup {
  return {
    label: t(group.labelKey),
    items: group.items.map((item) => toNavigationItem(item, t)),
  };
}

function toNavigationItem(item: DashboardShellItem, t: (key: MessageKey) => string): DashboardNavigationItem {
  return {
    href: item.href,
    iconClassName: item.iconClassName,
    label: t(item.labelKey),
    badge: item.badgeKey ? t(item.badgeKey) : undefined,
    children: item.children?.map((child) => toNavigationItem(child, t)),
    exactMatch: item.exactMatch,
  };
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
