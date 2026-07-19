import { describe, expect, it } from "vitest";
import { isClinicWorkspaceRoute } from "@/app/owner/clinic/clinicRouteLayout";

describe("clinic route layout policy", () => {
  it.each([
    "/owner/clinic/patients/patient-1/procedures/procedure-1/events/event-1",
    "/owner/clinic/patients/patient-1/procedures/procedure-1/events/event-1/",
  ])("uses workspace layout for the full-height tool %s", (pathname) => {
    expect(isClinicWorkspaceRoute(pathname)).toBe(true);
  });

  it.each([
    "/owner/clinic",
    "/owner/clinic/appointment-settings",
    "/owner/clinic/check-in",
    "/owner/clinic/patients",
    "/owner/clinic/patients/",
    "/owner/clinic/patients/new",
    "/owner/clinic/patients/patient-1",
    "/owner/clinic/patients/patient-1/edit",
    "/owner/clinic/patients/patient-1/procedures/procedure-1",
    "/owner/clinic/reminders",
    "/owner/clinic/reservations",
    "/owner/clinic/reservations/reservation-1",
    "/owner/clinic/service-providers",
    "/owner/clinic/service-providers/provider-1",
    "/owner/clinic/service-types",
    "/owner/clinic/service-types/type-1",
    "/owner/clinic/services",
    "/owner/clinic/services/service-1",
    "/owner/clinic/settings",
    "/owner/clinic/settings/statistics",
    "/owner/clinic/settings/users",
    "/owner/clinic/shifts",
    "/owner/clinic/shifts/new",
    "/owner/clinic/shifts/shift-1",
    "/owner/clinic/shifts/shift-1/edit",
    "/owner/clinic/shifts/shift-1/occurrence/2026-07-18",
  ])("keeps normal clinic page %s in frame-page layout", (pathname) => {
    expect(isClinicWorkspaceRoute(pathname)).toBe(false);
  });
});
