"use client";

import { LoginPageCard, PasskeyButton } from "@vitarish/auth-web";
import { useLocalizedText } from "@/localization";
import { Routes } from "@/app/routes";

export default function PasskeyLoginPage() {
  const { t } = useLocalizedText();

  return (
    <LoginPageCard
      title={t("auth.passkey.title")}
      subtitle={t("auth.passkey.subtitle")}
      skeletonHeight="128px"
      backHref={Routes.Login.Index}
    >
      <PasskeyButton />
    </LoginPageCard>
  );
}
