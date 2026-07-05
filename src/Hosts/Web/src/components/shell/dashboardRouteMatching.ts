import type { DashboardShellItem } from "./DashboardShell";

export function isRouteCurrent(pathname: string, item: DashboardShellItem) {
  if (item.exactMatch) {
    return pathname === item.href;
  }

  return pathname === item.href || pathname.startsWith(`${item.href}/`);
}
