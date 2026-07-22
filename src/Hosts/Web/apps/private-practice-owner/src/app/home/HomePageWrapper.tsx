"use client";

import { DashboardShell } from "@/components/shell";
import {
  dashboardFooterItems,
  homeDashboardNavGroups,
} from "@/components/shell/dashboardShellNavigation";
import { ContainedPage } from "@vitarish/web-ui/layout";

export function HomePageWrapper({ children }: { children: React.ReactNode }) {
  return (
    <DashboardShell
      footerItems={dashboardFooterItems}
      navGroups={homeDashboardNavGroups}
    >
      <ContainedPage>{children}</ContainedPage>
    </DashboardShell>
  );
}
