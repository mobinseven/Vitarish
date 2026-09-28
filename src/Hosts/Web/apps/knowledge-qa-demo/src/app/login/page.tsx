"use client";

import { Suspense, useCallback } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { LoginForm, LoginPageCard } from "@vitarish/auth";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";
import { StandaloneCenteredPage } from "@vitarish/ui/layout";

function LoginFormSurface() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { t } = useLocalizedText();
  const navigate = useCallback((href: string) => router.push(href), [router]);

  return (
    <LoginPageCard
      title={t("auth.login.title")}
      subtitle={t("auth.login.subtitle")}
      backHref={Routes.Home}
      backLabel={t("auth.login.backToLanding")}
      skeletonHeight="184px"
    >
      <LoginForm
        navigate={navigate}
        returnUrl={Routes.KnowledgeQa}
        returnUrlSearchParams={searchParams}
        forgotPasswordHref={Routes.LoginForgotPassword}
      />
    </LoginPageCard>
  );
}

export default function LoginPage() {
  const { t } = useLocalizedText();

  return (
    <Suspense
      fallback={
        <StandaloneCenteredPage>
          <p className="text-muted">{t("common.states.loading")}</p>
        </StandaloneCenteredPage>
      }
    >
      <LoginFormSurface />
    </Suspense>
  );
}
