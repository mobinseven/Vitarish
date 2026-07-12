"use client";

import { AuthorizationGuard } from "@vitarish/auth-web";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { Routes } from "@/app/routes";
import { DashboardShell } from "@/components/shell";
import { clinicUserDashboardNavGroups, dashboardFooterItems } from "@/components/shell/dashboardShellNavigation";

export default function ClinicUserLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <AuthorizationGuard requiredRole="user" loginHref={Routes.Login.Index}>
      <DashboardShell footerItems={dashboardFooterItems} navGroups={clinicUserDashboardNavGroups}>
        <div className="mx-auto flex max-w-7xl flex-col gap-4 px-5 pb-10 pt-4">
          <JourneyGateBoundary>{children}</JourneyGateBoundary>
        </div>
      </DashboardShell>
    </AuthorizationGuard>
  );
}
