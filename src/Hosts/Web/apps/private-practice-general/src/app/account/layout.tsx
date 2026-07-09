"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { DashboardShell } from "@/components/shell";
import { dashboardFooterItems, mainDashboardNavGroups } from "@/components/shell/dashboardShellNavigation";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";

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
      <main className="grid min-h-screen place-items-center">
        <p className="text-muted">{t("common.states.loading")}</p>
      </main>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  return (
    <DashboardShell footerItems={dashboardFooterItems} navGroups={mainDashboardNavGroups}>
      <div className="mx-auto flex max-w-7xl flex-col gap-4 px-5 pb-10 pt-4">
        <JourneyGateBoundary>{children}</JourneyGateBoundary>
      </div>
    </DashboardShell>
  );
}
