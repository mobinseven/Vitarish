import type { DashboardShellItem } from "./DashboardShell";

export function isRouteCurrent(pathname: string, item: DashboardShellItem) {
  if (item.exactMatch) {
    return pathname === item.href;
  }

  return pathname === item.href || pathname.startsWith(`${item.href}/`);
}

export function isRouteBranchCurrent(pathname: string, item: DashboardShellItem): boolean {
  if (isHrefBranchCurrent(pathname, item.href)) {
    return true;
  }

  return item.children?.some((child) => isRouteBranchCurrent(pathname, child)) ?? false;
}

export function findCurrentRouteItem(
  pathname: string,
  items: readonly DashboardShellItem[]
): DashboardShellItem | undefined {
  for (const item of items) {
    const childMatch = item.children ? findCurrentRouteItem(pathname, item.children) : undefined;

    if (childMatch) {
      return childMatch;
    }

    if (isRouteCurrent(pathname, item)) {
      return item;
    }

    if (item.children && isHrefBranchCurrent(pathname, item.href)) {
      return item;
    }
  }

  return undefined;
}

function isHrefBranchCurrent(pathname: string, href: string) {
  return pathname === href || (href !== "/" && pathname.startsWith(`${href}/`));
}
