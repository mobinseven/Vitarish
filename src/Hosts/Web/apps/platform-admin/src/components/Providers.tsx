"use client";

import { usePathname } from "next/navigation";
import { NavigationTransitionReadiness } from "@vitarish/auth/navigation-readiness";
import { AuthenticationWebRoutes } from "@vitarish/auth/routes";

import { themeModeAttribute, themePreferenceStorageKey } from "@vitarish/ui/theme-mode";

import { Toast } from "@heroui/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { type ReactNode, useState } from "react";
import { MockApiBootstrap } from "@vitarish/mock-runtime";

export function Providers({ children, nonce }: { children: ReactNode; nonce?: string }) {
  const pathname = usePathname();
  const [queryClient] = useState(() => new QueryClient({ defaultOptions: { queries: { retry: 1, staleTime: 300_000 } } }));
  return <MockApiBootstrap><QueryClientProvider client={queryClient}>
    <ThemeProvider storageKey={themePreferenceStorageKey} attribute={themeModeAttribute} defaultTheme="system" enableSystem disableTransitionOnChange nonce={nonce}>
        <NavigationTransitionReadiness sessionEnabled={pathname !== AuthenticationWebRoutes.Handoff} />
      {children}
      <Toast.Provider placement="bottom" />
    </ThemeProvider>
  </QueryClientProvider></MockApiBootstrap>;
}
