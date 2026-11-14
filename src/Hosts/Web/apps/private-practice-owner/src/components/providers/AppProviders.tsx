"use client";

import { usePathname } from "next/navigation";
import { NavigationTransitionReadiness } from "@vitarish/auth/navigation-readiness";
import { AuthenticationWebRoutes } from "@vitarish/auth/routes";

import { Toast } from "@heroui/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { type ReactNode, useState } from "react";
import { MockApiBootstrap } from "@vitarish/mock-runtime";
import { themeModeAttribute } from "@vitarish/ui/theme-palette";
import { themeModeOptions, themePreferenceStorageKey } from "./themeMode";

export function Providers({ children, nonce }: { children: ReactNode; nonce?: string }) {
  const pathname = usePathname();
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
        attribute={themeModeAttribute}
        storageKey={themePreferenceStorageKey}
        defaultTheme="system"
        themes={[...themeModeOptions]}
        enableSystem
        disableTransitionOnChange
        nonce={nonce}
      >
        <NavigationTransitionReadiness sessionEnabled={pathname !== AuthenticationWebRoutes.Handoff} />
        {children}
        <Toast.Provider placement="bottom" />
      </ThemeProvider>
    </QueryClientProvider></MockApiBootstrap>
  );
}
