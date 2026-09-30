"use client";

import { useCallback } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { LoginForm, LoginPageCard } from "@vitarish/auth";
import { useLocalizedText } from "@/localization";
import { Routes } from "@/app/routes";

export default function PasswordLoginPage() {
  const { t } = useLocalizedText();
  const router = useRouter();
  const searchParams = useSearchParams();
  const navigate = useCallback((href: string) => router.push(href), [router]);

  return (
    <LoginPageCard
      title={t("auth.login.shortTitle")}
      subtitle={t("auth.login.credentialsSubtitle")}
      skeletonHeight="184px"
      backHref={Routes.Login.Index}
    >
      <LoginForm navigate={navigate} returnUrlSearchParams={searchParams} forgotPasswordHref={Routes.Login.ForgotPassword} />
    </LoginPageCard>
  );
}
