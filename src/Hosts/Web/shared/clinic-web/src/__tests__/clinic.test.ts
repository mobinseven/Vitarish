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
