"use client";

import { Toast } from "@heroui/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { type ReactNode, useState } from "react";
import { I18nProvider } from "@react-aria/i18n";
import { LocalizationProvider, getReactAriaLocale, type SupportedLocale } from "@/localization";

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
        themes={["light", "dark"]}
        enableSystem
        disableTransitionOnChange
      >
        <I18nProvider locale={getReactAriaLocale(locale)}>
          <LocalizationProvider locale={locale}>
            {children}
            <Toast.Provider placement="bottom" />
          </LocalizationProvider>
        </I18nProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
