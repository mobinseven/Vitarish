"use client";

import { Card, Button } from "@heroui/react";
import { useLogout } from "@/hooks/auth/useLogout";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { useLocalizedText } from "@/localization";

export default function LogoutPage() {
  const { logout, isLoading, isContextLoading, contextStatus, errorMessage } = useLogout();
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
            <StatusMessage message={t("auth.logout.alreadyEnded")} />
          )}
          {contextStatus === "unavailable" && (
            <StatusMessage message={t("auth.logout.unavailable")} />
          )}
          {errorMessage && <StatusMessage message={errorMessage} />}
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
