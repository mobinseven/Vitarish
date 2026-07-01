"use client";

import { Button } from "@heroui/react";
import { CenteredSurface } from "@/design-system";
import { useLogout } from "@/hooks/auth/useLogout";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { useLocalizedText } from "@/localization";

export default function LogoutPage() {
  const { logout, isLoading, isContextLoading, contextStatus, errorMessage } = useLogout();
  const { t } = useLocalizedText();

  return (
    <CenteredSurface title={t("auth.logout.title")} description={t("auth.logout.description")}>
      <div className="flex flex-col gap-4">
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
          variant="tertiary"
          onPress={() => window.history.back()}
          isDisabled={isLoading || isContextLoading}
          className="w-full"
        >
          {t("common.actions.cancel")}
        </Button>
      </div>
    </CenteredSurface>
  );
}
