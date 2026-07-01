"use client";

import { LoginPageCard } from "@/components/auth/LoginPageCard";
import { PhoneOtpForm } from "@/components/auth/PhoneOtpForm";
import { useLocalizedText } from "@/localization";

export default function PhoneLoginPage() {
  const { t } = useLocalizedText();

  return (
    <LoginPageCard
      title={t("auth.otp.phoneTitle")}
      subtitle={t("auth.otp.phoneSubtitle")}
      skeletonHeight="112px"
    >
      <PhoneOtpForm />
    </LoginPageCard>
  );
}
