"use client";

import { useState, useCallback } from "react";
import { TextField, Label, Input, Button } from "@heroui/react";
import { useAuthSession } from "@vitarish/auth";
import {
  AccountBackLink,
  AccountSurface,
  AccountWidget,
} from "@/components/account/AccountSurface";
import { ApiError } from "@vitarish/api-client";
import { useAccountPasswordChange } from "@vitarish/account";
import { StatusAlert } from "@vitarish/ui";
import { localizeApiError, useLocalizedText } from "@/localization";

export default function PasswordChangePage() {
  useAuthSession();
  const { t } = useLocalizedText();
  const { isLoading, error, setError, success, setSuccess, changePassword } =
    useAccountPasswordChange();

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

      try {
        await changePassword(currentPassword, newPassword);
        setCurrentPassword("");
        setNewPassword("");
        setConfirmPassword("");
      } catch (err: unknown) {
        // The server owns the password policy; a 400 here is its policy rejection
        // (a wrong current password is a 401).
        setError(err instanceof ApiError && err.status === 400
          ? t("account.password.policyRejected")
          : localizeApiError(err, t));
      }
    },
    [
      currentPassword,
      newPassword,
      confirmPassword,
      changePassword,
      setError,
      setSuccess,
      t,
    ],
  );

  return (
    <AccountSurface>
      <AccountWidget title={t("account.password.title")}>
        {error && <StatusAlert className="mb-4" message={error} />}
        {success && (
          <StatusAlert
            className="mb-4"
            message={t("account.password.changeSucceeded")}
            type="success"
          />
        )}
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <TextField name="currentPassword" type="password" isRequired>
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
          <Button type="submit" isPending={isLoading} className="w-full">
            {t("account.password.changeAction")}
          </Button>
        </form>
        <AccountBackLink />
      </AccountWidget>
    </AccountSurface>
  );
}
