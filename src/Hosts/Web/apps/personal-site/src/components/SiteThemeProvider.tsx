"use client";

import { ThemeProvider } from "next-themes";
import type { ReactNode } from "react";
import { themeModeAttribute } from "@vitarish/ui/theme-palette";

export function SiteThemeProvider({ children }: { children: ReactNode }) {
  return (
    <ThemeProvider attribute={themeModeAttribute} defaultTheme="system" enableSystem disableTransitionOnChange>
      {children}
    </ThemeProvider>
  );
}
