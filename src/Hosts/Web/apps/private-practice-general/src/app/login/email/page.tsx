"use client";

import { EmailOtpForm, LoginPageCard } from "@vitarish/auth-web";
import { useLocalizedText } from "@/localization";
import { Routes } from "@/app/routes";

export default function EmailLoginPage() {
  const { t } = useLocalizedText();

  return (
    <LoginPageCard
      title={t("auth.otp.emailTitle")}
      subtitle={t("auth.otp.emailSubtitle")}
      skeletonHeight="112px"
      backHref={Routes.Login.Index}
    >
      <EmailOtpForm />
    </LoginPageCard>
  );
}
