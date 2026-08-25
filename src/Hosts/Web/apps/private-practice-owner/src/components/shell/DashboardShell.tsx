"use client";

import type { ReactNode } from "react";
import { useCallback, useMemo, useState } from "react";
import {
  DashboardFrame,
  type DashboardContentMode,
  type DashboardIdentity,
  type DashboardMobileNavigation,
  type DashboardNavigationGroup,
  type DashboardNavigationItem,
} from "@vitarish/ui";
import { usePathname, useRouter } from "next/navigation";
import { Routes } from "@/app/routes";
import { useAuthSession } from "@vitarish/auth";
import { useTenantAuthorization } from "@vitarish/auth";
import { ClinicAssistantAside, type ClinicAssistantScope } from "@vitarish/clinic";
import { useLocalizedText, type MessageKey } from "@/localization";
import { BreadcrumbUpButton } from "./BreadcrumbUpButton";
import { CommandPalette } from "./CommandPalette/CommandPalette";

export interface DashboardShellItem {
  href: string;
  icon: string;
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
  clinicOperationsAssistant?: boolean;
}

export function DashboardShell({
  children,
  contentMode = "page",
  footerItems = [],
  mobileNavigation = "sidebar",
  clinicOperationsAssistant = false,
  navGroups,
}: DashboardShellProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { displayProfile, isAuthenticated, isLoading } = useAuthSession();
  const { t } = useLocalizedText();
  const { hasAccess: canUseOperationsAssistant } = useTenantAuthorization("owner");
  const [operationsAssistantOpen, setOperationsAssistantOpen] = useState(false);
  const showOperationsAssistant = clinicOperationsAssistant && canUseOperationsAssistant;
  const clinicAssistantScope = useMemo(() => resolveClinicAssistantScope(pathname), [pathname]);
  const operationsAssistant = showOperationsAssistant ? <ClinicAssistantAside isOpen={operationsAssistantOpen} scope={clinicAssistantScope} onNavigateToPatient={(patientId) => router.push(Routes.Clinic.Patients.Detail(patientId))} /> : undefined;

  const navigate = useCallback((href: string) => router.push(href), [router]);

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
      initials:
        displayProfile?.initials ??
        getInitials(displayProfile?.displayLabel ?? ""),
      label: displayProfile?.displayLabel ?? "",
      subtitle: t("appShell.userMenu.account"),
    };
  }, [displayProfile, isAuthenticated, isLoading, t]);

  const navigationGroups = useMemo(
    () => navGroups.map((group) => toNavigationGroup(group, t)),
    [navGroups, t],
  );
  const footerNavigationItems = useMemo(
    () => footerItems.map((item) => toNavigationItem(item, t)),
    [footerItems, t],
  );

  return (
    <DashboardFrame
      aside={operationsAssistant}
      asideMobile={showOperationsAssistant ? "sheet" : "hidden"}
      asideOpen={operationsAssistantOpen}
      asideToggleLabel={showOperationsAssistant ? t("appShell.operationsAssistant") : undefined}
      contentMode={contentMode}
      currentPath={pathname}
      footerItems={footerNavigationItems}
      footerLabel={t("appShell.userMenu.signOut")}
      identity={sidebarIdentity}
      mobileNavigation={mobileNavigation}
      mobileAside={operationsAssistant}
      navigate={navigate}
      navigationGroups={navigationGroups}
      navigationLabel={t("appShell.navigation.dashboardMenu")}
      onAsideOpenChange={setOperationsAssistantOpen}
      skipLinkLabel={t("appShell.navigation.skipToContent")}
      title={null}
      toolbar={
        <>
          <CommandPalette />
          <BreadcrumbUpButton navGroups={navGroups} />
        </>
      }
    >
      {children}
    </DashboardFrame>
  );
}

function resolveClinicAssistantScope(pathname: string): ClinicAssistantScope {
  const match = /^\/patients\/([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})(?:\/|$)/i.exec(pathname);
  return match ? { kind: "patient", patientId: match[1] } : { kind: "practice" };
}

function toNavigationGroup(
  group: DashboardShellNavGroup,
  t: (key: MessageKey) => string,
): DashboardNavigationGroup {
  return {
    label: t(group.labelKey),
    items: group.items.map((item) => toNavigationItem(item, t)),
  };
}

function toNavigationItem(
  item: DashboardShellItem,
  t: (key: MessageKey) => string,
): DashboardNavigationItem {
  return {
    href: item.href,
    icon: item.icon,
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
