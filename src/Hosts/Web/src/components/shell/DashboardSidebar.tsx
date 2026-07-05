"use client";

import { Avatar, Chip } from "@heroui/react";
import { Sidebar } from "@heroui-pro/react";
import { useLocalizedText } from "@/localization";
import type { DashboardShellItem } from "./DashboardShell";
import { isRouteCurrent } from "./dashboardRouteMatching";

export interface DashboardSidebarIdentity {
  initials: string;
  label: string;
  subtitle: string;
}

export interface DashboardSidebarProps {
  footerItems: readonly DashboardShellItem[];
  identity: DashboardSidebarIdentity;
  navItems: readonly DashboardShellItem[];
  pathname: string;
}

export function DashboardSidebar({ footerItems, identity, navItems, pathname }: DashboardSidebarProps) {
  return (
    <>
      <Sidebar>
        <DashboardSidebarContents
          footerItems={footerItems}
          identity={identity}
          navItems={navItems}
          pathname={pathname}
        />
      </Sidebar>
      <Sidebar.Mobile>
        <DashboardSidebarContents
          footerItems={footerItems}
          identity={identity}
          idPrefix="mobile-"
          navItems={navItems}
          pathname={pathname}
        />
      </Sidebar.Mobile>
    </>
  );
}

interface DashboardSidebarContentsProps extends DashboardSidebarProps {
  idPrefix?: string;
}

function DashboardSidebarContents({
  footerItems,
  identity,
  idPrefix = "",
  navItems,
  pathname,
}: DashboardSidebarContentsProps) {
  const { t } = useLocalizedText();

  return (
    <>
      <Sidebar.Header>
        <div className="flex items-center gap-3 px-1 py-1">
          <Avatar className="size-9">
            <Avatar.Fallback>{identity.initials}</Avatar.Fallback>
          </Avatar>
          <div className="flex min-w-0 flex-col" data-sidebar="label">
            <span className="text-foreground truncate text-sm font-medium leading-tight">
              {identity.label}
            </span>
            <span className="text-muted truncate text-xs font-medium leading-tight">
              {identity.subtitle}
            </span>
          </div>
        </div>
      </Sidebar.Header>
      <Sidebar.Content>
        <Sidebar.Group>
          <Sidebar.Menu aria-label={t("appShell.navigation.dashboardMenu")}>
            {navItems.map((item) => (
              <DashboardSidebarNavItem
                key={item.href}
                idPrefix={idPrefix}
                item={item}
                pathname={pathname}
              />
            ))}
          </Sidebar.Menu>
        </Sidebar.Group>
      </Sidebar.Content>
      {footerItems.length > 0 ? (
        <Sidebar.Footer>
          <Sidebar.Menu aria-label={t("appShell.navigation.accountMenu")}>
            {footerItems.map((item) => (
              <DashboardSidebarNavItem
                key={item.href}
                idPrefix={idPrefix}
                item={item}
                pathname={pathname}
              />
            ))}
          </Sidebar.Menu>
        </Sidebar.Footer>
      ) : null}
    </>
  );
}

interface DashboardSidebarNavItemProps {
  idPrefix: string;
  item: DashboardShellItem;
  pathname: string;
}

function DashboardSidebarNavItem({ idPrefix, item, pathname }: DashboardSidebarNavItemProps) {
  const { t } = useLocalizedText();
  const label = t(item.labelKey);
  const badge = item.badgeKey ? t(item.badgeKey) : null;

  return (
    <Sidebar.MenuItem
      href={item.href}
      id={`${idPrefix}${item.href}`}
      isCurrent={isRouteCurrent(pathname, item)}
      textValue={label}
    >
      <Sidebar.MenuIcon>
        <i aria-hidden="true" className={`${item.iconClassName} size-4`} />
      </Sidebar.MenuIcon>
      <Sidebar.MenuLabel>{label}</Sidebar.MenuLabel>
      {badge ? (
        <Sidebar.MenuChip>
          <Chip color="success" size="sm" variant="soft">
            {badge}
          </Chip>
        </Sidebar.MenuChip>
      ) : null}
    </Sidebar.MenuItem>
  );
}
