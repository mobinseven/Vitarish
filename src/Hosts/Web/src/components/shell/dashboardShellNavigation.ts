import { Routes } from "@/app/routes";
import type { DashboardShellItem, DashboardShellNavGroup } from "./DashboardShell";

const homeItem = {
  href: Routes.Home,
  iconClassName: "fa-solid fa-house",
  labelKey: "appShell.navigation.home",
  exactMatch: true,
} as const satisfies DashboardShellItem;

const knowledgeFeatureItem = {
  href: Routes.Knowledge.Index,
  iconClassName: "fa-solid fa-book-open",
  labelKey: "appShell.navigation.knowledge",
  exactMatch: true,
  children: [
    {
      href: Routes.Knowledge.QA,
      iconClassName: "fa-solid fa-comments",
      labelKey: "knowledge.qa.title",
      exactMatch: true,
    },
  ] as const satisfies readonly DashboardShellItem[],
} as const satisfies DashboardShellItem;

export const dashboardFooterItems = [
  { href: Routes.Account.Index, iconClassName: "fa-solid fa-circle-user", labelKey: "appShell.userMenu.account" },
  { href: Routes.Logout, iconClassName: "fa-solid fa-arrow-right-from-bracket", labelKey: "appShell.userMenu.signOut", exactMatch: true },
] as const satisfies readonly DashboardShellItem[];

const clinicOwnerFeatureItem = {
  href: Routes.Clinic.Index,
  iconClassName: "fa-solid fa-house-medical",
  labelKey: "appShell.navigation.clinic",
  exactMatch: true,
  titleLabelKey: "clinic.dashboard.title",
  children: [
    { href: Routes.Clinic.Patients.Index, iconClassName: "fa-solid fa-users", labelKey: "clinic.patients.title" },
    { href: Routes.Clinic.CheckIn, iconClassName: "fa-solid fa-clipboard-check", labelKey: "clinic.dashboard.sections.checkIn.title" },
    { href: Routes.Clinic.Appointments.Reservations.Index, iconClassName: "fa-solid fa-calendar-check", labelKey: "clinic.appointments.reservations.title" },
    { href: Routes.Clinic.Appointments.Shifts.Index, iconClassName: "fa-solid fa-clock", labelKey: "clinic.appointments.shifts.title" },
    { href: Routes.Clinic.Appointments.Settings, iconClassName: "fa-solid fa-calendar-gear", labelKey: "clinic.appointments.settings.title" },
    { href: Routes.Clinic.Appointments.ServiceProviders.Index, iconClassName: "fa-solid fa-user-doctor", labelKey: "clinic.serviceProviders.title" },
    { href: Routes.Clinic.Appointments.ServiceTypes.Index, iconClassName: "fa-solid fa-tags", labelKey: "clinic.serviceTypes.title" },
    { href: Routes.Clinic.Appointments.Services.Index, iconClassName: "fa-solid fa-briefcase-medical", labelKey: "clinic.services.title" },
    { href: Routes.Clinic.Reminders, iconClassName: "fa-solid fa-bell", labelKey: "clinic.dashboard.sections.reminders.title" },
    { href: Routes.Clinic.Settings.Index, iconClassName: "fa-solid fa-gear", labelKey: "clinic.dashboard.sections.settings.title" },
  ] as const satisfies readonly DashboardShellItem[],
} as const satisfies DashboardShellItem;

const clinicUserFeatureItem = {
  href: Routes.ClinicUser.Shifts.Index,
  iconClassName: "fa-solid fa-house-medical",
  labelKey: "appShell.navigation.clinic",
  children: [
    { href: Routes.ClinicUser.Shifts.Index, iconClassName: "fa-solid fa-clock", labelKey: "clinic.userAppointments.shifts.title" },
    { href: Routes.ClinicUser.Reservations.Index, iconClassName: "fa-solid fa-calendar-check", labelKey: "clinic.userAppointments.reservations.title" },
  ] as const satisfies readonly DashboardShellItem[],
} as const satisfies DashboardShellItem;

const homeGroup: DashboardShellNavGroup = {
  labelKey: "appShell.navigation.mainGroup",
  items: [clinicOwnerFeatureItem, knowledgeFeatureItem] as const satisfies readonly DashboardShellItem[],
};

export const homeDashboardNavGroups = [homeGroup] as const satisfies readonly DashboardShellNavGroup[];

const appFeatureGroup: DashboardShellNavGroup = {
  labelKey: "appShell.navigation.mainGroup",
  items: [homeItem, clinicOwnerFeatureItem, knowledgeFeatureItem] as const satisfies readonly DashboardShellItem[],
};

export const mainDashboardNavGroups = [appFeatureGroup] as const satisfies readonly DashboardShellNavGroup[];

const clinicOwnerGroup: DashboardShellNavGroup = {
  labelKey: "appShell.navigation.mainGroup",
  items: [homeItem, clinicOwnerFeatureItem, knowledgeFeatureItem] as const satisfies readonly DashboardShellItem[],
};

export const clinicOwnerDashboardNavGroups = [clinicOwnerGroup] as const satisfies readonly DashboardShellNavGroup[];

const clinicUserGroup: DashboardShellNavGroup = {
  labelKey: "appShell.navigation.mainGroup",
  items: [homeItem, clinicUserFeatureItem, knowledgeFeatureItem] as const satisfies readonly DashboardShellItem[],
};

export const clinicUserDashboardNavGroups = [clinicUserGroup] as const satisfies readonly DashboardShellNavGroup[];

const accountGroup: DashboardShellNavGroup = {
  labelKey: "appShell.navigation.accountGroup",
  items: [
    { href: Routes.Account.Index, iconClassName: "fa-solid fa-gear", labelKey: "account.navigation.settings", exactMatch: true },
    { href: Routes.Account.Name, iconClassName: "fa-solid fa-person", labelKey: "account.navigation.name" },
    { href: Routes.Account.Email, iconClassName: "fa-solid fa-envelope", labelKey: "account.navigation.email" },
    { href: Routes.Account.Phone, iconClassName: "fa-solid fa-mobile", labelKey: "account.navigation.phone" },
    { href: Routes.Account.Password, iconClassName: "fa-solid fa-lock", labelKey: "account.navigation.password" },
    { href: Routes.Account.Passkeys, iconClassName: "fa-solid fa-key", labelKey: "account.navigation.passkeys" },
    { href: Routes.Account.Theme, iconClassName: "fa-solid fa-palette", labelKey: "account.navigation.theme" },
    { href: Routes.Account.Culture, iconClassName: "fa-solid fa-globe", labelKey: "account.navigation.language" },
  ] as const satisfies readonly DashboardShellItem[],
};

export const accountDashboardNavGroups = [accountGroup] as const satisfies readonly DashboardShellNavGroup[];
