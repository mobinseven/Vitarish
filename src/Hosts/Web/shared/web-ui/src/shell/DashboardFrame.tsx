"use client";

import type { ReactNode } from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Avatar, Button, Chip } from "@heroui/react";
import { AppLayout } from "@heroui-pro/react/app-layout";
import { Navbar } from "@heroui-pro/react/navbar";
import { Sidebar } from "@heroui-pro/react/sidebar";
import { useTheme } from "next-themes";
import {
  setLocaleCookie,
  supportedLocales,
  useLocalizedText,
  type SupportedLocale,
} from "@vitarish/web-localization";
import { FaIcon } from "../icons/FaIcon";
import { RevealHeader } from "../layout/RevealHeader";

export interface DashboardNavigationItem {
  href: string;
  icon: string;
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
  const { direction } = useLocalizedText();

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
        data-direction={direction}
        data-mobile-navigation={mobileNavigation}
        data-vitarish-layout="dashboard-frame"
        data-vitarish-scroll-owner={
          contentMode === "page" ? "frame-main" : "workspace"
        }
        defaultAsideOpen={defaultAsideOpen}
        navigate={navigate}
        navbar={
          contentMode === "page" ? (
            <DashboardFrameNavbar
              asideToggleLabel={asideToggleLabel}
              hasAside={aside !== undefined && aside !== null}
              mobileNavigation={mobileNavigation}
              title={title}
              toolbar={toolbar}
            />
          ) : undefined
        }
        onAsideOpenChange={onAsideOpenChange}
        scrollMode={contentMode === "page" ? "content" : "page"}
        sidebarVariant="inset"
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
        sidebarSide={direction === "rtl" ? "right" : "left"}
      >
        {mobileAside ? (
          <AppLayout.MobileAside>{mobileAside}</AppLayout.MobileAside>
        ) : null}
        <div
          className="vitarish-dashboard-content"
          data-vitarish-layout="dashboard-content"
          data-vitarish-reveal-header-region={
            contentMode === "workspace" ? "workspace" : undefined
          }
          data-vitarish-scroll-owner={
            contentMode === "workspace" ? "workspace" : undefined
          }
          id="vitarish-dashboard-main"
          tabIndex={-1}
        >
          {contentMode === "workspace" ? (
            <>
              <div data-vitarish-reveal-header-host="" />
              <DashboardFrameNavbar
                asideToggleLabel={asideToggleLabel}
                hasAside={aside !== undefined && aside !== null}
                mobileNavigation={mobileNavigation}
                title={title}
                toolbar={toolbar}
              />

              <RevealHeader.Boundary />
            </>
          ) : null}
          {children}
        </div>
      </AppLayout>
    </>
  );
}

interface DashboardFrameWorkspaceControlsProps {
  asideToggleLabel?: string;
  hasAside: boolean;
  mobileNavigation: DashboardMobileNavigation;
  toolbar?: ReactNode;
}

