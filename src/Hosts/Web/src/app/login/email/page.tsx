"use client";

import { LoginPageCard } from "@/components/auth/LoginPageCard";
import { EmailOtpForm } from "@/components/auth/EmailOtpForm";
import { useLocalizedText } from "@/localization";

export default function EmailLoginPage() {
  const { t } = useLocalizedText();

  return (
    <LoginPageCard
      title={t("auth.otp.emailTitle")}
      subtitle={t("auth.otp.emailSubtitle")}
      skeletonHeight="112px"
    >
      <EmailOtpForm />
    </LoginPageCard>
  );
}
