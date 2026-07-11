import { describe, expect, it } from "vitest";
import {
  accountThemeOptions,
  decodeAccountThemePreference,
  encodeAccountThemePreference,
  isAccountThemeValue,
  resolveThemeMode,
  setPaletteAttribute,
  setVibrantPaletteAttribute,
} from "../components/theme/themeOptions";

describe("account theme options", () => {
  it("contains every supported HeroUI Pro palette without mode variants", () => {
    expect(accountThemeOptions.map((option) => option.value)).toEqual([
      "default",
      "brutalism",
      "glass",
      "mouve",
    ]);
  });

  it("validates persisted theme values", () => {
    expect(isAccountThemeValue("glass")).toBe(true);
    expect(isAccountThemeValue("glass-dark")).toBe(false);
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
    expect(encodeAccountThemePreference({ theme: "glass", isVibrant: true })).toBe(
      "glass:vibrant"
    );
    expect(decodeAccountThemePreference("glass:vibrant")).toEqual({
      theme: "glass",
      isVibrant: true,
    });
    expect(decodeAccountThemePreference("glass")).toEqual({
      theme: "glass",
      isVibrant: false,
    });
    expect(decodeAccountThemePreference("unknown:vibrant")).toEqual({
      theme: "default",
      isVibrant: true,
    });
  });

  it("decodes legacy mode-bearing palette values as palette-only preferences", () => {
    expect(decodeAccountThemePreference("glass-dark:vibrant")).toEqual({
      theme: "glass",
      isVibrant: true,
    });
    expect(decodeAccountThemePreference("system")).toEqual({
      theme: "default",
      isVibrant: false,
    });
  });

  it("applies the selected palette class using the resolved theme mode", () => {
    const root = document.createElement("html");

    setPaletteAttribute("glass", "dark", root);
    expect(root.classList.contains("glass-dark")).toBe(true);

    setPaletteAttribute("mouve", "light", root);
    expect(root.classList.contains("glass-dark")).toBe(false);
    expect(root.classList.contains("mouve-light")).toBe(true);

    setPaletteAttribute("default", "light", root);
    expect(root.className).toBe("");
  });

  it("applies and removes the vibrant palette document attribute", () => {
    const root = document.createElement("html");

    setVibrantPaletteAttribute(true, root);
    expect(root.dataset.vibrantPalette).toBe("true");

    setVibrantPaletteAttribute(false, root);
    expect(root.dataset.vibrantPalette).toBeUndefined();
  });
});
