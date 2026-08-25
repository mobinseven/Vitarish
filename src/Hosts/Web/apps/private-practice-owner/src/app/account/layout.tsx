"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuthSession } from "@vitarish/auth";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { DashboardShell } from "@/components/shell";
import {
  dashboardFooterItems,
  mainDashboardNavGroups,
} from "@/components/shell/dashboardShellNavigation";
import { Routes } from "@/app/routes";
import { LoadingScreen } from "@/components/loading/LoadingScreen";
import { ContainedPage } from "@vitarish/ui/layout";

export default function AccountLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const { isAuthenticated, isLoading } = useAuthSession();
  const router = useRouter();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.push(Routes.Login.Index);
    }
  }, [isLoading, isAuthenticated, router]);

  if (isLoading) {
    return <LoadingScreen />;
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
