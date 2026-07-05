"use client";

import { useState, useCallback } from "react";
import { Card, TextField, Label, Input, Button, Link } from "@heroui/react";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { Routes } from "@/app/routes";
import { useAccountPasswordChange } from "@/features/account/hooks/useAccountPasswordChange";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { localizeApiError, useLocalizedText } from "@/localization";

export default function PasswordChangePage() {
  useAuthSession();
  const { t } = useLocalizedText();
  const {
    isLoading,
    error,
    setError,
    success,
    setSuccess,
    changePassword,
  } = useAccountPasswordChange();

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const handleSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      setError(null);
      setSuccess(false);

      if (newPassword !== confirmPassword) {
        setError(t("account.password.mismatch"));
        return;
      }

      if (newPassword.length < 6) {
        setError(t("account.password.tooShort"));
        return;
      }

      try {
        await changePassword(currentPassword, newPassword);
        setCurrentPassword("");
        setNewPassword("");
        setConfirmPassword("");
      } catch (err: unknown) {
        setError(localizeApiError(err, t));
      }
    },
    [currentPassword, newPassword, confirmPassword, changePassword, setError, setSuccess, t]
  );

  return (
    <div className="max-w-md mx-auto">
      <Card>
        <Card.Header>
          <h1 className="text-2xl font-bold">{t("account.password.title")}</h1>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          {error && <StatusMessage message={error} />}
          {success && <StatusMessage message={t("account.password.changeSucceeded")} type="success" />}
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <TextField name="currentPassword" type="password">
              <Label>{t("account.password.currentPassword")}</Label>
              <Input
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                autoComplete="current-password"
              />
            </TextField>
            <TextField name="newPassword" type="password" isRequired>
              <Label>{t("account.password.newPassword")}</Label>
              <Input
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                autoComplete="new-password"
              />
            </TextField>
            <TextField name="confirmPassword" type="password" isRequired>
              <Label>{t("account.password.confirmNewPassword")}</Label>
              <Input
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                autoComplete="new-password"
              />
            </TextField>
            <Button
              type="submit"
              isPending={isLoading}
              className="w-full"
            >
              {t("account.password.changeAction")}
            </Button>
          </form>
          <Link href={Routes.Account.Index} className="text-sm">
            {t("common.actions.backToSettings")}
          </Link>
        </Card.Content>
      </Card>
    </div>
  );
}
