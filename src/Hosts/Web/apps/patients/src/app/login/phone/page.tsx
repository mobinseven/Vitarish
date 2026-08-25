"use client";

import { useCallback } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { LoginPageCard, PhoneOtpForm } from "@vitarish/auth";
import { useLocalizedText } from "@/localization";
import { Routes } from "@/app/routes";

export default function PhoneLoginPage() {
  const { t } = useLocalizedText();
  const router = useRouter();
  const searchParams = useSearchParams();
  const navigate = useCallback((href: string) => router.push(href), [router]);

  return (
    <LoginPageCard
      title={t("auth.otp.phoneTitle")}
      subtitle={t("auth.otp.phoneSubtitle")}
      skeletonHeight="112px"
      backHref={Routes.Login.Index}
    >
      <PhoneOtpForm navigate={navigate} returnUrlSearchParams={searchParams} />
    </LoginPageCard>
  );
}
