import { Routes } from "@/app/routes";
import {
  clinicOwnerDestinations,
  clinicUserDestinations,
  type NavigationDestination,
} from "@/app/navigationRegistry";
import type { DashboardShellItem, DashboardShellNavGroup } from "./DashboardShell";

export const dashboardFooterItems = [
  { href: Routes.Logout, icon: "arrow-right-from-bracket", labelKey: "appShell.userMenu.signOut", exactMatch: true },
] as const satisfies readonly DashboardShellItem[];

function toShellItem(destination: NavigationDestination): DashboardShellItem {
  return {
    href: destination.href,
    icon: destination.icon,
    labelKey: destination.labelKey,
    owns: destination.owns,
    exactMatch: true,
  };
}

function mainGroup(destinations: readonly NavigationDestination[]): readonly DashboardShellNavGroup[] {
  return [{ labelKey: "appShell.navigation.mainGroup", items: destinations.map(toShellItem) }];
}

/** The clinic owner's sidebar, shared by the home, account and clinic pages. */
export const clinicOwnerDashboardNavGroups = mainGroup(clinicOwnerDestinations);
export const homeDashboardNavGroups = clinicOwnerDashboardNavGroups;
export const mainDashboardNavGroups = clinicOwnerDashboardNavGroups;

export const clinicUserDashboardNavGroups = mainGroup(clinicUserDestinations);

/** The central account's sidebar: the account itself and the way back to the platform home. */
export const centralAccountNavGroups = [{
  labelKey: "appShell.navigation.mainGroup",
  items: [
    { href: Routes.Account.Index, icon: "user-shield", labelKey: "appShell.userMenu.account", exactMatch: true },
    { href: Routes.SignInContinue, icon: "house", labelKey: "appShell.navigation.home", exactMatch: true },
  ],
}] as const satisfies readonly DashboardShellNavGroup[];
