import { describe, expect, it } from "vitest";
import { isClinicWorkspaceRoute } from "@/app/(owner)/clinicRouteLayout";

describe("clinic route layout policy", () => {
  it.each([
    "/patients/patient-1/procedures/procedure-1/events/event-1",
    "/patients/patient-1/procedures/procedure-1/events/event-1/",
  ])("uses workspace layout for the full-height tool %s", (pathname) => {
    expect(isClinicWorkspaceRoute(pathname)).toBe(true);
  });

  it.each([
    "/",
    "/appointments/settings",
    "/check-in",
    "/patients",
    "/patients/",
    "/patients/new",
    "/patients/patient-1",
    "/patients/patient-1/edit",
    "/patients/patient-1/procedures/procedure-1",
    "/reminders",
    "/appointments",
    "/appointments/reservation-1",
    "/appointments/service-providers",
    "/appointments/service-providers/provider-1",
    "/appointments/service-types",
    "/appointments/service-types/type-1",
    "/appointments/services",
    "/appointments/services/service-1",
    "/settings",
    "/settings/statistics",
    "/settings/users",
    "/appointments/shifts",
    "/appointments/shifts/new",
    "/appointments/shifts/shift-1",
    "/appointments/shifts/shift-1/edit",
    "/appointments/shifts/shift-1/occurrence/2026-07-18",
  ])("keeps normal clinic page %s in frame-page layout", (pathname) => {
    expect(isClinicWorkspaceRoute(pathname)).toBe(false);
  });
});
