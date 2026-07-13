"use client";

import { useAuthSession } from "@vitarish/auth-web";
import { useLocalizedText } from "@/localization";
import { PlatformAdminGuard } from "@/components/guards/PlatformAdminGuard";
import { Routes } from "@/app/routes";
import { useRouter } from "next/navigation";
import { useEffect } from "react";

export default function RootPage() {
  const router = useRouter();
  const { isAuthenticated, isLoading } = useAuthSession();
  const { t } = useLocalizedText();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace(Routes.Login.Index);
    }
  }, [isLoading, isAuthenticated, router]);

  if (isLoading || !isAuthenticated) {
    return (
      <main className="grid min-h-screen place-items-center">
        <p className="text-muted">{t("common.states.loading")}</p>
      </main>
    );
  }

  return (
    <PlatformAdminGuard>
      <div className="p-6">
        <h1 className="text-2xl font-bold">{t("platformAdmin.dashboard.title")}</h1>
      </div>
    </PlatformAdminGuard>
  );
}
