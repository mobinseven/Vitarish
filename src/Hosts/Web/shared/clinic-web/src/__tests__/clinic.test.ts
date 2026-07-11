import { describe, expect, it } from "vitest";
import { renderHook } from "@testing-library/react";
import { CalendarDate } from "@internationalized/date";
import { useAgendaEvents } from "../features/clinic/appointments/agenda/hooks/useAgendaEvents";

describe("useAgendaEvents", () => {
  it("is exported from clinic-web", () => {
    expect(typeof useAgendaEvents).toBe("function");
  });

  it("places Sunday shifts on Sunday without a weekday offset", () => {
    const sunday = new CalendarDate(2026, 7, 12);
    const { result } = renderHook(() =>
      useAgendaEvents(
        sunday,
        "day",
        [{
          id: "shift-1",
          createdAt: "2026-07-01T00:00:00Z",
          isDeleted: false,
          status: 0,
          visibility: 0,
          dayOfWeek: 0,
          startTime: "09:00",
          endTime: "10:00",
          timeZone: "UTC",
          totalSeats: 1,
        }],
        []
      )
    );

    expect(result.current).toHaveLength(1);
    expect(result.current[0]?.start.toString()).toBe("2026-07-12T09:00:00");
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
