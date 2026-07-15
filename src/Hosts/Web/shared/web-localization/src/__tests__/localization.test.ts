import { describe, expect, it } from "vitest";
import { resolveLocale } from "../resolveLocale";
import { formatDate, formatFileSize, formatNumber } from "../format";
import { parseIsoDateValue, dateValueToIsoDate } from "../dateValue";

describe("resolveLocale", () => {
  it("returns default when no input", () => {
    const result = resolveLocale();
    expect(result.locale).toBe("fa");
    expect(result.source).toBe("default");
  });

  it("prefers account locale over cookie", () => {
    const result = resolveLocale({ accountLocale: "en", cookieLocale: "fa" });
    expect(result.locale).toBe("en");
    expect(result.source).toBe("account");
  });

  it("falls back to cookie", () => {
    const result = resolveLocale({ cookieLocale: "en" });
    expect(result.locale).toBe("en");
    expect(result.source).toBe("cookie");
  });
});

describe("formatDate", () => {
  it("formats file sizes with locale-aware units", () => {
    expect(formatFileSize(1536, "en")).toContain("1.5");
    expect(formatFileSize(0, "fa")).not.toBe("");
    expect(formatFileSize(-1, "en")).toBe("");
  });
  it("formats a date string", () => {
    const result = formatDate("2024-01-15", "en");
    expect(result).toContain("1");
    expect(result).toContain("2024");
  });

  it("returns empty string for invalid date", () => {
    expect(formatDate("not-a-date", "en")).toBe("");
  });
});

describe("formatNumber", () => {
  it("formats a number", () => {
    const result = formatNumber(1234.56, "en");
    expect(result).toContain("1");
    expect(result).toContain("234");
  });
});

describe("parseIsoDateValue", () => {
  it("parses valid ISO date", () => {
    const result = parseIsoDateValue("2024-01-15");
    expect(result).not.toBeNull();
  });

  it("returns null for invalid date", () => {
    expect(parseIsoDateValue("not-a-date")).toBeNull();
  });

  it("returns null for empty input", () => {
    expect(parseIsoDateValue(null)).toBeNull();
  });
});
