"use client";

import { Toast } from "@heroui/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { type ReactNode, useState } from "react";
import { LocalizationProvider, type SupportedLocale } from "@/localization";
import { type ThemeMode } from "@/features/account/theme/themeOptions";
import { PaletteProvider } from "./PaletteProvider";
import { VibrantPaletteProvider } from "./VibrantPaletteProvider";

const themeModeOptions: readonly Exclude<ThemeMode, "system">[] = ["light", "dark"];

export function Providers({ children, locale }: { children: ReactNode; locale: SupportedLocale }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: { staleTime: 5 * 60 * 1000, retry: 1 },
        },
      })
  );

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider
        attribute="data-theme"
        defaultTheme="system"
        themes={[...themeModeOptions]}
        enableSystem
        disableTransitionOnChange
      >
        <LocalizationProvider locale={locale}>
          <PaletteProvider>
            <VibrantPaletteProvider>
              {children}
              <Toast.Provider placement="bottom" />
            </VibrantPaletteProvider>
          </PaletteProvider>
        </LocalizationProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
