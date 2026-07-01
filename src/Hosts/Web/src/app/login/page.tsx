"use client";

import { CenteredSurface } from "@/design-system";
import { AuthMethodSelector } from "@/components/auth/AuthMethodSelector";
import { useLocalizedText } from "@/localization";

export default function LoginPage() {
  const { t } = useLocalizedText();

  return (
    <CenteredSurface title={t("auth.login.brandTitle")} description={t("auth.login.accountSubtitle")}>
      <AuthMethodSelector />
    </CenteredSurface>
  );
}
