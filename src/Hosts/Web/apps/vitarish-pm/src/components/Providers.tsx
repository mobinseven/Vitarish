"use client";

import { Toast } from "@heroui/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { type ReactNode, useState } from "react";
import { MockApiBootstrap } from "@vitarish/mock-runtime";
import { themeModeAttribute } from "@vitarish/ui/theme-palette";
import { richSeaHandlers } from "./richSeaHandlers";

export function Providers({ children, nonce }: { children: ReactNode; nonce?: string }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: { queries: { staleTime: 300_000, retry: 1 } },
      }),
  );
  const handlers =
    process.env.NEXT_PUBLIC_VITARISH_MOCK_SCENARIO === "rich"
      ? richSeaHandlers
      : [];
  return (
    <MockApiBootstrap handlers={handlers}>
      <QueryClientProvider client={queryClient}>
        <ThemeProvider
          attribute={themeModeAttribute}
          defaultTheme="system"
          enableSystem
          disableTransitionOnChange
          nonce={nonce}
        >
          {children}
          <Toast.Provider placement="bottom" />
        </ThemeProvider>
      </QueryClientProvider>
    </MockApiBootstrap>
  );
}
