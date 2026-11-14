"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { currentReturnPath, useAuthSession } from "@vitarish/auth";
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
import { JourneyErrorState } from "@vitarish/journey";
import { navigationTransitionStates, useNavigationTransitionParticipant } from "@vitarish/ui/navigation-runtime";
import { ContainedPage } from "@vitarish/ui/layout";

export function AccountFrame({
  children,
  central,
}: {
  children: React.ReactNode;
  central: boolean;
}) {
  const session = useAuthSession();
  const { isAuthenticated, isLoading, isError } = session;
  useNavigationTransitionParticipant(isLoading || (!isAuthenticated && !isError), isError ? navigationTransitionStates.Failed : undefined);
  const router = useRouter();

  useEffect(() => {
    if (!isLoading && !isError && !isAuthenticated) {
      router.push(`${Routes.Login.Index}?${AuthenticationSearchParams.ReturnUrl}=${encodeURIComponent(currentReturnPath() ?? Routes.Root)}`);
    }
  }, [isLoading, isError, isAuthenticated, router]);

  if (isError) return <JourneyErrorState onRetry={() => void session.refetch()} />;

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
