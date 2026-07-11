"use client";

import { type ReactNode, createContext, useContext, useEffect, useState } from "react";
import { usePathname } from "next/navigation";
import { useTheme } from "next-themes";
import {
  decodeAccountThemePreference,
  paletteStorageKey,
  setPaletteAttribute,
  type AccountThemeValue,
} from "@vitarish/account-web";

interface PaletteContextValue {
  palette: AccountThemeValue;
  setPalette: (palette: AccountThemeValue) => void;
}

const PaletteContext = createContext<PaletteContextValue | undefined>(undefined);

export function PaletteProvider({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const { resolvedTheme } = useTheme();
  const [palette, setPalette] = useState<AccountThemeValue>("default");
  const isLandingPage = pathname === "/";

  useEffect(() => {
    if (isLandingPage) {
      setPalette("glass");
      return;
    }

    const storedPalette = window.localStorage.getItem(paletteStorageKey);
    setPalette(decodeAccountThemePreference(storedPalette).theme);

    const handleStorage = (event: StorageEvent) => {
      if (event.key === paletteStorageKey) {
        setPalette(decodeAccountThemePreference(event.newValue).theme);
      }
    };

    window.addEventListener("storage", handleStorage);
    return () => window.removeEventListener("storage", handleStorage);
  }, [isLandingPage]);

  useEffect(() => {
    if (!resolvedTheme) return;

    setPaletteAttribute(
      palette,
      resolvedTheme === "dark" ? "dark" : "light",
      document.documentElement
    );
  }, [palette, resolvedTheme]);

  return (
    <PaletteContext.Provider value={{ palette, setPalette }}>
      {children}
    </PaletteContext.Provider>
  );
}

export function usePalette() {
  const context = useContext(PaletteContext);

  if (!context) {
    throw new Error("usePalette must be used within a PaletteProvider");
  }

  return context;
}
