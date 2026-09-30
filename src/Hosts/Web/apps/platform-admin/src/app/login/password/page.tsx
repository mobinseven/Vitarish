"use client";

import { useCallback } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { LoginForm, LoginPageCard } from "@vitarish/auth";
import { useLocalizedText } from "@vitarish/localization";
import { Routes } from "@/app/routes";

export default function PlatformPasswordLoginPage() {
  const { t } = useLocalizedText();
  const router = useRouter();
  const searchParams = useSearchParams();
  const navigate = useCallback((href: string) => router.push(href), [router]);

  return (
    <LoginPageCard
      title={t("auth.login.shortTitle")}
      subtitle={t("auth.login.credentialsSubtitle")}
      skeletonHeight="184px"
    >
      <LoginForm
        navigate={navigate}
        returnUrl={Routes.Audit}
        returnUrlSearchParams={searchParams}
        forgotPasswordHref={Routes.LoginForgotPassword}
      />
    </LoginPageCard>
  );
}
