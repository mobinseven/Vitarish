import { Routes } from "@/app/routes";

const clinicEventWorkspaceRoute = new RegExp(
  `^${escapeRegex(Routes.Clinic.Patients.Index)}/[^/]+/procedures/[^/]+/events/[^/]+$`,
);

export function isClinicWorkspaceRoute(pathname: string) {
  const normalizedPathname =
    pathname.length > 1 ? pathname.replace(/\/+$/, "") : pathname;

  return clinicEventWorkspaceRoute.test(normalizedPathname);
}

function escapeRegex(value: string) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}
