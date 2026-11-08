"use client";

import { useLocalizedText } from "@vitarish/localization";
import { LoginPageCard } from "./LoginPageCard";
import { PasswordRecoveryForm } from "./PasswordRecoveryForm";

export function PasswordRecoveryPage({ loginHref, returnUrlSearchParams }: { loginHref: string; returnUrlSearchParams?: Pick<URLSearchParams, "get"> }) {
  const { t } = useLocalizedText();

  return (
    <LoginPageCard
      title={t("auth.recovery.title")}
      subtitle={t("auth.recovery.subtitle")}
      returnUrlSearchParams={returnUrlSearchParams}
      backHref={loginHref}
      backLabel={t("auth.recovery.backToLogin")}
      skeletonHeight="280px"
    >
      <PasswordRecoveryForm />
    </LoginPageCard>
  );
}
