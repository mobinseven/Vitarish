"use client";

import { type ReactNode, useEffect, useState } from "react";
import { useTheme } from "next-themes";
import {
  decodeAccountThemePreference,
  paletteStorageKey,
  setPaletteAttribute,
  type AccountThemeValue,
} from "@/features/account/theme/themeOptions";

export function PaletteProvider({ children }: { children: ReactNode }) {
  const { resolvedTheme } = useTheme();
  const [palette, setPalette] = useState<AccountThemeValue>("default");

  useEffect(() => {
    const storedPalette = window.localStorage.getItem(paletteStorageKey);
    setPalette(decodeAccountThemePreference(storedPalette).theme);

    const handleStorage = (event: StorageEvent) => {
      if (event.key === paletteStorageKey) {
        setPalette(decodeAccountThemePreference(event.newValue).theme);
      }
    };

    window.addEventListener("storage", handleStorage);
    return () => window.removeEventListener("storage", handleStorage);
  }, []);

  useEffect(() => {
    setPaletteAttribute(
      palette,
      resolvedTheme === "dark" ? "dark" : "light",
      document.documentElement
    );
  }, [palette, resolvedTheme]);

  return children;
}
