"use client";

import { Card } from "@heroui/react";
import { AuthMethodSelector, useAuthenticationProfile } from "@vitarish/auth";
import { useLocalizedText, type MessageKey } from "@/localization";
import { Routes } from "@/app/routes";
import { StandaloneCenteredPage } from "@vitarish/ui/layout";

const authMethods = [
  {
    id: "password",
    labelKey: "auth.login.passwordMethod" as MessageKey,
    href: Routes.Login.Password,
    icon: "lock",
  },
  {
    id: "phone",
    labelKey: "auth.login.phoneMethod" as MessageKey,
    href: Routes.Login.Phone,
    icon: "mobile-screen-button",
  },
  {
    id: "email",
    labelKey: "auth.login.emailMethod" as MessageKey,
    href: Routes.Login.Email,
    icon: "envelope",
  },
  {
    id: "passkey",
    labelKey: "auth.login.passkeyMethod" as MessageKey,
    href: Routes.Login.Passkey,
    icon: "key",
  },
];

export default function LoginPage() {
  const { t } = useLocalizedText();
  const { data: profile } = useAuthenticationProfile();
  const enabledMethods = authMethods.filter((method) =>
    method.id === "password" ||
    (method.id === "phone" && profile?.allowPasswordlessPhoneLogin === true) ||
    (method.id === "email" && profile?.allowPasswordlessEmailLogin === true) ||
    (method.id === "passkey" && profile?.allowPasskeyLogin === true));

  return (
    <StandaloneCenteredPage>
      <Card className="w-full max-w-md">
        <Card.Header className="flex flex-col items-start gap-1">
          <h1 className="text-2xl font-bold">{t("auth.login.brandTitle")}</h1>
          <p className="text-sm text-muted">
            {t("auth.login.accountSubtitle")}
          </p>
        </Card.Header>
        <Card.Content>
          <AuthMethodSelector methods={enabledMethods} />
        </Card.Content>
      </Card>
    </StandaloneCenteredPage>
  );
}
