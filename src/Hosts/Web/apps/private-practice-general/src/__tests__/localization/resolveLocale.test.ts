import { describe, expect, it } from "vitest";
import { defaultLocale, resolveLocale } from "@/localization";

describe("resolveLocale", () => {
  it("uses default locale without inputs", () => {
    expect(resolveLocale().locale).toBe(defaultLocale);
  });

  it("uses supported account locale before cookie", () => {
    expect(resolveLocale({ accountLocale: "en", cookieLocale: "fr" }).source).toBe("account");
  });

  it("uses supported cookie locale", () => {
    expect(resolveLocale({ cookieLocale: "en" }).source).toBe("cookie");
  });

  it("falls back from unsupported cookie", () => {
    expect(resolveLocale({ cookieLocale: "fr" }).source).toBe("default");
  });

  it("uses Accept-Language fallback", () => {
    expect(resolveLocale({ acceptLanguageLocales: ["fr-CA", "en-US"] }).source).toBe("accept-language");
  });
});
