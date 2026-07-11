import { describe, expect, it } from "vitest";
import { useAgendaEvents } from "../features/clinic/appointments/agenda/hooks/useAgendaEvents";

describe("useAgendaEvents", () => {
  it("is exported from clinic-web", () => {
    expect(typeof useAgendaEvents).toBe("function");
  });
});

describe("clinic-web barrel exports", () => {
  it("exports key components", async () => {
    const mod = await import("../index");
    expect(mod.ClinicEmptyState).toBeDefined();
    expect(mod.ClinicErrorState).toBeDefined();
    expect(mod.ClinicLoadingState).toBeDefined();
    expect(mod.useOwnerShifts).toBeDefined();
    expect(mod.useOwnerReservations).toBeDefined();
    expect(mod.OwnerShiftsList).toBeDefined();
    expect(mod.PatientCheckInWizard).toBeDefined();
  }, 10_000);
});
