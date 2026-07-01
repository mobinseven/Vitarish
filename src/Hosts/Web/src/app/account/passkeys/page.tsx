"use client";

import { useState, useCallback, useEffect } from "react";
import { Card, Button, Link } from "@heroui/react";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { Routes } from "@/app/routes";
import {
  useUsersListWebAuthnCredentials,
  useUsersBeginWebAuthnRegister,
  useUsersCompleteWebAuthnRegister,
  useUsersDeleteWebAuthnCredential,
} from "@/api/generated/users/users";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { startRegistration } from "@/lib/webauthn";
import { localizeApiError, useLocalizedText } from "@/localization";

export default function PasskeysPage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const { data: credentials, isLoading: isLoadingCredentials } =
    useUsersListWebAuthnCredentials(userId);
  const beginRegister = useUsersBeginWebAuthnRegister();
  const completeRegister = useUsersCompleteWebAuthnRegister();
  const deleteCredential = useUsersDeleteWebAuthnCredential();
  const { t } = useLocalizedText();

  const [isSupported, setIsSupported] = useState<boolean | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    setIsSupported(
      typeof window !== "undefined" &&
        window.PublicKeyCredential !== undefined
    );
  }, []);

  const handleRegister = useCallback(async () => {
    setError(null);
    setSuccess(null);
    try {
      const optionsJson = await beginRegister.mutateAsync({ userId });
      const options = JSON.parse(optionsJson);
      const credential = await startRegistration(options);
      await completeRegister.mutateAsync({ userId, data: { attestationResponseJson: JSON.stringify(credential) } });
      setSuccess(t("account.passkeys.registered"));
    } catch (err: unknown) {
      if (err instanceof Error && err.name === "NotAllowedError") {
        setError(t("account.passkeys.registrationCancelled"));
      } else {
        setError(localizeApiError(err, t));
      }
    }
  }, [beginRegister, completeRegister, userId, t]);

  const handleDelete = useCallback(
    async (credentialId: string) => {
      setError(null);
      setSuccess(null);
      try {
        await deleteCredential.mutateAsync({ userId, credentialId });
        setSuccess(t("account.passkeys.deleted"));
      } catch (err: unknown) {
        setError(localizeApiError(err, t));
      }
    },
    [deleteCredential, userId, t]
  );

  return (
    <div className="max-w-md mx-auto">
      <Card>
        <Card.Header>
          <h1 className="text-2xl font-bold">{t("account.passkeys.title")}</h1>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          {error && <StatusMessage message={error} />}
          {success && <StatusMessage message={success} type="success" />}

          {isSupported === false && (
            <StatusMessage message={t("account.passkeys.unsupported")} />
          )}

          {isLoadingCredentials ? (
            <p className="text-sm text-default-500">{t("account.passkeys.loading")}</p>
          ) : credentials && credentials.length > 0 ? (
            <div className="flex flex-col gap-2">
              {credentials.map((cred) => (
                <div
                  key={cred.id ?? `cred-${cred.friendlyName ?? "unknown"}`}
                  className="flex items-center justify-between p-3 border border-default-200 rounded-lg"
                >
                  <span className="text-sm">
                    {cred.friendlyName ?? t("account.passkeys.unnamed")}
                  </span>
                  <Button
                    variant="danger"
                    size="sm"
                    onPress={() => cred.id && handleDelete(cred.id)}
                    isPending={deleteCredential.isPending}
                  >
                    {t("common.actions.delete")}
                  </Button>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-sm text-default-500">{t("account.passkeys.empty")}</p>
          )}

          {isSupported && (
            <Button
              onPress={handleRegister}
              isPending={beginRegister.isPending || completeRegister.isPending}
              className="w-full"
            >
              {t("account.passkeys.registerAction")}
            </Button>
          )}

          <Link href={Routes.Account.Index} className="text-sm">
            {t("common.actions.backToSettings")}
          </Link>
        </Card.Content>
      </Card>
    </div>
  );
}
