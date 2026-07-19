"use client";

import { useCallback } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { EmailOtpForm, LoginPageCard } from "@vitarish/auth-web";
import { useLocalizedText } from "@/localization";
import { Routes } from "@/app/routes";

export default function EmailLoginPage() {
  const { t } = useLocalizedText();
  const router = useRouter();
  const searchParams = useSearchParams();
  const navigate = useCallback((href: string) => router.push(href), [router]);

  return (
    <LoginPageCard
      title={t("auth.otp.emailTitle")}
      subtitle={t("auth.otp.emailSubtitle")}
      skeletonHeight="112px"
      backHref={Routes.Login.Index}
    >
      <EmailOtpForm navigate={navigate} returnUrlSearchParams={searchParams} />
    </LoginPageCard>
  );
}
