"use client";

import { AuthorizationGuard } from "@/components/auth/AuthorizationGuard";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { DashboardShell } from "@/components/shell";
import { clinicOwnerDashboardNavGroups, dashboardFooterItems } from "@/components/shell/dashboardShellNavigation";

export default function ClinicLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <AuthorizationGuard requiredRole="owner">
      <DashboardShell footerItems={dashboardFooterItems} navGroups={clinicOwnerDashboardNavGroups}>
        <div className="mx-auto flex max-w-7xl flex-col gap-4 px-5 pb-10 pt-4">
          <JourneyGateBoundary>{children}</JourneyGateBoundary>
        </div>
      </DashboardShell>
    </AuthorizationGuard>
  );
}
