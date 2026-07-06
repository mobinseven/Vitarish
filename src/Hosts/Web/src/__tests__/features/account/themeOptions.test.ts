import { describe, expect, it } from "vitest";
import {
  accountThemeOptions,
  decodeAccountThemePreference,
  encodeAccountThemePreference,
  isAccountThemeValue,
  resolveThemeMode,
  setVibrantPaletteAttribute,
} from "@/features/account/theme/themeOptions";

describe("account theme options", () => {
  it("contains every supported HeroUI Pro palette mode", () => {
    expect(accountThemeOptions.map((option) => option.value)).toEqual([
      "system",
      "light",
      "dark",
      "brutalism-light",
      "brutalism-dark",
      "glass-light",
      "glass-dark",
      "mouve-light",
      "mouve-dark",
    ]);
  });

  it("validates persisted theme values", () => {
    expect(isAccountThemeValue("glass-dark")).toBe(true);
    expect(isAccountThemeValue("unknown")).toBe(false);
    expect(isAccountThemeValue(undefined)).toBe(false);
  });

  it("resolves the mode from default and palette theme values", () => {
    expect(resolveThemeMode("system")).toBe("system");
    expect(resolveThemeMode("light")).toBe("light");
    expect(resolveThemeMode("mouve-light")).toBe("light");
    expect(resolveThemeMode("dark")).toBe("dark");
    expect(resolveThemeMode("brutalism-dark")).toBe("dark");
  });

  it("encodes and decodes the vibrant palette modifier", () => {
    expect(encodeAccountThemePreference({ theme: "glass-dark", isVibrant: true })).toBe(
      "glass-dark:vibrant"
    );
    expect(decodeAccountThemePreference("glass-dark:vibrant")).toEqual({
      theme: "glass-dark",
      isVibrant: true,
    });
    expect(decodeAccountThemePreference("glass-dark")).toEqual({
      theme: "glass-dark",
      isVibrant: false,
    });
    expect(decodeAccountThemePreference("unknown:vibrant")).toEqual({
      theme: "system",
      isVibrant: true,
    });
  });

  it("applies and removes the vibrant palette document attribute", () => {
    const root = document.createElement("html");

    setVibrantPaletteAttribute(true, root);
    expect(root.dataset.vibrantPalette).toBe("true");

    setVibrantPaletteAttribute(false, root);
    expect(root.dataset.vibrantPalette).toBeUndefined();
  });
});
