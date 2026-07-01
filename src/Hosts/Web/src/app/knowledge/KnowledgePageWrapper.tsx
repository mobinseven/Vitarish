"use client";

import { AppShellWrapper } from "@/components/app-shell";

export function KnowledgePageWrapper({ children }: { children: React.ReactNode }) {
  return <AppShellWrapper>{children}</AppShellWrapper>;
}
