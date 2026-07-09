import { describe, expect, it } from "vitest";
import { parseIsoDateValue, dateValueToIsoDate } from "@/localization/dateValue";

describe("dateValue", () => {
  describe("parseIsoDateValue", () => {
    it("parses a valid ISO date string", () => {
      const result = parseIsoDateValue("2026-07-06");
      expect(result).not.toBeNull();
      expect(result!.toString()).toBe("2026-07-06");
    });

    it("returns null for empty string", () => {
      expect(parseIsoDateValue("")).toBeNull();
    });

    it("returns null for null", () => {
      expect(parseIsoDateValue(null)).toBeNull();
    });

    it("returns null for undefined", () => {
      expect(parseIsoDateValue(undefined)).toBeNull();
    });

    it("returns null for malformed date", () => {
      expect(parseIsoDateValue("not-a-date")).toBeNull();
    });

    it("returns null for incomplete date", () => {
      expect(parseIsoDateValue("2026-07")).toBeNull();
    });

    it("returns null for date with time", () => {
      expect(parseIsoDateValue("2026-07-06T12:00:00")).toBeNull();
    });
  });

  describe("dateValueToIsoDate", () => {
    it("converts a DateValue to ISO string", () => {
      const dateValue = parseIsoDateValue("2026-07-06");
      expect(dateValueToIsoDate(dateValue)).toBe("2026-07-06");
    });

    it("returns empty string for null", () => {
      expect(dateValueToIsoDate(null)).toBe("");
    });

    it("round-trips correctly", () => {
      const input = "2026-07-06";
      const parsed = parseIsoDateValue(input);
      const output = dateValueToIsoDate(parsed);
      expect(output).toBe(input);
    });
  });
});
