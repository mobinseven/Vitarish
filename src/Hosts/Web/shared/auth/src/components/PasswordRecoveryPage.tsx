"use client";

import { useLocalizedText } from "@vitarish/localization";
import { LoginPageCard } from "./LoginPageCard";
import { PasswordRecoveryForm } from "./PasswordRecoveryForm";

export function PasswordRecoveryPage({ loginHref }: { loginHref: string }) {
  const { t } = useLocalizedText();

  return (
    <LoginPageCard
      title={t("auth.recovery.title")}
      subtitle={t("auth.recovery.subtitle")}
      backHref={loginHref}
      backLabel={t("auth.recovery.backToLogin")}
      skeletonHeight="280px"
    >
      <PasswordRecoveryForm />
    </LoginPageCard>
  );
}
