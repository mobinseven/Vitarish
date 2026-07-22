"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuthSession } from "@vitarish/auth-web";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { DashboardShell } from "@/components/shell";
import {
  dashboardFooterItems,
  mainDashboardNavGroups,
} from "@/components/shell/dashboardShellNavigation";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";
import { ContainedPage, StandaloneCenteredPage } from "@vitarish/web-ui/layout";

export default function AccountLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const { isAuthenticated, isLoading } = useAuthSession();
  const router = useRouter();
  const { t } = useLocalizedText();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.push(Routes.Login.Index);
    }
  }, [isLoading, isAuthenticated, router]);

  if (isLoading) {
    return (
      <StandaloneCenteredPage>
        <p className="text-muted">{t("common.states.loading")}</p>
      </StandaloneCenteredPage>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  return (
    <DashboardShell
      footerItems={dashboardFooterItems}
      navGroups={mainDashboardNavGroups}
    >
      <ContainedPage>
        <JourneyGateBoundary>{children}</JourneyGateBoundary>
      </ContainedPage>
    </DashboardShell>
  );
}
