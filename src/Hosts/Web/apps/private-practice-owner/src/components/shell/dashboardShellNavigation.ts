import { Routes } from "@/app/routes";
import type { DashboardShellItem, DashboardShellNavGroup } from "./DashboardShell";

const homeItem = {
  href: Routes.Home,
  icon: "house",
  context: "general",
  labelKey: "appShell.navigation.home",
  exactMatch: true,
} as const satisfies DashboardShellItem;

export const dashboardFooterItems = [
  { href: Routes.Logout, icon: "arrow-right-from-bracket", labelKey: "appShell.userMenu.signOut", exactMatch: true },
] as const satisfies readonly DashboardShellItem[];

const clinicOwnerItems = [
  { href: Routes.Clinic.Patients.Index, icon: "users", context: "patient-record", labelKey: "clinic.patients.title", exactMatch: true },
  { href: Routes.Clinic.CheckIn, icon: "clipboard-check", context: "check-in", labelKey: "clinic.dashboard.sections.checkIn.title", exactMatch: true },
  { href: Routes.Clinic.Appointments.Reservations.Index, icon: "calendar-check", context: "scheduling", labelKey: "clinic.appointments.reservations.title", exactMatch: true },
  { href: Routes.Clinic.Reminders, icon: "bell", context: "communication", labelKey: "clinic.dashboard.sections.reminders.title", exactMatch: true },
  { href: Routes.Clinic.Settings.Index, icon: "gear", context: "general", labelKey: "clinic.dashboard.sections.settings.title", exactMatch: true },
] as const satisfies readonly DashboardShellItem[];

const clinicUserItems = [
  { href: Routes.ClinicUser.Shifts.Index, icon: "clock", context: "scheduling", labelKey: "clinic.userAppointments.shifts.title", exactMatch: true },
  { href: Routes.ClinicUser.Reservations.Index, icon: "calendar-check", context: "scheduling", labelKey: "clinic.userAppointments.reservations.title", exactMatch: true },
] as const satisfies readonly DashboardShellItem[];

const homeGroup: DashboardShellNavGroup = {
  labelKey: "appShell.navigation.mainGroup",
  items: [homeItem, ...clinicOwnerItems] as const satisfies readonly DashboardShellItem[],
};

export const homeDashboardNavGroups = [homeGroup] as const satisfies readonly DashboardShellNavGroup[];

const appFeatureGroup: DashboardShellNavGroup = {
  labelKey: "appShell.navigation.mainGroup",
  items: [homeItem, ...clinicOwnerItems] as const satisfies readonly DashboardShellItem[],
};

export const mainDashboardNavGroups = [appFeatureGroup] as const satisfies readonly DashboardShellNavGroup[];

const clinicOwnerGroup: DashboardShellNavGroup = {
  labelKey: "appShell.navigation.mainGroup",
  items: [homeItem, ...clinicOwnerItems] as const satisfies readonly DashboardShellItem[],
};

export const clinicOwnerDashboardNavGroups = [clinicOwnerGroup] as const satisfies readonly DashboardShellNavGroup[];

const clinicUserGroup: DashboardShellNavGroup = {
  labelKey: "appShell.navigation.mainGroup",
  items: [homeItem, ...clinicUserItems] as const satisfies readonly DashboardShellItem[],
};

export const clinicUserDashboardNavGroups = [clinicUserGroup] as const satisfies readonly DashboardShellNavGroup[];
