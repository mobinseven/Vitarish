"use client";

import { LoginPageCard, PhoneOtpForm } from "@vitarish/auth-web";
import { useLocalizedText } from "@/localization";
import { Routes } from "@/app/routes";

export default function PhoneLoginPage() {
  const { t } = useLocalizedText();

  return (
    <LoginPageCard
      title={t("auth.otp.phoneTitle")}
      subtitle={t("auth.otp.phoneSubtitle")}
      skeletonHeight="112px"
      backHref={Routes.Login.Index}
    >
      <PhoneOtpForm />
    </LoginPageCard>
  );
}
