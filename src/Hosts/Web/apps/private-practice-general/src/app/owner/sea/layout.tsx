"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { AuthorizationGuard } from "@vitarish/auth-web";
import { ContainedPage } from "@vitarish/web-ui/layout";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { DashboardShell } from "@/components/shell";
import {
  dashboardFooterItems,
  mainDashboardNavGroups,
} from "@/components/shell/dashboardShellNavigation";
import { Routes } from "@/app/routes";

export default function SeaLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const navigate = useCallback((href: string) => router.push(href), [router]);

  return (
    <AuthorizationGuard
      requiredRole="owner"
      loginHref={Routes.Login.Index}
      onNavigate={navigate}
    >
      <DashboardShell
        footerItems={dashboardFooterItems}
        navGroups={mainDashboardNavGroups}
      >
        <ContainedPage width="content">
          <JourneyGateBoundary>{children}</JourneyGateBoundary>
        </ContainedPage>
      </DashboardShell>
    </AuthorizationGuard>
  );
}
