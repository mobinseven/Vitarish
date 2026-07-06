"use client";

import { Avatar, Chip } from "@heroui/react";
import { Sidebar } from "@heroui-pro/react";
import { useEffect, useMemo, useState } from "react";
import { useLocalizedText } from "@/localization";
import type { DashboardShellItem, DashboardShellNavGroup } from "./DashboardShell";
import { isRouteBranchCurrent, isRouteCurrent } from "./dashboardRouteMatching";

export interface DashboardSidebarIdentity {
    initials: string;
    label: string;
    subtitle: string;
}

export interface DashboardSidebarProps {
    footerItems: readonly DashboardShellItem[];
    identity: DashboardSidebarIdentity;
    navGroups: readonly DashboardShellNavGroup[];
    pathname: string;
}

export function DashboardSidebar({ footerItems, identity, navGroups, pathname }: DashboardSidebarProps) {
    return (
        <>
            <Sidebar>
                <DashboardSidebarContents
                    footerItems={footerItems}
                    identity={identity}
                    navGroups={navGroups}
                    pathname={pathname}
                />
            </Sidebar>
            <Sidebar.Mobile>
                <DashboardSidebarContents
                    footerItems={footerItems}
                    identity={identity}
                    idPrefix="mobile-"
                    navGroups={navGroups}
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
    navGroups,
    pathname,
}: DashboardSidebarContentsProps) {
    const { t } = useLocalizedText();
    const requiredExpandedKeys = useMemo(
        () => getCurrentExpandedItemIds(navGroups, pathname, idPrefix),
        [idPrefix, navGroups, pathname]
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
                    <Sidebar.Menu
                        aria-label={t("appShell.navigation.dashboardMenu")}
                        expandedKeys={displayedExpandedKeys}
                        onExpandedChange={(keys) => setExpandedKeys(new Set(Array.from(keys, String)))}
                    >
                        {navGroups.map((group) => (
                            <Sidebar.MenuSection key={group.labelKey}>
                                <Sidebar.MenuHeader>
                                    {t(group.labelKey)}
                                </Sidebar.MenuHeader>
                                {group.items.map((item) => (
                                    <DashboardSidebarNavItem
                                        key={item.href}
                                        idPrefix={idPrefix}
                                        item={item}
                                        pathname={pathname}
                                    />
                                ))}
                            </Sidebar.MenuSection>
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
    const { direction, t } = useLocalizedText();
    const label = t(item.labelKey);
    const badge = item.badgeKey ? t(item.badgeKey) : null;

    return (
        <Sidebar.MenuItem
            href={item.href}
            id={getSidebarItemId(idPrefix, item)}
            isCurrent={isSidebarItemCurrent(pathname, item)}
            textValue={label}
        >
            <Sidebar.MenuIcon>
                <i aria-hidden="true" className={`${item.iconClassName} size-4`} />
            </Sidebar.MenuIcon>
            <Sidebar.MenuLabel>{label}</Sidebar.MenuLabel>
            {item.children?.length ? (
                <Sidebar.MenuTrigger>
                    <Sidebar.MenuIndicator aria-hidden="true" />
                </Sidebar.MenuTrigger>
            ) : null}
            {badge ? (
                <Sidebar.MenuChip>
                    <Chip color="success" size="sm" variant="soft">
                        {badge}
                    </Chip>
                </Sidebar.MenuChip>
            ) : null}
            {item.children?.length ? (
                <Sidebar.Submenu>
                    {item.children.map((child) => (
                        <DashboardSidebarNavItem
                            key={child.href}
                            idPrefix={idPrefix}
                            item={child}
                            pathname={pathname}
                        />
                    ))}
                </Sidebar.Submenu>
            ) : null}
        </Sidebar.MenuItem>
    );
}

function isSidebarItemCurrent(pathname: string, item: DashboardShellItem) {
    if (item.children?.some((child) => isRouteBranchCurrent(pathname, child))) {
        return false;
    }

    return isRouteCurrent(pathname, item);
}

function getSidebarItemId(idPrefix: string, item: DashboardShellItem) {
    return `${idPrefix}${item.href}`;
}

function getCurrentExpandedItemIds(
    navGroups: readonly DashboardShellNavGroup[],
    pathname: string,
    idPrefix: string
) {
    return navGroups.flatMap((group) =>
        group.items.flatMap((item) => getCurrentExpandedItemIdsForItem(item, pathname, idPrefix))
    );
}

function getCurrentExpandedItemIdsForItem(
    item: DashboardShellItem,
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
