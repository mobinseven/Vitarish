import { describe, expect, it } from "vitest";
import { getReactAriaLocale, getDateCalendar } from "@/localization/dateLocale";
import { supportedLocales, type SupportedLocale } from "@/localization/locales";

describe("dateLocale", () => {
  it("maps fa to fa-IR-u-ca-persian", () => {
    expect(getReactAriaLocale("fa")).toBe("fa-IR-u-ca-persian");
  });

  it("maps en to en-US-u-ca-gregory", () => {
    expect(getReactAriaLocale("en")).toBe("en-US-u-ca-gregory");
  });

  it("maps fa calendar to persian", () => {
    expect(getDateCalendar("fa")).toBe("persian");
  });

  it("maps en calendar to gregory", () => {
    expect(getDateCalendar("en")).toBe("gregory");
  });

  it("covers all supported locales", () => {
    const mappedLocales = new Set<SupportedLocale>();
    for (const locale of supportedLocales) {
      getReactAriaLocale(locale);
      getDateCalendar(locale);
      mappedLocales.add(locale);
    }
    expect(mappedLocales.size).toBe(supportedLocales.length);
  });
});
