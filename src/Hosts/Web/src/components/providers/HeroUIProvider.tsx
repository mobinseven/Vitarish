"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { type ReactNode, useState } from "react";
import { LocaleProvider } from "@/i18n/client";
import type { SupportedCulture } from "@/i18n/cultures";

export function Providers({
  children,
  culture,
}: {
  children: ReactNode;
  culture: SupportedCulture;
}) {
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
      <LocaleProvider culture={culture}>
        <ThemeProvider attribute="class" defaultTheme="light" enableSystem>
          {children}
        </ThemeProvider>
      </LocaleProvider>
    </QueryClientProvider>
  );
}
