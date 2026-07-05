"use client";

import { AuthorizationGuard } from "@/components/auth/AuthorizationGuard";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { DashboardShell } from "@/components/shell";
import { clinicUserDashboardNavItems, dashboardFooterItems } from "@/components/shell/dashboardShellNavigation";

export default function ClinicUserLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <AuthorizationGuard requiredRole="user">
      <DashboardShell footerItems={dashboardFooterItems} navItems={clinicUserDashboardNavItems}>
        <div className="mx-auto flex max-w-7xl flex-col gap-4 px-5 pb-10 pt-4">
          <JourneyGateBoundary>{children}</JourneyGateBoundary>
        </div>
      </DashboardShell>
    </AuthorizationGuard>
  );
}
