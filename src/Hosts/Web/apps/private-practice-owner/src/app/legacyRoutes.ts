import { notFound, redirect } from "next/navigation";
import { Routes } from "./routes";

interface LegacyRoute {
  /** The retired address, with capture groups for its ids. */
  pattern: RegExp;
  target: (...ids: string[]) => string;
}

/** The retired top-level addresses, kept in one place for the redirect pages and the rules below. */
export const LegacyPaths = {
  Reservations: "/appointments/reservations",
  Settings: "/appointments/settings",
  Shifts: "/appointments/shifts",
  Services: "/appointments/services",
  ServiceTypes: "/appointments/service-types",
  ServiceProviders: "/appointments/service-providers",
} as const;

const segment = "([^/]+)";

/**
 * Addresses the clinic app used before shifts, services and appointment settings moved under Settings
 * and the appointment detail moved beside the list. They redirect so bookmarks and the links already
 * sent in SMS and email keep working. Remove a rule when nothing links to it for one release.
 */
const legacyRoutes: readonly LegacyRoute[] = [
  { pattern: new RegExp(`^${LegacyPaths.Reservations}$`), target: () => Routes.Clinic.Appointments.Reservations.Index },
  { pattern: new RegExp(`^${LegacyPaths.Reservations}/${segment}$`), target: Routes.Clinic.Appointments.Reservations.Detail },
  { pattern: new RegExp(`^${LegacyPaths.Settings}$`), target: () => Routes.Clinic.Settings.Appointments },
  { pattern: new RegExp(`^${LegacyPaths.Shifts}$`), target: () => Routes.Clinic.Settings.Shifts.Index },
  { pattern: new RegExp(`^${LegacyPaths.Shifts}/new$`), target: () => Routes.Clinic.Settings.Shifts.New },
  { pattern: new RegExp(`^${LegacyPaths.Shifts}/new/${segment}$`), target: Routes.Clinic.Settings.Shifts.NewWithDay },
  { pattern: new RegExp(`^${LegacyPaths.Shifts}/${segment}$`), target: Routes.Clinic.Settings.Shifts.Detail },
  { pattern: new RegExp(`^${LegacyPaths.Shifts}/${segment}/edit$`), target: Routes.Clinic.Settings.Shifts.Edit },
  { pattern: new RegExp(`^${LegacyPaths.Shifts}/${segment}/occurrence/${segment}$`), target: Routes.Clinic.Settings.Shifts.Occurrence },
  { pattern: new RegExp(`^${LegacyPaths.Services}$`), target: () => Routes.Clinic.Settings.Services.Index },
  { pattern: new RegExp(`^${LegacyPaths.Services}/${segment}$`), target: Routes.Clinic.Settings.Services.Detail },
  { pattern: new RegExp(`^${LegacyPaths.ServiceTypes}$`), target: () => Routes.Clinic.Settings.Services.Types.Index },
  { pattern: new RegExp(`^${LegacyPaths.ServiceTypes}/${segment}$`), target: Routes.Clinic.Settings.Services.Types.Detail },
  { pattern: new RegExp(`^${LegacyPaths.ServiceProviders}$`), target: () => Routes.Clinic.Settings.Services.Providers.Index },
  { pattern: new RegExp(`^${LegacyPaths.ServiceProviders}/${segment}$`), target: Routes.Clinic.Settings.Services.Providers.Detail },
];

/** Where a retired address now lives, or undefined when the address was never a clinic route. */
export function legacyRouteTarget(pathname: string): string | undefined {
  for (const { pattern, target } of legacyRoutes) {
    const match = pattern.exec(pathname);
    if (match) return target(...match.slice(1));
  }
  return undefined;
}

/** Sends a retired address to its new home, or to the in-shell not-found page. */
export function redirectLegacyRoute(pathname: string): never {
  const target = legacyRouteTarget(pathname);
  if (target) redirect(target);
  notFound();
}
