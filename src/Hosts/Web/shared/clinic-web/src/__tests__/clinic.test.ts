import { describe, expect, it } from "vitest";
import { renderHook } from "@testing-library/react";
import { CalendarDate } from "@internationalized/date";
import { readdirSync, readFileSync, statSync } from "node:fs";
import { join } from "node:path";
import { useAgendaEvents } from "../hooks/appointments/agenda/useAgendaEvents";
import {
  DashboardPage,
  PatientListPage,
  OwnerShiftListPage,
  ServiceDetailPage,
} from "../index";
import { clinicMessageCatalogs } from "../localization";

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

  it("expands shifts over the selected culture's visible week", () => {
    const sunday = new CalendarDate(2026, 7, 12);
    const { result } = renderHook(() =>
      useAgendaEvents(
        sunday,
        "week",
        [{
          id: "shift-1",
          createdAt: "2026-07-01T00:00:00Z",
          isDeleted: false,
          status: 0,
          visibility: 0,
          dayOfWeek: 6,
          startTime: "09:00",
          endTime: "10:00",
          timeZone: "UTC",
          totalSeats: 1,
        }],
        [],
        "fa"
      )
    );

    expect(result.current).toHaveLength(1);
    expect(result.current[0]?.start.toString()).toBe("2026-07-11T09:00:00");
  });

  it("expands shifts across every day visible in the month grid", () => {
    const july = new CalendarDate(2026, 7, 15);
    const { result } = renderHook(() =>
      useAgendaEvents(
        july,
        "month",
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
        [],
        "en"
      )
    );

    expect(result.current).toHaveLength(5);
    expect(result.current[0]?.start.toString()).toBe("2026-06-28T09:00:00");
    expect(result.current.at(-1)?.start.toString()).toBe("2026-07-26T09:00:00");
  });

  it("divides appointment time by capacity or over-capacity reservations", () => {
    const date = new CalendarDate(2026, 7, 12);
    const shift = {
      id: "shift-1",
      createdAt: "2026-07-01T00:00:00Z",
      isDeleted: false,
      status: 0,
      visibility: 0,
      dayOfWeek: 0,
      startTime: "09:00",
      endTime: "10:00",
      timeZone: "UTC",
      totalSeats: 2,
    };
    const reservations = [1, 2, 3].map((id) => ({
      id: `reservation-${id}`,
      createdAt: "2026-07-01T00:00:00Z",
      isDeleted: false,
      status: 1,
      shiftId: "shift-1",
      date: "2026-07-12",
      isHold: false,
    }));

    const { result } = renderHook(() =>
      useAgendaEvents(date, "day", [shift], reservations),
    );

    const reservationEvents = result.current
      .filter((event) => event.id.startsWith("reservation-"));

    expect(reservationEvents.map((event) => event.start.toString())).toEqual([
      "2026-07-12T09:00:00",
      "2026-07-12T09:20:00",
      "2026-07-12T09:40:00",
    ]);
    expect(reservationEvents.map((event) => event.end.toString())).toEqual([
      "2026-07-12T09:20:00",
      "2026-07-12T09:40:00",
      "2026-07-12T10:00:00",
    ]);
  });

  it("does not expose a reservation action for an occurrence absent from the server bookable set", () => {
    const date = new CalendarDate(2026, 7, 12);
    const { result } = renderHook(() =>
      useAgendaEvents(date, "day", [{
        id: "shift-1",
        createdAt: "2026-07-01T00:00:00Z",
        isDeleted: false,
        status: 1,
        visibility: 0,
        dayOfWeek: 0,
        startTime: "09:00",
        endTime: "10:00",
        timeZone: "UTC",
        totalSeats: 1,
      }], [], "en", undefined, new Set()),
    );

    expect(result.current).toHaveLength(0);
  });

  it("uses the reservee name for a reservation event", () => {
    const { result } = renderHook(() =>
      useAgendaEvents(new CalendarDate(2026, 7, 12), "day", [], [{
        id: "reservation-1",
        createdAt: "2026-07-01T00:00:00Z",
        isDeleted: false,
        status: 1,
        shiftId: "shift-1",
        date: "2026-07-12",
        reservingPartyId: "patient-1",
        isHold: false,
      }], "en", undefined, undefined, undefined, undefined, "Shift", undefined, undefined,
        new Map([["patient-1", "Ada Lovelace"]])),
    );

    expect(result.current[0]?.title).toBe("Ada Lovelace");
  });
});

describe("clinic-web public surface", () => {
  it("exports route-ready page surfaces", () => {
    expect(DashboardPage).toBeDefined();
    expect(PatientListPage).toBeDefined();
    expect(OwnerShiftListPage).toBeDefined();
    expect(ServiceDetailPage).toBeDefined();
  });
});

function leafKeys(value: unknown, prefix = ""): string[] {
  if (typeof value !== "object" || value === null || Array.isArray(value)) return [prefix];
  return Object.entries(value).flatMap(([key, child]) =>
    leafKeys(child, prefix ? `${prefix}.${key}` : key)
  );
}

describe("clinic message catalogs", () => {
  it("keeps English and Persian leaf keys equivalent", () => {
    expect(leafKeys(clinicMessageCatalogs.en).sort()).toEqual(
      leafKeys(clinicMessageCatalogs.fa).sort()
    );
  });

  it("resolves every literal clinic key used by package code", () => {
    const sourceRoot = join(import.meta.dirname, "..");
    const files: string[] = [];
    const walk = (directory: string) => {
      for (const entry of readdirSync(directory)) {
        const path = join(directory, entry);
        if (path.includes("__tests__") || path.includes("api\\generated") || path.includes("api/generated")) continue;
        if (statSync(path).isDirectory()) walk(path);
        else if (path.endsWith(".ts") || path.endsWith(".tsx")) files.push(path);
      }
    };
    walk(sourceRoot);
    const keys = new Set<string>();
    for (const file of files) {
      const source = readFileSync(file, "utf8");
      for (const match of source.matchAll(/\bt\(["'](clinic\.[^"']+)["']/g)) keys.add(match[1]);
    }
    const resolve = (catalog: unknown, key: string) =>
      key.split(".").reduce<unknown>(
        (current, segment) =>
          current && typeof current === "object"
            ? (current as Record<string, unknown>)[segment]
            : undefined,
        catalog
      );
    const missing = [...keys].flatMap((key) => [
      ...(typeof resolve(clinicMessageCatalogs.en, key) === "string" ? [] : [`en:${key}`]),
      ...(typeof resolve(clinicMessageCatalogs.fa, key) === "string" ? [] : [`fa:${key}`]),
    ]);
    expect(missing).toEqual([]);
  });
});
