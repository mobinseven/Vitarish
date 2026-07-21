"use client";

import { useCallback, useMemo } from "react";
import { Button, Tooltip } from "@heroui/react";
import { usePathname, useRouter } from "next/navigation";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";
import { FaIcon } from "@vitarish/web-ui";
import type { DashboardShellItem, DashboardShellNavGroup } from "./DashboardShell";

interface BreadcrumbUpButtonProps {
  navGroups: readonly DashboardShellNavGroup[];
}

export function BreadcrumbUpButton({ navGroups }: BreadcrumbUpButtonProps) {
  const pathname = usePathname();
  const router = useRouter();
  const { direction, t } = useLocalizedText();

  const navigate = useCallback((href: string) => router.push(href), [router]);
  const sidebarHrefs = useMemo(() => collectSidebarHrefs(navGroups), [navGroups]);
  const routeParent = getBreadcrumbParent(pathname);
  const href = routeParent && !sidebarHrefs.has(routeParent) ? routeParent : undefined;

  if (!href) return null;

  return (
    <Tooltip>
      <Button
        aria-label={t("appShell.navigation.up")}
        isIconOnly
        variant="secondary"
        onPress={() => navigate(href)}
      >
        <FaIcon icon={direction === "rtl" ? "arrow-left" : "arrow-right"} className="size-4" />
      </Button>
      <Tooltip.Content>
        {t("appShell.navigation.up")}
      </Tooltip.Content>
    </Tooltip>
  );
}

// ── Route-tree breadcrumb parent resolution ──

type RouteBreadcrumbPattern = {
  pattern: RegExp;
  parent: (match: RegExpMatchArray) => string | undefined;
};

/** Ordered most-specific first — first match wins. */
const routeBreadcrumbPatterns: readonly RouteBreadcrumbPattern[] = [
  // ── Patients ──
  { pattern: /^\/patients\/new$/, parent: () => Routes.Clinic.Patients.Index },
  { pattern: /^\/patients\/([^/]+)\/edit$/, parent: (m) => Routes.Clinic.Patients.Detail(m[1]) },
  {
    pattern: /^\/patients\/([^/]+)\/procedures\/([^/]+)\/events\/([^/]+)$/,
    parent: (m) => Routes.Clinic.Patients.Procedure(m[1], m[2]),
  },
  {
    pattern: /^\/patients\/([^/]+)\/procedures\/([^/]+)$/,
    parent: (m) => Routes.Clinic.Patients.Detail(m[1]),
  },
  { pattern: /^\/patients\/([^/]+)$/, parent: () => Routes.Clinic.Patients.Index },
  { pattern: /^\/patients$/, parent: () => Routes.Home },

  // ── Appointments ──
  {
    pattern: /^\/appointments\/shifts\/new\/([^/]+)$/,
    parent: () => Routes.Clinic.Appointments.Shifts.Index,
  },
  { pattern: /^\/appointments\/shifts\/new$/, parent: () => Routes.Clinic.Appointments.Shifts.Index },
  {
    pattern: /^\/appointments\/shifts\/([^/]+)\/occurrence\/([^/]+)$/,
    parent: (m) => Routes.Clinic.Appointments.Shifts.Detail(m[1]),
  },
  {
    pattern: /^\/appointments\/shifts\/([^/]+)\/edit$/,
    parent: (m) => Routes.Clinic.Appointments.Shifts.Detail(m[1]),
  },
  { pattern: /^\/appointments\/shifts\/([^/]+)$/, parent: () => Routes.Clinic.Appointments.Shifts.Index },
  { pattern: /^\/appointments\/shifts$/, parent: () => Routes.Clinic.Appointments.Reservations.Index },
  {
    pattern: /^\/appointments\/service-types\/([^/]+)$/,
    parent: () => Routes.Clinic.Appointments.ServiceTypes.Index,
  },
  { pattern: /^\/appointments\/service-types$/, parent: () => Routes.Clinic.Appointments.Reservations.Index },
  {
    pattern: /^\/appointments\/services\/([^/]+)$/,
    parent: () => Routes.Clinic.Appointments.Services.Index,
  },
  { pattern: /^\/appointments\/services$/, parent: () => Routes.Clinic.Appointments.Reservations.Index },
  {
    pattern: /^\/appointments\/service-providers\/([^/]+)$/,
    parent: () => Routes.Clinic.Appointments.ServiceProviders.Index,
  },
  { pattern: /^\/appointments\/service-providers$/, parent: () => Routes.Clinic.Appointments.Reservations.Index },
  { pattern: /^\/appointments\/settings$/, parent: () => Routes.Clinic.Appointments.Reservations.Index },
  { pattern: /^\/appointments\/([^/]+)$/, parent: () => Routes.Clinic.Appointments.Reservations.Index },
  { pattern: /^\/appointments$/, parent: () => Routes.Home },

  // ── Settings ──
  { pattern: /^\/settings\/users$/, parent: () => Routes.Clinic.Settings.Index },
  { pattern: /^\/settings\/statistics$/, parent: () => Routes.Clinic.Settings.Index },
  { pattern: /^\/settings$/, parent: () => Routes.Home },

  // ── Knowledge ──
  { pattern: /^\/knowledge\/qa$/, parent: () => Routes.Knowledge.Index },
  { pattern: /^\/knowledge$/, parent: () => Routes.Home },

  // ── Account ──
  { pattern: /^\/account\/.+$/, parent: () => Routes.Account.Index },
  { pattern: /^\/account$/, parent: () => Routes.Home },

  // ── Other top-level ──
  { pattern: /^\/reminders$/, parent: () => Routes.Home },
  { pattern: /^\/check-in$/, parent: () => Routes.Home },
  { pattern: /^\/home$/, parent: () => undefined },
];

function getBreadcrumbParent(pathname: string): string | undefined {
  for (const { pattern, parent } of routeBreadcrumbPatterns) {
    const match = pathname.match(pattern);
    if (match) return parent(match);
  }
  return undefined;
}

function collectSidebarHrefs(groups: readonly DashboardShellNavGroup[]): Set<string> {
  const hrefs = new Set<string>();
  const walk = (items: readonly DashboardShellItem[]) => {
    for (const item of items) {
      hrefs.add(item.href);
      if (item.children) walk(item.children);
    }
  };
  for (const group of groups) walk(group.items);
  return hrefs;
}
