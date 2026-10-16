import { describe, expect, it } from "vitest";
import { parseDate } from "@internationalized/date";
import { surgeryCalendarRange } from "../models/surgeries/surgeryCalendarRange";

const timeZone = "Asia/Tehran";

describe("surgeryCalendarRange", () => {
  it("covers six weeks from the locale's first day of the week", () => {
    const range = surgeryCalendarRange(
      "month",
      parseDate("2026-05-15"),
      timeZone,
      "en-US",
    );

    expect(range.days).toHaveLength(42);
    expect(range.days[0].toString()).toBe("2026-04-26");
    expect(range.days[41].toString()).toBe("2026-06-06");
  });

  it("makes to the exclusive midnight after the last visible day", () => {
    const range = surgeryCalendarRange(
      "month",
      parseDate("2026-05-15"),
      timeZone,
      "en-US",
    );
    const lastDay = range.days[range.days.length - 1];
    const lastMidnight = lastDay.toDate(timeZone).getTime();

    expect(new Date(range.from).getTime()).toBe(
      range.days[0].toDate(timeZone).getTime(),
    );
    expect(new Date(range.to).getTime() - lastMidnight).toBe(24 * 60 * 60 * 1000);
  });

  it("shows a single week in the week view", () => {
    const range = surgeryCalendarRange(
      "week",
      parseDate("2026-05-15"),
      timeZone,
      "en-US",
    );

    expect(range.days).toHaveLength(7);
  });
});
