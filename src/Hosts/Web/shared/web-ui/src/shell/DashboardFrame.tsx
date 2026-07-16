"use client";

import type { ReactNode } from "react";
import { useEffect, useMemo, useState } from "react";
import { Avatar, Button, Chip } from "@heroui/react";
import { AppLayout } from "@heroui-pro/react/app-layout";
import { Navbar } from "@heroui-pro/react/navbar";
import { Sidebar } from "@heroui-pro/react/sidebar";

export interface DashboardNavigationItem {
  href: string;
  iconClassName: string;
  label: string;
  badge?: string;
  children?: readonly DashboardNavigationItem[];
  exactMatch?: boolean;
}

export interface DashboardNavigationGroup {
  label: string;
  items: readonly DashboardNavigationItem[];
}

export interface DashboardIdentity {
  accountHref: string;
  accountLabel: string;
  initials: string;
  label: string;
  subtitle: string;
}

export interface DashboardFrameProps {
  children: ReactNode;
  currentPath: string;
  footerItems?: readonly DashboardNavigationItem[];
  identity: DashboardIdentity;
  navigationGroups: readonly DashboardNavigationGroup[];
  navigationLabel: string;
  navigate: (href: string) => void;
  title: ReactNode;
  footerLabel?: string;
  toolbar?: ReactNode;
}

export function DashboardFrame({
  children,
  currentPath,
  footerItems = [],
  footerLabel,
  identity,
  navigationGroups,
  navigationLabel,
  navigate,
  title,
  toolbar,
}: DashboardFrameProps) {
  return (
    <AppLayout
      navigate={navigate}
      navbar={<DashboardFrameNavbar title={title} toolbar={toolbar} />}
      scrollMode="content"
      sidebar={
        <DashboardFrameSidebar
          currentPath={currentPath}
          footerItems={footerItems}
          footerLabel={footerLabel}
          identity={identity}
          navigationGroups={navigationGroups}
          navigationLabel={navigationLabel}
          onNavigate={navigate}
        />
      }
      sidebarCollapsible="icon"
    >
      {children}
    </AppLayout>
  );
}

function DashboardFrameNavbar({ title, toolbar }: { title: ReactNode; toolbar?: ReactNode }) {
  return (
    <Navbar maxWidth="full">
      <Navbar.Header>
        <AppLayout.MenuToggle />
        <Sidebar.Trigger />
        {title}
        <Navbar.Spacer />
        {toolbar ? <div className="flex items-center gap-2">{toolbar}</div> : null}
      </Navbar.Header>
    </Navbar>
  );
}

interface DashboardFrameSidebarProps {
  currentPath: string;
  footerItems: readonly DashboardNavigationItem[];
  footerLabel?: string;
  identity: DashboardIdentity;
  navigationGroups: readonly DashboardNavigationGroup[];
  navigationLabel: string;
  onNavigate: (href: string) => void;
}

function DashboardFrameSidebar(props: DashboardFrameSidebarProps) {
  return (
    <>
      <Sidebar>
        <DashboardFrameSidebarContents {...props} />
      </Sidebar>
      <Sidebar.Mobile>
        <DashboardFrameSidebarContents {...props} idPrefix="mobile-" />
      </Sidebar.Mobile>
    </>
  );
}

interface DashboardFrameSidebarContentsProps extends DashboardFrameSidebarProps {
  idPrefix?: string;
}

function DashboardFrameSidebarContents({
  currentPath,
  footerItems,
  footerLabel,
  idPrefix = "",
  identity,
  navigationGroups,
  navigationLabel,
  onNavigate,
}: DashboardFrameSidebarContentsProps) {
  const requiredExpandedKeys = useMemo(
    () => getCurrentExpandedItemIds(navigationGroups, currentPath, idPrefix),
    [currentPath, idPrefix, navigationGroups]
  );
  const [expandedKeys, setExpandedKeys] = useState<Set<string>>(() => new Set(requiredExpandedKeys));
  const displayedExpandedKeys = useMemo(
    () => mergeRequiredExpandedKeys(expandedKeys, requiredExpandedKeys),
    [expandedKeys, requiredExpandedKeys]
  );

  useEffect(() => {
    setExpandedKeys((current) => mergeRequiredExpandedKeys(current, requiredExpandedKeys));
  }, [requiredExpandedKeys]);

  return (
    <>
      <Sidebar.Header>
        <Button
          fullWidth
          variant="ghost"
          onPress={() => onNavigate(identity.accountHref)}
          aria-label={identity.accountLabel}
          className="justify-start px-3 py-2 h-auto"
        >
          <Avatar className="size-9">
            <Avatar.Fallback>{identity.initials}</Avatar.Fallback>
          </Avatar>
          <div className="flex min-w-0 flex-col" data-sidebar="label">
            <span className="text-foreground truncate text-sm font-medium leading-tight">{identity.label}</span>
            <span className="text-muted truncate text-xs font-medium leading-tight">{identity.subtitle}</span>
          </div>
        </Button>
      </Sidebar.Header>
      <Sidebar.Content>
        <Sidebar.Group>
          <Sidebar.Menu
            aria-label={navigationLabel}
            expandedKeys={displayedExpandedKeys}
            onExpandedChange={(keys) => setExpandedKeys(new Set(Array.from(keys, String)))}
          >
            {navigationGroups.map((group) => (
              <Sidebar.MenuSection key={group.label}>
                <Sidebar.MenuHeader>{group.label}</Sidebar.MenuHeader>
                {group.items.map((item) => (
                  <DashboardFrameSidebarNavItem
                    key={item.href}
                    currentPath={currentPath}
                    idPrefix={idPrefix}
                    item={item}
                  />
                ))}
              </Sidebar.MenuSection>
            ))}
          </Sidebar.Menu>
        </Sidebar.Group>
      </Sidebar.Content>
      {footerItems.length > 0 ? (
        <Sidebar.Footer>
          <Sidebar.Menu aria-label={footerLabel ?? navigationLabel}>
            {footerItems.map((item) => (
              <DashboardFrameSidebarNavItem key={item.href} currentPath={currentPath} idPrefix={idPrefix} item={item} />
            ))}
          </Sidebar.Menu>
        </Sidebar.Footer>
      ) : null}
    </>
  );
}

