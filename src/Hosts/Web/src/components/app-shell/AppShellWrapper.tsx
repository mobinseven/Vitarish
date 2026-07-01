"use client";

import { AppShell } from "./AppShell";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";
import type { NavItem } from "./types";

export type AppShellWrapperProps = {
  children: React.ReactNode;
  navItems?: NavItem[];
  sidebar?: React.ReactNode;
  hideNavbar?: boolean;
  hideFooter?: boolean;
  onSearch?: (query: string) => void;
};

export function AppShellWrapper({
  children,
  navItems,
  sidebar,
  hideNavbar,
  hideFooter,
  onSearch,
}: AppShellWrapperProps) {
  const { t } = useLocalizedText();

  const defaultNavItems: NavItem[] = [
    { href: Routes.Home, label: t("appShell.navigation.home") },
    { href: Routes.Knowledge.Index, label: t("appShell.navigation.knowledge") },
  ];

  return (
    <AppShell
      navItems={navItems ?? defaultNavItems}
      sidebar={sidebar}
      hideNavbar={hideNavbar}
      hideFooter={hideFooter}
      onSearch={onSearch}
    >
      {children}
    </AppShell>
  );
}
