import { Routes } from "@/app/routes";
import type { DashboardShellItem } from "./DashboardShell";

export const mainDashboardNavItems = [
  { href: Routes.Home, iconClassName: "fa-solid fa-house", labelKey: "appShell.navigation.home", exactMatch: true },
  { href: Routes.Knowledge.Index, iconClassName: "fa-solid fa-book-open", labelKey: "appShell.navigation.knowledge" },
  { href: Routes.Workspaces.Select, iconClassName: "fa-solid fa-layer-group", labelKey: "journey.workspace.title", exactMatch: true },
] as const satisfies readonly DashboardShellItem[];

export const dashboardFooterItems = [
  { href: Routes.Account.Index, iconClassName: "fa-solid fa-circle-user", labelKey: "appShell.userMenu.account" },
  { href: Routes.Logout, iconClassName: "fa-solid fa-arrow-right-from-bracket", labelKey: "appShell.userMenu.signOut", exactMatch: true },
] as const satisfies readonly DashboardShellItem[];

export const clinicOwnerDashboardNavItems = [
  { href: Routes.Clinic.Index, iconClassName: "fa-solid fa-gauge", labelKey: "clinic.dashboard.title", exactMatch: true },
  { href: Routes.Clinic.Patients.Index, iconClassName: "fa-solid fa-users", labelKey: "clinic.patients.title" },
  { href: Routes.Clinic.CheckIn, iconClassName: "fa-solid fa-clipboard-check", labelKey: "clinic.dashboard.sections.checkIn.title" },
  { href: Routes.Clinic.Appointments.Reservations.Index, iconClassName: "fa-solid fa-calendar", labelKey: "clinic.dashboard.sections.appointments.title" },
  { href: Routes.Clinic.Reminders, iconClassName: "fa-solid fa-bell", labelKey: "clinic.dashboard.sections.reminders.title" },
  { href: Routes.Clinic.Settings.Index, iconClassName: "fa-solid fa-gear", labelKey: "clinic.dashboard.sections.settings.title" },
] as const satisfies readonly DashboardShellItem[];

export const clinicUserDashboardNavItems = [
  { href: Routes.ClinicUser.Shifts.Index, iconClassName: "fa-solid fa-clock", labelKey: "clinic.userAppointments.shifts.title" },
  { href: Routes.ClinicUser.Reservations.Index, iconClassName: "fa-solid fa-calendar-check", labelKey: "clinic.userAppointments.reservations.title" },
] as const satisfies readonly DashboardShellItem[];

export const accountDashboardNavItems = [
  { href: Routes.Account.Index, iconClassName: "fa-solid fa-gear", labelKey: "account.navigation.settings", exactMatch: true },
  { href: Routes.Account.Name, iconClassName: "fa-solid fa-person", labelKey: "account.navigation.name" },
  { href: Routes.Account.Email, iconClassName: "fa-solid fa-envelope", labelKey: "account.navigation.email" },
  { href: Routes.Account.Phone, iconClassName: "fa-solid fa-mobile", labelKey: "account.navigation.phone" },
  { href: Routes.Account.Password, iconClassName: "fa-solid fa-lock", labelKey: "account.navigation.password" },
  { href: Routes.Account.Passkeys, iconClassName: "fa-solid fa-key", labelKey: "account.navigation.passkeys" },
  { href: Routes.Account.Theme, iconClassName: "fa-solid fa-palette", labelKey: "account.navigation.theme" },
  { href: Routes.Account.Culture, iconClassName: "fa-solid fa-globe", labelKey: "account.navigation.language" },
] as const satisfies readonly DashboardShellItem[];
