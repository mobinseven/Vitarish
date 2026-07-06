"use client";

import { Toast } from "@heroui/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { type ReactNode, useState } from "react";
import { LocalizationProvider, type SupportedLocale } from "@/localization";
import { accountThemeOptions } from "@/features/account/theme/themeOptions";
import { VibrantPaletteProvider } from "./VibrantPaletteProvider";

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
        themes={accountThemeOptions.map((option) => option.value)}
        enableSystem
        disableTransitionOnChange
      >
        <LocalizationProvider locale={locale}>
          <VibrantPaletteProvider>
            {children}
            <Toast.Provider placement="bottom" />
          </VibrantPaletteProvider>
        </LocalizationProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
