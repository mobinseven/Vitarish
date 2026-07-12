"use client";

import { Card } from "@heroui/react";
import { AuthMethodSelector } from "@vitarish/auth-web";
import { useLocalizedText, type MessageKey } from "@/localization";
import { Routes } from "@/app/routes";

const authMethods = [
  { id: "password", labelKey: "auth.login.passwordMethod" as MessageKey, href: Routes.Login.Password, iconClassName: "fa-solid fa-lock" },
  { id: "phone", labelKey: "auth.login.phoneMethod" as MessageKey, href: Routes.Login.Phone, iconClassName: "fa-solid fa-mobile-screen-button" },
  { id: "email", labelKey: "auth.login.emailMethod" as MessageKey, href: Routes.Login.Email, iconClassName: "fa-solid fa-envelope" },
  { id: "passkey", labelKey: "auth.login.passkeyMethod" as MessageKey, href: Routes.Login.Passkey, iconClassName: "fa-solid fa-key" },
];

export default function LoginPage() {
  const { t } = useLocalizedText();

  return (
    <main className="grid min-h-screen place-items-center px-4">
      <Card className="w-full max-w-md">
        <Card.Header className="flex flex-col items-start gap-1">
          <h1 className="text-2xl font-bold">{t("auth.login.brandTitle")}</h1>
          <p className="text-sm text-muted">{t("auth.login.accountSubtitle")}</p>
        </Card.Header>
        <Card.Content>
          <AuthMethodSelector methods={authMethods} />
        </Card.Content>
      </Card>
    </main>
  );
}
