"use client";

import { LoginPageCard } from "@/components/auth/LoginPageCard";
import { LoginForm } from "@/components/auth/LoginForm";
import { useLocalizedText } from "@/localization";

export default function PasswordLoginPage() {
  const { t } = useLocalizedText();

  return (
    <LoginPageCard
      title={t("auth.login.shortTitle")}
      subtitle={t("auth.login.credentialsSubtitle")}
      skeletonHeight="184px"
    >
      <LoginForm />
    </LoginPageCard>
  );
}
