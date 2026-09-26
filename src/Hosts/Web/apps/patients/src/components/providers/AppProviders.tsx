"use client";

import { Toast } from "@heroui/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { type ReactNode, useState } from "react";
import { MockApiBootstrap } from "@vitarish/mock-runtime";

const themeModeOptions = ["light", "dark"] as const;

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
        attribute="data-theme"
        defaultTheme="system"
        themes={[...themeModeOptions]}
        // value={{ light: "glass-light", dark: "glass-dark" }}
        enableSystem
        disableTransitionOnChange
        nonce={nonce}
      >
        {children}
        <Toast.Provider placement="bottom" />
      </ThemeProvider>
    </QueryClientProvider></MockApiBootstrap>
  );
}
