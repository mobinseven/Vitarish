"use client";

import { useRouter } from "next/navigation";
import { Routes } from "@/app/routes";
import { useAuthSession } from "@vitarish/auth-web";
import { useLocalizedText } from "@/localization";
import { LandingPage } from "@/features/landing/landing-page";

export default function RootPage() {
  const router = useRouter();
  const { isAuthenticated, isLoading } = useAuthSession();
  const { t } = useLocalizedText();

  if (isLoading) {
    return (
      <main className="grid min-h-screen place-items-center">
        <p className="text-muted">{t("common.states.loading")}</p>
      </main>
    );
  }

  if (isAuthenticated) {
    router.replace(Routes.Home);
    return (
      <main className="grid min-h-screen place-items-center">
        <p className="text-muted">{t("common.states.redirecting")}</p>
      </main>
    );
  }

  return <LandingPage />;
}
