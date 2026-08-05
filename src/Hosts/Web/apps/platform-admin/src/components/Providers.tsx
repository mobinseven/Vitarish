"use client";

import { Toast } from "@heroui/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode, useState } from "react";

export function Providers({ children }: { children: ReactNode }) {
  const [queryClient] = useState(() => new QueryClient({ defaultOptions: { queries: { retry: 1, staleTime: 300_000 } } }));
  return <QueryClientProvider client={queryClient}>{children}<Toast.Provider placement="bottom" /></QueryClientProvider>;
}
