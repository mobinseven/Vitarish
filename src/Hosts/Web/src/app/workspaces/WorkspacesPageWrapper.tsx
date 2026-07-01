"use client";

import { AppShellWrapper } from "@/components/app-shell";

export function WorkspacesPageWrapper({ children }: { children: React.ReactNode }) {
  return <AppShellWrapper>{children}</AppShellWrapper>;
}
