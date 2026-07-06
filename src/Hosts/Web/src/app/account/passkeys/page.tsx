"use client";

import { useCallback, useState } from "react";
import { Button } from "@heroui/react";
import { ListView } from "@heroui-pro/react";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { AccountBackLink, AccountSurface, AccountWidget } from "@/components/account/AccountSurface";
import { useAccountPasskeys } from "@/features/account/hooks/useAccountPasskeys";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { localizeApiError, useLocalizedText } from "@/localization";

export default function PasskeysPage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const { t } = useLocalizedText();
  const {
    credentials,
    isLoadingCredentials,
    isSupported,
    isRegisterPending,
    isDeletePending,
    register,
    remove,
  } = useAccountPasskeys(userId);

  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const handleRegister = useCallback(async () => {
    setError(null);
    setSuccess(null);
    try {
      await register();
      setSuccess(t("account.passkeys.registered"));
    } catch (err: unknown) {
      if (err instanceof Error && err.name === "NotAllowedError") {
        setError(t("account.passkeys.registrationCancelled"));
      } else {
        setError(localizeApiError(err, t));
      }
    }
  }, [register, t]);

  const handleDelete = useCallback(
    async (credentialId: string) => {
      setError(null);
      setSuccess(null);
      try {
        await remove(credentialId);
        setSuccess(t("account.passkeys.deleted"));
      } catch (err: unknown) {
        setError(localizeApiError(err, t));
      }
    },
    [remove, t]
  );

  return (
    <AccountSurface>
      <AccountWidget title={t("account.passkeys.title")}>
        {error && <StatusMessage message={error} />}
        {success && <StatusMessage message={success} type="success" />}

        {isSupported === false && (
          <StatusMessage message={t("account.passkeys.unsupported")} />
        )}

        {isLoadingCredentials ? (
          <p className="text-sm text-muted">{t("account.passkeys.loading")}</p>
        ) : credentials && credentials.length > 0 ? (
          <ListView aria-label={t("account.passkeys.title")}>
            {credentials.map((cred) => (
              <ListView.Item
                key={cred.id ?? `cred-${cred.friendlyName ?? "unknown"}`}
                id={cred.id ?? `cred-${cred.friendlyName ?? "unknown"}`}
                textValue={cred.friendlyName ?? t("account.passkeys.unnamed")}
              >
                <ListView.ItemContent>
                  <ListView.Title className="whitespace-normal break-words">
                    {cred.friendlyName ?? t("account.passkeys.unnamed")}
                  </ListView.Title>
                </ListView.ItemContent>
                <ListView.ItemAction>
                  <Button
                    variant="danger"
                    size="sm"
                    onPress={() => cred.id && handleDelete(cred.id)}
                    isPending={isDeletePending}
                    isDisabled={!cred.id}
                  >
                    {t("common.actions.delete")}
                  </Button>
                </ListView.ItemAction>
              </ListView.Item>
            ))}
          </ListView>
        ) : (
          <p className="text-sm text-muted">{t("account.passkeys.empty")}</p>
        )}

        {isSupported && (
          <Button
            onPress={handleRegister}
            isPending={isRegisterPending}
            className="w-full"
          >
            {t("account.passkeys.registerAction")}
          </Button>
        )}

        <AccountBackLink />
      </AccountWidget>
    </AccountSurface>
  );
}
