"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { type ReactNode, useState } from "react";
import { MockApiBootstrap } from "@vitarish/mock-runtime";

export function Providers({ children, nonce }: { children: ReactNode; nonce?: string }) {
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
        themes={["light", "dark"]}
        enableSystem
        disableTransitionOnChange
      >
        {children}
      </ThemeProvider>
    </QueryClientProvider></MockApiBootstrap>
  );
}
