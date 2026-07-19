"use client";

import { useRouter } from "next/navigation";
import { Routes } from "@/app/routes";
import { useAuthSession } from "@vitarish/auth-web";
import { useLocalizedText } from "@/localization";
import { LandingPage } from "@/features/landing/landing-page";
import { StandaloneCenteredPage } from "@vitarish/web-ui/layout";

export default function RootPage() {
  const router = useRouter();
  const { isAuthenticated, isLoading } = useAuthSession();
  const { t } = useLocalizedText();

  if (isLoading) {
    return (
      <StandaloneCenteredPage>
        <p className="text-muted">{t("common.states.loading")}</p>
      </StandaloneCenteredPage>
    );
  }

  if (isAuthenticated) {
    router.replace(Routes.Clinic.Index);
    return (
      <StandaloneCenteredPage>
        <p className="text-muted">{t("common.states.redirecting")}</p>
      </StandaloneCenteredPage>
    );
  }

  return <LandingPage />;
}
