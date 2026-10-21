import { describe, expect, it } from "vitest";
import { resolveLocale } from "../resolveLocale";
import { parseAcceptLanguage } from "../acceptLanguage";
import { resolveI18nAppLocale } from "../I18nAppLayout";
import { formatDate, formatFileSize, formatNumber, formatTimeOfDay } from "../format";
import { dateValueToIsoDate, parseIsoDateValue, toLocalizedDateValue } from "../dateValue";

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

  it("prefers a user selection over the browser culture", () => {
    const result = resolveLocale({ selectedLocale: "fa-IR", browserLocales: ["en-US"] });
    expect(result.locale).toBe("fa");
    expect(result.source).toBe("selected");
  });

  it("uses the first supported browser culture when there is no user selection", () => {
    const result = resolveLocale({ browserLocales: ["fr-CA", "en-US"] });
    expect(result.locale).toBe("en");
    expect(result.source).toBe("browser");
  });

  it("falls back to the default when the selection and browser cultures are unsupported", () => {
    const result = resolveLocale({ selectedLocale: "de-DE", browserLocales: ["fr-CA"] });
    expect(result.locale).toBe("fa");
    expect(result.source).toBe("default");
  });
});

describe("parseAcceptLanguage", () => {
  it("orders browser cultures by quality and ignores rejected or wildcard values", () => {
    expect(parseAcceptLanguage("fa-IR;q=0.7, en-US;q=0.9, *;q=1, de;q=0")).toEqual(["en-US", "fa-IR"]);
  });
});

describe("resolveI18nAppLocale", () => {
  it("prefers an explicit selection, then a persisted selection, then browser detection", () => {
    expect(resolveI18nAppLocale({
      selectedLocale: "fa-IR",
      cookieHeader: "vitarish.locale=en",
      acceptLanguageHeader: "en-US",
    }).locale).toBe("fa");

    expect(resolveI18nAppLocale({
      cookieHeader: "vitarish.locale=en",
      acceptLanguageHeader: "fa-IR",
    }).locale).toBe("en");

    expect(resolveI18nAppLocale({ acceptLanguageHeader: "en-US" }).locale).toBe("en");
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

describe("formatTimeOfDay", () => {
  it("writes a server time of day in the locale's digits without seconds", () => {
    expect(formatTimeOfDay("09:00:00", "fa")).toBe(formatTimeOfDay("09:00", "fa"));
    expect(formatTimeOfDay("09:00:00", "fa")).toMatch(/^[۰-۹]{1,2}:[۰-۹]{2}$/);
    expect(formatTimeOfDay("13:05:00", "en")).toMatch(/^1:05\sPM$/);
  });
  it("leaves a value it cannot read unchanged", () => {
    expect(formatTimeOfDay("not-a-time", "en")).toBe("not-a-time");
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

  it("uses the locale calendar while preserving Gregorian API dates", () => {
    const localized = toLocalizedDateValue(parseIsoDateValue("2024-01-15"), "fa");

    expect(localized?.calendar.identifier).toBe("persian");
    expect(dateValueToIsoDate(localized)).toBe("2024-01-15");
  });
});
