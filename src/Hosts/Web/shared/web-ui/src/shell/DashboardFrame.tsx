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

export type DashboardContentMode = "page" | "workspace";
export type DashboardMobileNavigation = "none" | "sidebar";

export interface DashboardFrameProps {
  aside?: ReactNode;
  asideMobile?: "hidden" | "sheet";
  asideOpen?: boolean;
  asideToggleLabel?: string;
  children: ReactNode;
  contentMode: DashboardContentMode;
  currentPath: string;
  defaultAsideOpen?: boolean;
  footerItems?: readonly DashboardNavigationItem[];
  identity: DashboardIdentity;
  mobileAside?: ReactNode;
  mobileNavigation: DashboardMobileNavigation;
  navigationGroups: readonly DashboardNavigationGroup[];
  navigationLabel: string;
  navigate: (href: string) => void;
  onAsideOpenChange?: (open: boolean) => void;
  skipLinkLabel: string;
  title: ReactNode;
  footerLabel?: string;
  toolbar?: ReactNode;
}

export function DashboardFrame({
  aside,
  asideMobile = "hidden",
  asideOpen,
  asideToggleLabel,
  children,
  contentMode,
  currentPath,
  defaultAsideOpen,
  footerItems = [],
  footerLabel,
  identity,
  mobileAside,
  mobileNavigation,
  navigationGroups,
  navigationLabel,
  navigate,
  onAsideOpenChange,
  skipLinkLabel,
  title,
  toolbar,
}: DashboardFrameProps) {
  return (
    <>
      <a className="vitarish-skip-link" href="#vitarish-dashboard-main">
        {skipLinkLabel}
      </a>
      <AppLayout
        aside={aside}
        asideMobile={asideMobile}
        asideOpen={asideOpen}
        data-content-mode={contentMode}
        data-mobile-navigation={mobileNavigation}
        data-vitarish-layout="dashboard-frame"
        data-vitarish-scroll-owner={contentMode === "page" ? "frame-main" : "workspace"}
        defaultAsideOpen={defaultAsideOpen}
        navigate={navigate}
        navbar={
          <DashboardFrameNavbar
            asideToggleLabel={asideToggleLabel}
            hasAside={aside !== undefined && aside !== null}
            mobileNavigation={mobileNavigation}
            title={title}
            toolbar={toolbar}
          />
        }
        onAsideOpenChange={onAsideOpenChange}
        scrollMode={contentMode === "page" ? "content" : "page"}
        sidebar={
          <DashboardFrameSidebar
            currentPath={currentPath}
            footerItems={footerItems}
            footerLabel={footerLabel}
            identity={identity}
            mobileNavigation={mobileNavigation}
            navigationGroups={navigationGroups}
            navigationLabel={navigationLabel}
            onNavigate={navigate}
          />
        }
        sidebarCollapsible="icon"
      >
        {mobileAside ? <AppLayout.MobileAside>{mobileAside}</AppLayout.MobileAside> : null}
        <div
          className="vitarish-dashboard-content"
          data-vitarish-layout="dashboard-content"
          id="vitarish-dashboard-main"
          tabIndex={-1}
        >
          {children}
        </div>
      </AppLayout>
    </>
  );
}

interface DashboardFrameNavbarProps {
  asideToggleLabel?: string;
  hasAside: boolean;
  mobileNavigation: DashboardMobileNavigation;
  title: ReactNode;
  toolbar?: ReactNode;
}

function DashboardFrameNavbar({
  asideToggleLabel,
  hasAside,
  mobileNavigation,
  title,
  toolbar,
}: DashboardFrameNavbarProps) {
  return (
    <Navbar maxWidth="full">
      <Navbar.Header>
        {mobileNavigation === "sidebar" ? <AppLayout.MenuToggle /> : null}
        <Sidebar.Trigger />
        {title}
        <Navbar.Spacer />
        {toolbar ? <div className="flex items-center gap-2">{toolbar}</div> : null}
        {hasAside ? (
          asideToggleLabel ? (
            <AppLayout.AsideTrigger aria-label={asideToggleLabel} />
          ) : (
            <AppLayout.AsideTrigger />
          )
        ) : null}
      </Navbar.Header>
    </Navbar>
  );
}

interface DashboardFrameSidebarProps {
  currentPath: string;
  footerItems: readonly DashboardNavigationItem[];
  footerLabel?: string;
  identity: DashboardIdentity;
  mobileNavigation: DashboardMobileNavigation;
  navigationGroups: readonly DashboardNavigationGroup[];
  navigationLabel: string;
  onNavigate: (href: string) => void;
}

function DashboardFrameSidebar({ mobileNavigation, ...props }: DashboardFrameSidebarProps) {
  return (
    <>
      <Sidebar>
        <DashboardFrameSidebarContents {...props} />
      </Sidebar>
      {mobileNavigation === "sidebar" ? (
        <Sidebar.Mobile>
          <DashboardFrameSidebarContents {...props} idPrefix="mobile-" />
        </Sidebar.Mobile>
      ) : null}
    </>
  );
}

interface DashboardFrameSidebarContentsProps
  extends Omit<DashboardFrameSidebarProps, "mobileNavigation"> {
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
