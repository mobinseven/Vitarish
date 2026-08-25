"use client";

import { useCallback } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { LoginPageCard, PasskeyButton } from "@vitarish/auth";
import { useLocalizedText } from "@/localization";
import { Routes } from "@/app/routes";

export default function PasskeyLoginPage() {
  const { t } = useLocalizedText();
  const router = useRouter();
  const searchParams = useSearchParams();
  const navigate = useCallback((href: string) => router.push(href), [router]);

  return (
    <LoginPageCard
      title={t("auth.passkey.title")}
      subtitle={t("auth.passkey.subtitle")}
      skeletonHeight="128px"
      backHref={Routes.Login.Index}
    >
      <PasskeyButton navigate={navigate} returnUrlSearchParams={searchParams} />
    </LoginPageCard>
  );
}
