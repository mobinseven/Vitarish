"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuthSession } from "@vitarish/auth";
import { AuthenticationSearchParams } from "@vitarish/auth/routes";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { DashboardShell } from "@/components/shell";
import {
  dashboardFooterItems,
  mainDashboardNavGroups,
  centralAccountNavGroups,
} from "@/components/shell/dashboardShellNavigation";
import { Routes } from "@/app/routes";
import { LoadingScreen } from "@/components/loading/LoadingScreen";
import { ContainedPage } from "@vitarish/ui/layout";

export function AccountFrame({
  children,
  central,
}: {
  children: React.ReactNode;
  central: boolean;
}) {
  const { isAuthenticated, isLoading } = useAuthSession();
  const router = useRouter();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.push(`${Routes.Login.Index}?${AuthenticationSearchParams.ReturnUrl}=${encodeURIComponent(window.location.pathname + window.location.search)}`);
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
      navGroups={central ? centralAccountNavGroups : mainDashboardNavGroups}
    >
      <ContainedPage>
        <JourneyGateBoundary central={central}>{children}</JourneyGateBoundary>
      </ContainedPage>
    </DashboardShell>
  );
}
