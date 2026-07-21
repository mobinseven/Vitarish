"use client";

import { Toast } from "@heroui/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { type ReactNode, useState } from "react";

const themeModeOptions = ["light", "dark"] as const;

export function Providers({ children }: { children: ReactNode }) {
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
        // value={{ light: "glass-light", dark: "glass-dark" }}
        enableSystem
        disableTransitionOnChange
      >
        {children}
        <Toast.Provider placement="bottom" />
      </ThemeProvider>
    </QueryClientProvider>
  );
}