function DashboardFrameWorkspaceControls({
  asideToggleLabel,
  hasAside,
  mobileNavigation,
  toolbar,
}: DashboardFrameWorkspaceControlsProps) {
  return (
    <div
      className="vitarish-dashboard-workspace-controls"
      data-vitarish-layout="workspace-controls"
    >
      <div className="pointer-events-auto flex items-center gap-2">
        {mobileNavigation === "sidebar" ? (
          <AppLayout.MenuToggle variant="secondary" />
        ) : null}
        <Sidebar.Trigger variant="secondary" />
      </div>
      <div className="pointer-events-auto flex items-center gap-2">
        {toolbar}
        {hasAside ? (
          asideToggleLabel ? (
            <AppLayout.AsideTrigger aria-label={asideToggleLabel} />
          ) : (
            <AppLayout.AsideTrigger />
          )
        ) : null}
      </div>
    </div>
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
  const { direction } = useLocalizedText();

  return (
    <Navbar maxWidth="full" position="floating">
      <Navbar.Header>
        {direction === "rtl" ? <Sidebar.Trigger /> : null}
        {mobileNavigation === "sidebar" ? <AppLayout.MenuToggle /> : null}
        {direction === "ltr" ? <Sidebar.Trigger /> : null}
        {title}
        <Navbar.Spacer />
        {toolbar ? (
          <div className="flex items-center gap-2">{toolbar}</div>
        ) : null}
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

function DashboardFrameSidebar({
  mobileNavigation,
  ...props
}: DashboardFrameSidebarProps) {
  return (
    <>
      <Sidebar className="vitarish-dashboard-sidebar">
        <DashboardFrameSidebarContents {...props} />
      </Sidebar>
      {mobileNavigation === "sidebar" ? (
        <Sidebar.Mobile className="vitarish-dashboard-sidebar-mobile">
          <DashboardFrameSidebarContents {...props} idPrefix="mobile-" />
        </Sidebar.Mobile>
      ) : null}
    </>
  );
}

interface DashboardFrameSidebarContentsProps extends Omit<
  DashboardFrameSidebarProps,
  "mobileNavigation"
> {
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
    [currentPath, idPrefix, navigationGroups],
  );
  const [expandedKeys, setExpandedKeys] = useState<Set<string>>(
    () => new Set(requiredExpandedKeys),
  );
  const displayedExpandedKeys = useMemo(
    () => mergeRequiredExpandedKeys(expandedKeys, requiredExpandedKeys),
    [expandedKeys, requiredExpandedKeys],
  );

  const [themeMounted, setThemeMounted] = useState(false);
  const { setTheme, theme } = useTheme();
  const { locale, t } = useLocalizedText();
  const currentTheme: ThemeMode =
    theme === "system" ? "system" : theme === "dark" ? "dark" : "light";

  useEffect(() => {
    setExpandedKeys((current) =>
      mergeRequiredExpandedKeys(current, requiredExpandedKeys),
    );
  }, [requiredExpandedKeys]);

  useEffect(() => setThemeMounted(true), []);

  const handleLocaleChange = useCallback(
    (key: React.Key) => {
      if (key === locale) return;
      setLocaleCookie(key as SupportedLocale);
      window.location.reload();
    },
    [locale],
  );

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
            <Avatar.Fallback>
              <FaIcon icon="user" />
            </Avatar.Fallback>
          </Avatar>
          <div className="flex min-w-0 flex-col" data-sidebar="label">
            <span className="text-foreground truncate text-sm font-medium leading-tight">
              {identity.label}
            </span>
            <span className="text-muted truncate text-xs font-medium leading-tight">
              {identity.subtitle}
            </span>
          </div>
        </Button>
      </Sidebar.Header>
      <Sidebar.Content>
        <Sidebar.Group>
          <Sidebar.Menu
            aria-label={navigationLabel}
            expandedKeys={displayedExpandedKeys}
            onExpandedChange={(keys) =>
              setExpandedKeys(new Set(Array.from(keys, String)))
            }
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
      <Sidebar.Footer>
        <Sidebar.Menu aria-label={footerLabel ?? navigationLabel}>
          <Sidebar.MenuItem
            id="culture-switcher"
            textValue={t("appShell.culture.label")}
          >
            <Sidebar.MenuIcon>
              <FaIcon
                icon="globe"
                aria-hidden="true"
                className="size-4 shrink-0"
              />
            </Sidebar.MenuIcon>
            <Sidebar.MenuLabel>{t("appShell.culture.label")}</Sidebar.MenuLabel>
            <Sidebar.MenuTrigger aria-label={t("appShell.culture.label")}>
              <Sidebar.MenuIndicator aria-hidden="true" />
            </Sidebar.MenuTrigger>
            <Sidebar.Submenu>
              {supportedLocales.map((loc) => (
                <Sidebar.MenuItem
                  key={loc}
                  id={`culture-${loc}`}
                  isCurrent={loc === locale}
                  textValue={localeLabels[loc]}
                  onAction={() => handleLocaleChange(loc)}
                >
                  <Sidebar.MenuLabel>{localeLabels[loc]}</Sidebar.MenuLabel>
                </Sidebar.MenuItem>
              ))}
            </Sidebar.Submenu>
          </Sidebar.MenuItem>
          {themeMounted ? (
            <Sidebar.MenuItem
              id="theme-switcher"
              textValue={t(`appShell.theme.${currentTheme}`)}
            >
              <Sidebar.MenuIcon>
                <FaIcon
                  icon={themeIcons[currentTheme]}
                  aria-hidden="true"
                  className="size-4 shrink-0"
                />
              </Sidebar.MenuIcon>
              <Sidebar.MenuLabel>
                {t(`appShell.theme.${currentTheme}`)}
              </Sidebar.MenuLabel>
              <Sidebar.MenuTrigger
                aria-label={t(`appShell.theme.${currentTheme}`)}
              >
                <Sidebar.MenuIndicator aria-hidden="true" />
              </Sidebar.MenuTrigger>
              <Sidebar.Submenu>
                {themeModes.map((mode) => (
                  <Sidebar.MenuItem
                    key={mode}
                    id={`theme-${mode}`}
                    isCurrent={mode === currentTheme}
                    textValue={t(`appShell.theme.${mode}`)}
                    onAction={() => setTheme(mode)}
                  >
                    <Sidebar.MenuIcon>
                      <FaIcon
                        icon={themeIcons[mode]}
                        aria-hidden="true"
                        className="size-4 shrink-0"
                      />
                    </Sidebar.MenuIcon>
                    <Sidebar.MenuLabel>
                      {t(`appShell.theme.${mode}`)}
                    </Sidebar.MenuLabel>
                  </Sidebar.MenuItem>
                ))}
              </Sidebar.Submenu>
            </Sidebar.MenuItem>
          ) : (
            <div aria-hidden="true" className="h-9 w-9" />
          )}
          {footerItems.length > 0
            ? footerItems.map((item) => (
                <DashboardFrameSidebarNavItem
                  key={item.href}
                  currentPath={currentPath}
                  idPrefix={idPrefix}
                  item={item}
                />
              ))
            : null}
        </Sidebar.Menu>
      </Sidebar.Footer>
    </>
  );
}

interface DashboardFrameSidebarNavItemProps {
  currentPath: string;
  idPrefix: string;
  item: DashboardNavigationItem;
}

function DashboardFrameSidebarNavItem({
  currentPath,
  idPrefix,
  item,
}: DashboardFrameSidebarNavItemProps) {
  return (
    <Sidebar.MenuItem
      href={item.children?.length ? undefined : item.href}
      id={getSidebarItemId(idPrefix, item)}
      isCurrent={isSidebarItemCurrent(currentPath, item)}
      textValue={item.label}
    >
      <Sidebar.MenuIcon>
        <FaIcon icon={item.icon} aria-hidden="true" className="size-4" />
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

const themeModes = ["light", "dark", "system"] as const;
type ThemeMode = (typeof themeModes)[number];
const themeIcons: Record<ThemeMode, string> = {
  light: "sun",
  dark: "moon",
  system: "display",
};
const localeLabels: Record<SupportedLocale, string> = {
  fa: "فارسی",
  en: "English",
};

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

function isRouteBranchCurrent(
  pathname: string,
  item: DashboardNavigationItem,
): boolean {
  if (isHrefBranchCurrent(pathname, item.href)) {
    return true;
  }

  return (
    item.children?.some((child) => isRouteBranchCurrent(pathname, child)) ??
    false
  );
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
  idPrefix: string,
) {
  return navGroups.flatMap((group) =>
    group.items.flatMap((item) =>
      getCurrentExpandedItemIdsForItem(item, pathname, idPrefix),
    ),
  );
}

function getCurrentExpandedItemIdsForItem(
  item: DashboardNavigationItem,
  pathname: string,
  idPrefix: string,
): string[] {
  if (!item.children?.length) {
    return [];
  }

  const childExpandedIds = item.children.flatMap((child) =>
    getCurrentExpandedItemIdsForItem(child, pathname, idPrefix),
  );

  if (!isRouteBranchCurrent(pathname, item)) {
    return childExpandedIds;
  }

  return [getSidebarItemId(idPrefix, item), ...childExpandedIds];
}

function mergeRequiredExpandedKeys(
  expandedKeys: Set<string>,
  requiredExpandedKeys: readonly string[],
) {
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
