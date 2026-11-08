import type { MessageKey } from "@/localization";
import { Routes } from "./routes";

/** A place a person goes from the sidebar or the command menu. */
export interface NavigationDestination {
  id: string;
  href: string;
  icon: string;
  labelKey: MessageKey;
  /** Path prefixes whose pages belong to this destination; the longest match wins. */
  owns: readonly string[];
  shortcut?: string;
  keywords: readonly string[];
}

const home = {
  id: "home",
  href: Routes.Home,
  icon: "house",
  labelKey: "appShell.navigation.home",
  owns: [Routes.Home],
  shortcut: "G H",
  keywords: ["home", "dashboard", "main"],
} as const satisfies NavigationDestination;

/** The clinic owner's destinations, in sidebar order. */
export const clinicOwnerDestinations = [
  home,
  {
    id: "patients",
    href: Routes.Clinic.Patients.Index,
    icon: "users",
    labelKey: "clinic.patients.title",
    owns: [Routes.Clinic.Patients.Index],
    shortcut: "G P",
    keywords: ["patients", "users", "list"],
  },
  {
    id: "check-in",
    href: Routes.Clinic.CheckIn,
    icon: "clipboard-check",
    labelKey: "clinic.dashboard.sections.checkIn.title",
    owns: [Routes.Clinic.CheckIn],
    shortcut: "G C",
    keywords: ["checkin", "check-in", "arrival"],
  },
  {
    id: "appointments",
    href: Routes.Clinic.Appointments.Reservations.Index,
    icon: "calendar-check",
    labelKey: "appShell.navigation.schedule",
    owns: [Routes.Clinic.Appointments.Reservations.Index],
    shortcut: "G A",
    keywords: ["appointments", "reservations", "booking"],
  },
  {
    id: "surgeries",
    href: Routes.Clinic.Surgeries.Index,
    icon: "scissors",
    labelKey: "clinic.surgeries.title",
    owns: [Routes.Clinic.Surgeries.Index],
    shortcut: "G S",
    keywords: ["surgeries", "surgery", "operations"],
  },
  {
    id: "reminders",
    href: Routes.Clinic.Reminders,
    icon: "bell",
    labelKey: "clinic.dashboard.sections.reminders.title",
    owns: [Routes.Clinic.Reminders],
    shortcut: "G R",
    keywords: ["reminders", "alerts", "notifications"],
  },
  {
    id: "settings",
    href: Routes.Clinic.Settings.Index,
    icon: "gear",
    labelKey: "clinic.dashboard.sections.settings.title",
    owns: [Routes.Clinic.Settings.Index],
    keywords: ["settings", "configuration", "preferences"],
  },
] as const satisfies readonly NavigationDestination[];

/** The destinations of a signed-in clinic user who is not the owner. */
export const clinicUserDestinations = [
  home,
  {
    id: "shifts",
    href: Routes.ClinicUser.Shifts.Index,
    icon: "clock",
    labelKey: "clinic.userAppointments.shifts.title",
    owns: [Routes.ClinicUser.Shifts.Index],
    keywords: ["shifts", "schedule", "work"],
  },
  {
    id: "reservations",
    href: Routes.ClinicUser.Reservations.Index,
    icon: "calendar-check",
    labelKey: "clinic.userAppointments.reservations.title",
    owns: [Routes.ClinicUser.Reservations.Index],
    keywords: ["reservations", "appointments", "booking"],
  },
] as const satisfies readonly NavigationDestination[];

/** The destination a page belongs to, or undefined for pages outside the sidebar, such as the account. */
export function destinationForPath<T extends { owns?: readonly string[] }>(
  pathname: string,
  destinations: readonly T[],
): T | undefined {
  let best: T | undefined;
  let bestLength = -1;

  for (const destination of destinations) {
    for (const prefix of destination.owns ?? []) {
      const owned = pathname === prefix || pathname.startsWith(`${prefix}/`);
      if (owned && prefix.length > bestLength) {
        best = destination;
        bestLength = prefix.length;
      }
    }
  }

  return best;
}
