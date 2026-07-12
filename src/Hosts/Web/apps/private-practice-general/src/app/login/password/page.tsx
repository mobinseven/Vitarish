"use client";

import { LoginForm, LoginPageCard } from "@vitarish/auth-web";
import { useLocalizedText } from "@/localization";
import { Routes } from "@/app/routes";

export default function PasswordLoginPage() {
  const { t } = useLocalizedText();

  return (
    <LoginPageCard
      title={t("auth.login.shortTitle")}
      subtitle={t("auth.login.credentialsSubtitle")}
      skeletonHeight="184px"
      backHref={Routes.Login.Index}
    >
      <LoginForm />
    </LoginPageCard>
  );
}
