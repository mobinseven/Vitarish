"use client";

import { DashboardShell } from "@/components/shell";
import { dashboardFooterItems, mainDashboardNavGroups } from "@/components/shell/dashboardShellNavigation";

export function WorkspacesPageWrapper({ children }: { children: React.ReactNode }) {
  return (
    <DashboardShell footerItems={dashboardFooterItems} navGroups={mainDashboardNavGroups}>
      {children}
    </DashboardShell>
  );
}
