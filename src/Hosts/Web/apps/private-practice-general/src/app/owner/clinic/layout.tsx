"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { AuthorizationGuard } from "@vitarish/auth-web";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { Routes } from "@/app/routes";
import { DashboardShell } from "@/components/shell";
import { clinicOwnerDashboardNavGroups, dashboardFooterItems } from "@/components/shell/dashboardShellNavigation";
import { ContainedPage } from "@vitarish/web-ui/layout";

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
        <ContainedPage>
          <JourneyGateBoundary>{children}</JourneyGateBoundary>
        </ContainedPage>
      </DashboardShell>
    </AuthorizationGuard>
  );
}
