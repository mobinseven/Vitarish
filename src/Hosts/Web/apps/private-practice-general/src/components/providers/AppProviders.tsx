"use client";

import { Toast } from "@heroui/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { type ReactNode, useState } from "react";
import { I18nProvider } from "@react-aria/i18n";
import { LocalizationProvider, getReactAriaLocale, type SupportedLocale } from "@/localization";
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
        <I18nProvider locale={getReactAriaLocale(locale)}>
          <LocalizationProvider locale={locale}>
            <PaletteProvider>
              <VibrantPaletteProvider>
                {children}
                <Toast.Provider placement="bottom" />
              </VibrantPaletteProvider>
            </PaletteProvider>
          </LocalizationProvider>
        </I18nProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
