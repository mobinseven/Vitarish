"use client";

import { type ReactNode, useEffect } from "react";
import {
  setVibrantPaletteAttribute,
  vibrantPaletteStorageKey,
} from "@/features/account/theme/themeOptions";

export function VibrantPaletteProvider({ children }: { children: ReactNode }) {
  useEffect(() => {
    setVibrantPaletteAttribute(
      window.localStorage.getItem(vibrantPaletteStorageKey) === "true",
      document.documentElement
    );
  }, []);

  return children;
}
