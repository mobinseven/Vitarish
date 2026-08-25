"use client";

import { DashboardShell } from "@/components/shell";
import {
  dashboardFooterItems,
  mainDashboardNavGroups,
} from "@/components/shell/dashboardShellNavigation";
import { ImmersiveRegion } from "@vitarish/ui/layout";

export function KnowledgePageWrapper({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <DashboardShell
      contentMode="workspace"
      footerItems={dashboardFooterItems}
      navGroups={mainDashboardNavGroups}
    >
      <ImmersiveRegion>{children}</ImmersiveRegion>
    </DashboardShell>
  );
}
