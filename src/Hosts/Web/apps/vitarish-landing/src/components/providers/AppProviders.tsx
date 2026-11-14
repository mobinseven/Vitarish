"use client";

import { usePathname } from "next/navigation";
import { NavigationTransitionReadiness } from "@vitarish/auth/navigation-readiness";
import { AuthenticationWebRoutes } from "@vitarish/auth/routes";

import { themePreferenceStorageKey } from "@vitarish/ui/theme-mode";

import { ThemeProvider } from "next-themes";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode, useState } from "react";
import { MockApiBootstrap } from "@vitarish/mock-runtime";
import { themeModeAttribute } from "@vitarish/ui/theme-palette";

const themeModes = ["light", "dark"] as const;

export function AppProviders({ children, nonce }: { children: ReactNode; nonce?: string }) {
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
      <ThemeProvider storageKey={themePreferenceStorageKey}
        attribute={themeModeAttribute}
        defaultTheme="system"
        disableTransitionOnChange
        enableSystem
        themes={[...themeModes]}
        nonce={nonce}
      >
        <NavigationTransitionReadiness sessionEnabled={pathname !== AuthenticationWebRoutes.Handoff} />
        {children}
      </ThemeProvider>
    </QueryClientProvider></MockApiBootstrap>
  );
}
