"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { Card, Button } from "@heroui/react";
import { useLogout } from "@vitarish/auth-web";
import { StatusAlert } from "@vitarish/web-ui";
import { useLocalizedText } from "@/localization";
import { Routes } from "@/app/routes";

export default function LogoutPage() {
  const router = useRouter();
  const navigate = useCallback((href: string) => router.push(href), [router]);
  const { logout, isLoading, isContextLoading, contextStatus, errorMessage } = useLogout(undefined, Routes.Login.Index, navigate);
  const { t } = useLocalizedText();

  return (
    <main className="grid min-h-screen place-items-center px-4">
      <Card className="w-full max-w-md">
        <Card.Header className="flex flex-col items-start gap-1">
          <h1 className="text-2xl font-bold">{t("auth.logout.title")}</h1>
          <p className="text-sm text-muted">{t("auth.logout.description")}</p>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          {contextStatus === "already-signed-out" && (
            <StatusAlert className="mb-4" message={t("auth.logout.alreadyEnded")} />
          )}
          {contextStatus === "unavailable" && (
            <StatusAlert className="mb-4" message={t("auth.logout.unavailable")} />
          )}
          {errorMessage && <StatusAlert className="mb-4" message={errorMessage} />}
          <Button
            variant="danger"
            isPending={isLoading || isContextLoading}
            onPress={() => logout()}
            className="w-full"
          >
            {t("auth.logout.title")}
          </Button>
          <Button
            variant="secondary"
            onPress={() => window.history.back()}
            isDisabled={isLoading || isContextLoading}
            className="w-full"
          >
            {t("common.actions.cancel")}
          </Button>
        </Card.Content>
      </Card>
    </main>
  );
}
