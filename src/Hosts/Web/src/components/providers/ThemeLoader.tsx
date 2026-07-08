"use client";

import { useEffect, useRef } from "react";
import { usePalette } from "./PaletteProvider";

const themeImports: Record<string, () => Promise<unknown>> = {
  brutalism: () => import("@heroui-pro/react/themes/brutalism"),
  glass: () => import("@heroui-pro/react/themes/glass"),
  mouve: () => import("@heroui-pro/react/themes/mouve"),
};

export function ThemeLoader() {
  const { palette } = usePalette();
  const loadedRef = useRef<Set<string>>(new Set());

  useEffect(() => {
    if (palette === "default" || loadedRef.current.has(palette)) return;

    const importTheme = themeImports[palette];
    if (!importTheme) return;

    importTheme().then(() => {
      loadedRef.current.add(palette);
    });
  }, [palette]);

  return null;
}
