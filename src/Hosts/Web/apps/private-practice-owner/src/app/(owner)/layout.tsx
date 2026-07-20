"use client";

import { useCallback } from "react";
import { usePathname, useRouter } from "next/navigation";
import { AuthorizationGuard } from "@vitarish/auth-web";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { Routes } from "@/app/routes";
import { DashboardShell } from "@/components/shell";
import {
  clinicOwnerDashboardNavGroups,
  dashboardFooterItems,
} from "@/components/shell/dashboardShellNavigation";
import {
  ContainedPage,
  ImmersiveRegion,
  PageContainer,
} from "@vitarish/web-ui/layout";
import { isClinicWorkspaceRoute } from "./clinicRouteLayout";

export default function OwnerLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const pathname = usePathname();
  const navigate = useCallback((href: string) => router.push(href), [router]);
  const isWorkspace = isClinicWorkspaceRoute(pathname);
  const gatedContent = <JourneyGateBoundary>{children}</JourneyGateBoundary>;

  return (
    <AuthorizationGuard requiredRole="owner" loginHref={Routes.Login.Index} onNavigate={navigate}>
      <DashboardShell
        contentMode={isWorkspace ? "workspace" : "page"}
        footerItems={dashboardFooterItems}
        navGroups={clinicOwnerDashboardNavGroups}
      >
        {isWorkspace ? (
          <ImmersiveRegion>
            <PageContainer
              className="flex min-h-0 flex-1 flex-col py-6 lg:py-8"
              width="content"
            >
              {gatedContent}
            </PageContainer>
          </ImmersiveRegion>
        ) : (
          <ContainedPage width="content">{gatedContent}</ContainedPage>
        )}
      </DashboardShell>
    </AuthorizationGuard>
  );
}
