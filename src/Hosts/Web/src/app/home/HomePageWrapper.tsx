"use client";

import { DashboardShell } from "@/components/shell";
import { dashboardFooterItems, homeDashboardNavGroups } from "@/components/shell/dashboardShellNavigation";

export function HomePageWrapper({ children }: { children: React.ReactNode }) {
  return (
    <DashboardShell footerItems={dashboardFooterItems} navGroups={homeDashboardNavGroups}>
      {children}
    </DashboardShell>
  );
}
