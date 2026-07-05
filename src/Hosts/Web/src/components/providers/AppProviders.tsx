"use client";

import { Toast } from "@heroui/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { type ReactNode, useState } from "react";
import { LocalizationProvider, type SupportedLocale } from "@/localization";

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
      <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
        <LocalizationProvider locale={locale}>
          {children}
          <Toast.Provider placement="bottom" />
        </LocalizationProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
