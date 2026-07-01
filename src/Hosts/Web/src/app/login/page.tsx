"use client";

import { Card } from "@heroui/react";
import { AuthMethodSelector } from "@/components/auth/AuthMethodSelector";
import { useLocalizedText } from "@/localization";

export default function LoginPage() {
  const { t } = useLocalizedText();

  return (
    <main className="grid min-h-screen place-items-center px-4">
      <Card className="w-full max-w-md">
        <Card.Header className="flex flex-col items-start gap-1">
          <h1 className="text-2xl font-bold">{t("auth.login.brandTitle")}</h1>
          <p className="text-sm text-default-500">{t("auth.login.accountSubtitle")}</p>
        </Card.Header>
        <Card.Content>
          <AuthMethodSelector />
        </Card.Content>
      </Card>
    </main>
  );
}
