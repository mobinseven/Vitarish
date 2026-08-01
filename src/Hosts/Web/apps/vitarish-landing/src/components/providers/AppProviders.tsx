"use client";

import { ThemeProvider } from "next-themes";
import type { ReactNode } from "react";

const themeModes = ["light", "dark"] as const;

export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <ThemeProvider
      attribute="data-theme"
      defaultTheme="system"
      disableTransitionOnChange
      enableSystem
      themes={[...themeModes]}
    >
      {children}
    </ThemeProvider>
  );
}
