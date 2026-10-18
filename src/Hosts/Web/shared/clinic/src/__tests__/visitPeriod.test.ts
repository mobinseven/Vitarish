import { describe, expect, it } from "vitest";
import { createCalendar, parseDate, toCalendar } from "@internationalized/date";
import { shiftVisitPeriod, visitPeriodRange } from "../models/appointments/visitPeriod";

describe("visitPeriodRange", () => {
  it("covers the single focused day", () => {
    expect(visitPeriodRange("day", parseDate("2026-10-17"), "en-US")).toEqual({
      dateFrom: "2026-10-17",
      dateTo: "2026-10-17",
    });
  });

  it("starts a week on the locale's first day", () => {
    // 2026-10-17 is a Saturday: the first day of a Persian week, the last of a US week.
    expect(visitPeriodRange("week", parseDate("2026-10-17"), "en-US")).toEqual({
      dateFrom: "2026-10-11",
      dateTo: "2026-10-17",
    });
    expect(visitPeriodRange("week", parseDate("2026-10-17"), "fa-IR")).toEqual({
      dateFrom: "2026-10-17",
      dateTo: "2026-10-23",
    });
  });

  it("follows the month of the focused date's own calendar and returns Gregorian dates", () => {
    // 25 Mehr 1405 falls on 2026-10-17; Mehr 1405 runs from 2026-09-23 to 2026-10-22.
    const persian = toCalendar(parseDate("2026-10-17"), createCalendar("persian"));

    expect(visitPeriodRange("month", persian, "fa-IR")).toEqual({
      dateFrom: "2026-09-23",
      dateTo: "2026-10-22",
    });
  });
});

describe("shiftVisitPeriod", () => {
  it("moves by whole periods", () => {
    const focused = parseDate("2026-10-17");

    expect(shiftVisitPeriod(focused, "day", -1).toString()).toBe("2026-10-16");
    expect(shiftVisitPeriod(focused, "week", 1).toString()).toBe("2026-10-24");
    expect(shiftVisitPeriod(focused, "month", 1).toString()).toBe("2026-11-17");
  });
});
