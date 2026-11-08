import { describe, expect, it } from "vitest";
import { LegacyPaths, legacyRouteTarget } from "@/app/legacyRoutes";
import { Routes } from "@/app/routes";

describe("legacy clinic routes", () => {
  it("sends every retired list address to its new home", () => {
    expect(legacyRouteTarget(LegacyPaths.Reservations)).toBe(Routes.Clinic.Appointments.Reservations.Index);
    expect(legacyRouteTarget(LegacyPaths.Settings)).toBe(Routes.Clinic.Settings.Appointments);
    expect(legacyRouteTarget(LegacyPaths.Shifts)).toBe(Routes.Clinic.Settings.Shifts.Index);
    expect(legacyRouteTarget(LegacyPaths.Services)).toBe(Routes.Clinic.Settings.Services.Index);
    expect(legacyRouteTarget(LegacyPaths.ServiceTypes)).toBe(Routes.Clinic.Settings.Services.Types.Index);
    expect(legacyRouteTarget(LegacyPaths.ServiceProviders)).toBe(Routes.Clinic.Settings.Services.Providers.Index);
  });

  it("keeps ids when a retired detail address redirects", () => {
    expect(legacyRouteTarget(`${LegacyPaths.Reservations}/r1`)).toBe(Routes.Clinic.Appointments.Reservations.Detail("r1"));
    expect(legacyRouteTarget(`${LegacyPaths.Shifts}/s1`)).toBe(Routes.Clinic.Settings.Shifts.Detail("s1"));
    expect(legacyRouteTarget(`${LegacyPaths.Shifts}/s1/edit`)).toBe(Routes.Clinic.Settings.Shifts.Edit("s1"));
    expect(legacyRouteTarget(`${LegacyPaths.Shifts}/s1/occurrence/2026-01-02`)).toBe(
      Routes.Clinic.Settings.Shifts.Occurrence("s1", "2026-01-02"),
    );
    expect(legacyRouteTarget(`${LegacyPaths.Services}/v1`)).toBe(Routes.Clinic.Settings.Services.Detail("v1"));
    expect(legacyRouteTarget(`${LegacyPaths.ServiceTypes}/t1`)).toBe(Routes.Clinic.Settings.Services.Types.Detail("t1"));
    expect(legacyRouteTarget(`${LegacyPaths.ServiceProviders}/p1`)).toBe(
      Routes.Clinic.Settings.Services.Providers.Detail("p1"),
    );
  });

  it("prefers the new-shift form over a shift id", () => {
    expect(legacyRouteTarget(`${LegacyPaths.Shifts}/new`)).toBe(Routes.Clinic.Settings.Shifts.New);
    expect(legacyRouteTarget(`${LegacyPaths.Shifts}/new/monday`)).toBe(Routes.Clinic.Settings.Shifts.NewWithDay("monday"));
  });

  it("leaves every current clinic address alone", () => {
    expect(legacyRouteTarget(Routes.Clinic.Appointments.Reservations.Index)).toBeUndefined();
    expect(legacyRouteTarget(Routes.Clinic.Settings.Shifts.Index)).toBeUndefined();
    expect(legacyRouteTarget("/does-not-exist")).toBeUndefined();
  });
});
