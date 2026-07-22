"use client";

import { DashboardShell } from "@/components/shell";
import {
  dashboardFooterItems,
  mainDashboardNavGroups,
} from "@/components/shell/dashboardShellNavigation";
import { ContainedPage } from "@vitarish/web-ui/layout";

export function WorkspacesPageWrapper({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <DashboardShell
      footerItems={dashboardFooterItems}
      navGroups={mainDashboardNavGroups}
    >
      <ContainedPage>{children}</ContainedPage>
    </DashboardShell>
  );
}
