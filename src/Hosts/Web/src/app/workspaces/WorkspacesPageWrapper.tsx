"use client";

import { DashboardShell } from "@/components/shell";
import { dashboardFooterItems, mainDashboardNavItems } from "@/components/shell/dashboardShellNavigation";

export function WorkspacesPageWrapper({ children }: { children: React.ReactNode }) {
  return (
    <DashboardShell footerItems={dashboardFooterItems} navItems={mainDashboardNavItems}>
      {children}
    </DashboardShell>
  );
}
