"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { AuthorizationGuard } from "@vitarish/auth-web";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { Routes } from "@/app/routes";
import { DashboardShell } from "@/components/shell";
import { clinicOwnerDashboardNavGroups, dashboardFooterItems } from "@/components/shell/dashboardShellNavigation";

export default function ClinicLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const navigate = useCallback((href: string) => router.push(href), [router]);

  return (
    <AuthorizationGuard requiredRole="owner" loginHref={Routes.Login.Index} onNavigate={navigate}>
      <DashboardShell footerItems={dashboardFooterItems} navGroups={clinicOwnerDashboardNavGroups}>
        <div className="mx-auto flex max-w-7xl flex-col gap-4 px-5 pb-10 pt-4">
          <JourneyGateBoundary>{children}</JourneyGateBoundary>
        </div>
      </DashboardShell>
    </AuthorizationGuard>
  );
}