interface DashboardFrameSidebarNavItemProps {
  currentPath: string;
  idPrefix: string;
  item: DashboardNavigationItem;
}

function DashboardFrameSidebarNavItem({ currentPath, idPrefix, item }: DashboardFrameSidebarNavItemProps) {
  return (
    <Sidebar.MenuItem
      href={item.children?.length ? undefined : item.href}
      id={getSidebarItemId(idPrefix, item)}
      isCurrent={isSidebarItemCurrent(currentPath, item)}
      textValue={item.label}
    >
      <Sidebar.MenuIcon>
        <i aria-hidden="true" className={`${item.iconClassName} size-4`} />
      </Sidebar.MenuIcon>
      <Sidebar.MenuLabel>{item.label}</Sidebar.MenuLabel>
      {item.children?.length ? (
        <Sidebar.MenuTrigger aria-label={item.label}>
          <Sidebar.MenuIndicator aria-hidden="true" />
        </Sidebar.MenuTrigger>
      ) : null}
      {item.badge ? (
        <Sidebar.MenuChip>
          <Chip color="success" size="sm" variant="soft">
            {item.badge}
          </Chip>
        </Sidebar.MenuChip>
      ) : null}
      {item.children?.length ? (
        <Sidebar.Submenu>
          {item.children.map((child) => (
            <DashboardFrameSidebarNavItem
              key={child.href}
              currentPath={currentPath}
              idPrefix={idPrefix}
              item={child}
            />
          ))}
        </Sidebar.Submenu>
      ) : null}
    </Sidebar.MenuItem>
  );
}

function isSidebarItemCurrent(pathname: string, item: DashboardNavigationItem) {
  if (item.children?.some((child) => isRouteBranchCurrent(pathname, child))) {
    return false;
  }

  return isRouteCurrent(pathname, item);
}

function isRouteCurrent(pathname: string, item: DashboardNavigationItem) {
  if (item.exactMatch) {
    return pathname === item.href;
  }

  return pathname === item.href || pathname.startsWith(`${item.href}/`);
}

function isRouteBranchCurrent(pathname: string, item: DashboardNavigationItem): boolean {
  if (isHrefBranchCurrent(pathname, item.href)) {
    return true;
  }

  return item.children?.some((child) => isRouteBranchCurrent(pathname, child)) ?? false;
}

function isHrefBranchCurrent(pathname: string, href: string) {
  return pathname === href || (href !== "/" && pathname.startsWith(`${href}/`));
}

function getSidebarItemId(idPrefix: string, item: DashboardNavigationItem) {
  return `${idPrefix}${item.href}`;
}

function getCurrentExpandedItemIds(
  navGroups: readonly DashboardNavigationGroup[],
  pathname: string,
  idPrefix: string
) {
  return navGroups.flatMap((group) =>
    group.items.flatMap((item) => getCurrentExpandedItemIdsForItem(item, pathname, idPrefix))
  );
}

function getCurrentExpandedItemIdsForItem(
  item: DashboardNavigationItem,
  pathname: string,
  idPrefix: string
): string[] {
  if (!item.children?.length) {
    return [];
  }

  const childExpandedIds = item.children.flatMap((child) =>
    getCurrentExpandedItemIdsForItem(child, pathname, idPrefix)
  );

  if (!isRouteBranchCurrent(pathname, item)) {
    return childExpandedIds;
  }

  return [getSidebarItemId(idPrefix, item), ...childExpandedIds];
}

function mergeRequiredExpandedKeys(expandedKeys: Set<string>, requiredExpandedKeys: readonly string[]) {
  let hasChanges = false;
  const nextExpandedKeys = new Set(expandedKeys);

  for (const key of requiredExpandedKeys) {
    if (!nextExpandedKeys.has(key)) {
      nextExpandedKeys.add(key);
      hasChanges = true;
    }
  }

  return hasChanges ? nextExpandedKeys : expandedKeys;
}
