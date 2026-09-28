"use client";

import { ThemeProvider } from "next-themes";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode, useState } from "react";
import { MockApiBootstrap } from "@vitarish/mock-runtime";

const themeModes = ["light", "dark"] as const;

export function AppProviders({ children, nonce }: { children: ReactNode; nonce?: string }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: { staleTime: 5 * 60 * 1000, retry: 1 },
        },
      }),
  );

  return (
    <MockApiBootstrap><QueryClientProvider client={queryClient}>
      <ThemeProvider
        nonce={nonce}
        attribute="data-theme"
        defaultTheme="system"
        disableTransitionOnChange
        enableSystem
        themes={[...themeModes]}
      >
        {children}
      </ThemeProvider>
    </QueryClientProvider></MockApiBootstrap>
  );
}
