"use client";

import { type ReactNode, useMemo } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useAuthSession } from "@vitarish/auth";
import {
  CenteredRegion,
  ContainedPage,
  DashboardFrame,
  type DashboardNavigationGroup,
} from "@vitarish/ui";
import { useLocalizedText } from "@vitarish/localization";
import { Routes } from "@/app/routes";

export function AppShell({ children }: { children: ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const { isAuthenticated, isLoading, displayProfile } = useAuthSession();
  const { t } = useLocalizedText();
  const navigationGroups = useMemo<DashboardNavigationGroup[]>(
    () => [
      {
        label: t("sea.navigation.title"),
        items: [
          {
            href: Routes.Root,
            icon: "fa-chart-pie",
            label: t("pm.overview"),
            exactMatch: true,
          },
          { href: Routes.Projects, icon: "fa-folder", label: t("pm.projects") },
          { href: Routes.Roles, icon: "fa-users", label: t("pm.roles") },
          {
            href: Routes.Statistics,
            icon: "fa-chart-simple",
            label: t("pm.statistics"),
          },
        ],
      },
    ],
    [t],
  );

  if (isLoading)
    return (
      <CenteredRegion>
        <p>{t("common.states.loading")}</p>
      </CenteredRegion>
    );
  if (!isAuthenticated)
    return (
      <CenteredRegion>
        <h1 className="text-2xl font-semibold">{t("pm.title")}</h1>
        <p className="mt-2 text-muted">{t("pm.signIn")}</p>
      </CenteredRegion>
    );

  return (
    <DashboardFrame
      contentMode="page"
      currentPath={pathname}
      identity={{
        accountHref: "/account",
        accountLabel: t("pm.account"),
        initials: getInitials(displayProfile?.displayLabel ?? ""),
        label: displayProfile?.displayLabel ?? t("pm.title"),
        subtitle: t("pm.title"),
      }}
      mobileNavigation="sidebar"
      navigationGroups={navigationGroups}
      navigationLabel={t("pm.title")}
      navigate={(href) => router.push(href)}
      skipLinkLabel={t("pm.skip")}
      title={
        <span className="font-semibold tracking-tight">{t("pm.title")}</span>
      }
    >
      <ContainedPage width="content">{children}</ContainedPage>
    </DashboardFrame>
  );
}

function getInitials(label: string) {
  return (
    label
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0])
      .join("")
      .toUpperCase() || "VP"
  );
}
