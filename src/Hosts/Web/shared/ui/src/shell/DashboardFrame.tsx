"use client";

import type { ReactNode } from "react";
import {
  useCallback,
  useEffect,
  useLayoutEffect,
  useMemo,
  useState,
} from "react";
import { Avatar, Button, Chip, Dropdown, Label, Tooltip } from "@heroui/react";
import { AppLayout } from "@heroui-pro/react/app-layout";
import { Navbar } from "@heroui-pro/react/navbar";
import { Sidebar } from "@heroui-pro/react/sidebar";
import { useSidebar } from "@heroui-pro/react/sidebar";
import { useTheme } from "next-themes";
import {
  setLocaleCookie,
  supportedLocales,
  useLocalizedText,
  type SupportedLocale,
} from "@vitarish/localization";
import { contextColorClassName, type BusinessContext } from "../color/businessContext";
import { FaIcon } from "../icons/FaIcon";
import { RevealHeader } from "../layout/RevealHeader";

const useBeforePaintEffect =
  typeof window === "undefined" ? useEffect : useLayoutEffect;

export interface DashboardNavigationItem {
  href: string;
  icon: string;
  label: string;
  /** Business context the destination belongs to; colors the item's icon. Children inherit it. */
  context?: BusinessContext;
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
  /** Brand mark shown above the sidebar identity; hidden in the collapsed rail. */
  sidebarBrand?: ReactNode;
  sidebarSupplement?: ReactNode;
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
  sidebarBrand,
  sidebarSupplement,
  title,
  toolbar,
}: DashboardFrameProps) {
  const { direction } = useLocalizedText();
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [sidebarStateRestored, setSidebarStateRestored] = useState(false);

  useBeforePaintEffect(() => {
    try {
      const savedState = window.localStorage.getItem("sidebar_state");
      if (savedState === "true" || savedState === "false") {
        setSidebarOpen(savedState === "true");
      }
    } catch {
      // Storage can be unavailable or blocked; the in-memory state remains usable.
    } finally {
      setSidebarStateRestored(true);
    }
  }, []);

  const handleSidebarOpenChange = useCallback((open: boolean) => {
    setSidebarOpen(open);
    try {
      window.localStorage.setItem("sidebar_state", String(open));
    } catch {
      // Storage can be unavailable or blocked; keep the session-local toggle.
    }
  }, []);

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
        data-sidebar-state-restored={sidebarStateRestored ? "true" : "false"}
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
        onSidebarOpenChange={handleSidebarOpenChange}
        scrollMode={contentMode === "page" ? "content" : "page"}
        sidebarOpen={sidebarOpen}
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
            sidebarBrand={sidebarBrand}
            sidebarSupplement={sidebarSupplement}
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
  const { t } = useLocalizedText();

  return (
    <div
      className="vitarish-dashboard-workspace-controls"
      data-vitarish-layout="workspace-controls"
    >
      <div className="pointer-events-auto flex items-center gap-2">
        {mobileNavigation === "sidebar" ? (
          <AppLayout.MenuToggle
            aria-label={t("appShell.menu.open")}
            tooltip={t("appShell.menu.open")}
            variant="secondary"
          >
            <FaIcon icon="bars" aria-hidden="true" className="size-4" />
          </AppLayout.MenuToggle>
        ) : null}
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
  const { t } = useLocalizedText();

  return (
    <Navbar maxWidth="full" position="floating">
      <Navbar.Header>
        {mobileNavigation === "sidebar" ? (
          <AppLayout.MenuToggle
            aria-label={t("appShell.menu.open")}
            tooltip={t("appShell.menu.open")}
          >
            <FaIcon icon="bars" aria-hidden="true" className="size-4" />
          </AppLayout.MenuToggle>
        ) : null}
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
  /** Brand mark shown above the sidebar identity; hidden in the collapsed rail. */
  sidebarBrand?: ReactNode;
  sidebarSupplement?: ReactNode;
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
  sidebarBrand,
  sidebarSupplement,
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
  const [isSidebarTooltipOpen, setSidebarTooltipOpen] = useState(false);
  const { setTheme, theme } = useTheme();
  const { direction, locale, t } = useLocalizedText();
  const { isMobile, isOpen, toggleSidebar } = useSidebar();
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
        {sidebarBrand ? (
          <div data-sidebar="label" data-vitarish-layout="dashboard-sidebar-brand">
            {sidebarBrand}
          </div>
        ) : null}
        <div data-vitarish-layout="dashboard-sidebar-header-row">
          <Button
            fullWidth
            variant="ghost"
            onPress={() => onNavigate(identity.accountHref)}
            aria-label={identity.accountLabel}
            className="h-auto min-w-0 justify-start px-2 py-2"
            data-vitarish-layout="dashboard-sidebar-identity"
          >
            <Avatar className="size-8 shrink-0">
              <Avatar.Fallback className="bg-accent text-accent-foreground">{identity.initials}</Avatar.Fallback>
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
          <Tooltip delay={0} isOpen={isSidebarTooltipOpen}>
            <Button
              isIconOnly
              aria-label={
                isMobile || isOpen
                  ? t("appShell.menu.close")
                  : t("appShell.menu.open")
              }
              className="shrink-0"
              data-vitarish-layout="dashboard-sidebar-toggle"
              size="sm"
              variant="ghost"
              onBlur={() => setSidebarTooltipOpen(false)}
              onFocus={() => setSidebarTooltipOpen(true)}
              onHoverEnd={() => setSidebarTooltipOpen(false)}
              onHoverStart={() => setSidebarTooltipOpen(true)}
              onPress={() => {
                setSidebarTooltipOpen(false);
                toggleSidebar();
              }}
            >
              <FaIcon
                icon={isMobile ? "xmark" : "sidebar-flip"}
                aria-hidden="true"
                className="size-4"
                flip={
                  !isMobile && direction === "rtl" ? "horizontal" : undefined
                }
              />
            </Button>
            <Tooltip.Content
              placement={
                isMobile ? "bottom" : direction === "rtl" ? "left" : "right"
              }
            >
              {isMobile || isOpen
                ? t("appShell.menu.close")
                : t("appShell.menu.open")}
            </Tooltip.Content>
          </Tooltip>
        </div>
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
        {sidebarSupplement ? (
          <Sidebar.Group data-vitarish-layout="dashboard-sidebar-supplement">
            {sidebarSupplement}
          </Sidebar.Group>
        ) : null}
      </Sidebar.Content>
      <Sidebar.Footer>
        <Dropdown>
          <Dropdown.Trigger aria-label={t("appShell.settings.label")}>
            <FaIcon icon="gear" aria-hidden="true" className="size-4" />
          </Dropdown.Trigger>
          <Dropdown.Popover>
            <Dropdown.Menu>
              <Dropdown.SubmenuTrigger>
                <Dropdown.Item id="culture-options" textValue={t("appShell.culture.label")}>
                  <FaIcon icon="earth-africa" aria-hidden="true" className="size-4" />
                  <Label>{t("appShell.culture.label")}</Label>
                </Dropdown.Item>
                <Dropdown.Popover>
                  <Dropdown.Menu>
                    {supportedLocales.map((loc) => (
                      <Dropdown.Item key={loc} id={`culture-${loc}`} textValue={localeLabels[loc]} onAction={() => handleLocaleChange(loc)}>
                        <Label>{localeLabels[loc]}</Label>
                        {loc === locale ? <FaIcon icon="check" aria-hidden="true" className="size-4" /> : null}
                      </Dropdown.Item>
                    ))}
                  </Dropdown.Menu>
                </Dropdown.Popover>
              </Dropdown.SubmenuTrigger>
              {themeMounted ? (
                <Dropdown.SubmenuTrigger>
                  <Dropdown.Item id="theme-options" textValue={t(`appShell.theme.${currentTheme}`)}>
                          <FaIcon icon="palette" aria-hidden="true" className="size-4" />
                    <Label>{t(`appShell.theme.${currentTheme}`)}</Label>
                  </Dropdown.Item>
                  <Dropdown.Popover>
                    <Dropdown.Menu>
                      {themeModes.map((mode) => (
                        <Dropdown.Item key={mode} id={`theme-${mode}`} textValue={t(`appShell.theme.${mode}`)} onAction={() => setTheme(mode)}>
                          <FaIcon icon={themeIcons[mode]} aria-hidden="true" className="size-4" />
                          <Label>{t(`appShell.theme.${mode}`)}</Label>
                          {mode === currentTheme ?
                          <FaIcon icon="check" aria-hidden="true" className="size-4" /> : null}
                        </Dropdown.Item>
                      ))}
                    </Dropdown.Menu>
                  </Dropdown.Popover>
                </Dropdown.SubmenuTrigger>
              ) : null}
            </Dropdown.Menu>
          </Dropdown.Popover>
        </Dropdown>
        {footerItems.length > 0 ? (
          <Sidebar.Menu aria-label={footerLabel ?? navigationLabel}>
            {footerItems.map((item) => (
              <DashboardFrameSidebarNavItem
                key={item.href}
                currentPath={currentPath}
                idPrefix={idPrefix}
                item={item}
              />
            ))}
          </Sidebar.Menu>
        ) : null}
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
      className={item.context ? contextColorClassName(item.context) : undefined}
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
