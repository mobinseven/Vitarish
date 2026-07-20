"use client";

import type { ReactNode } from "react";
import { useCallback, useMemo } from "react";
import { Button, Tooltip } from "@heroui/react";
import {
  DashboardFrame,
  type DashboardContentMode,
  type DashboardIdentity,
  type DashboardMobileNavigation,
  type DashboardNavigationGroup,
  type DashboardNavigationItem,
} from "@vitarish/web-ui";
import { usePathname, useRouter } from "next/navigation";
import { Routes } from "@/app/routes";
import { useAuthSession } from "@vitarish/auth-web";
import { useLocalizedText, type MessageKey } from "@/localization";

export interface DashboardShellItem {
  href: string;
  iconClassName: string;
  labelKey: MessageKey;
  badgeKey?: MessageKey;
  children?: readonly DashboardShellItem[];
  exactMatch?: boolean;
}

export interface DashboardShellNavGroup {
  labelKey: MessageKey;
  items: readonly DashboardShellItem[];
}

export interface DashboardShellProps {
  children: ReactNode;
  contentMode?: DashboardContentMode;
  navGroups: readonly DashboardShellNavGroup[];
  footerItems?: readonly DashboardShellItem[];
  mobileNavigation?: DashboardMobileNavigation;
}

export function DashboardShell({
  children,
  contentMode = "page",
  footerItems = [],
  mobileNavigation = "sidebar",
  navGroups,
}: DashboardShellProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { displayProfile, isAuthenticated, isLoading } = useAuthSession();
  const { t } = useLocalizedText();

  const navigate = useCallback((href: string) => router.push(href), [router]);
  const upHref = getBreadcrumbParent(pathname);

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
      contentMode={contentMode}
      currentPath={pathname}
      footerItems={footerNavigationItems}
      footerLabel={t("appShell.userMenu.signOut")}
      identity={sidebarIdentity}
      mobileNavigation={mobileNavigation}
      navigate={navigate}
      navigationGroups={navigationGroups}
      navigationLabel={t("appShell.navigation.dashboardMenu")}
      skipLinkLabel={t("appShell.navigation.skipToContent")}
      title={null}
      toolbar={
        upHref ? (
          <Tooltip>
            <Button
              aria-label={t("appShell.navigation.up")}
              isDisabled={!upHref}
              variant="tertiary"
              onPress={() => upHref && navigate(upHref)}
            >
              <i aria-hidden="true" className="fa-solid fa-arrow-up" />
              {t("appShell.navigation.up")}
            </Button>
            <Tooltip.Content>
              {t("appShell.navigation.up")}
            </Tooltip.Content>
          </Tooltip>
        ) : null
      }

    >
      {children}
    </DashboardFrame>
  );
}

function getBreadcrumbParent(pathname: string): string | undefined {
  const segments = pathname.split("/").filter(Boolean);

  if (pathname === Routes.Home) {
    return undefined;
  }

  return segments.length > 1 ? `/${segments.slice(0, -1).join("/")}` : Routes.Home;
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
