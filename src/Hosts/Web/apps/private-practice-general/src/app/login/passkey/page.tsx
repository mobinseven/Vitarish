"use client";

import { LoginPageCard } from "@/components/auth/LoginPageCard";
import { PasskeyButton } from "@/components/auth/PasskeyButton";
import { useLocalizedText } from "@/localization";

export default function PasskeyLoginPage() {
  const { t } = useLocalizedText();

  return (
    <LoginPageCard
      title={t("auth.passkey.title")}
      subtitle={t("auth.passkey.subtitle")}
      skeletonHeight="128px"
    >
      <PasskeyButton />
    </LoginPageCard>
  );
}
