"use client";

import { AppShellWrapper } from "@/components/app-shell";

export function HomePageWrapper({ children }: { children: React.ReactNode }) {
  return <AppShellWrapper>{children}</AppShellWrapper>;
}
