"use client";

import { Button } from "@heroui/react";
import { AppLayout, Navbar, Sidebar } from "@heroui-pro/react";
import { useLocalizedText } from "@/localization";
import { CultureSwitcher } from "./CultureSwitcher";
import { IconButton } from "./IconButton";
import { ThemeSwitch } from "./ThemeSwitch";

export interface DashboardNavbarProps {
  title: string;
}

export function DashboardNavbar({ title }: DashboardNavbarProps) {
  const { t } = useLocalizedText();

  return (
    <Navbar maxWidth="full" hideOnScroll>
      <Navbar.Header>
        <AppLayout.MenuToggle />
        <Sidebar.Trigger />
        <h1 className="text-foreground truncate text-xl font-semibold">{title}</h1>
        <Navbar.Spacer />
        <div className="flex items-center gap-2">
          <IconButton label={t("appShell.search.label")} size="sm" variant="tertiary">
            <i aria-hidden="true" className="fa-solid fa-magnifying-glass size-4" />
          </IconButton>
          <IconButton label={t("appShell.actions.notifications")} size="sm" variant="tertiary">
            <i aria-hidden="true" className="fa-solid fa-bell size-4" />
          </IconButton>
          <CultureSwitcher />
          <ThemeSwitch />
        </div>
      </Navbar.Header>
    </Navbar>
  );
}
