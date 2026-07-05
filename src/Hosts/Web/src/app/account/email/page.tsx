"use client";

import { useCallback } from "react";
import { Card, TextField, Label, Input, Button, Link } from "@heroui/react";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { Routes } from "@/app/routes";
import { useAccountEmailChange } from "@/features/account/hooks/useAccountEmailChange";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { OtpCodeInput } from "@/components/auth/OtpCodeInput";
import { localizeApiError, useLocalizedText } from "@/localization";

export default function EmailChangePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const { t } = useLocalizedText();
  const {
    profile,
    step,
    email,
    code,
    isLoading,
    error,
    success,
    setEmail,
    setCode,
    setError,
    reset,
    beginEmailChange,
    verifyEmailChange,
  } = useAccountEmailChange(userId);

  const handleEmailSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      try {
        await beginEmailChange(email);
      } catch (err: unknown) {
        setError(localizeApiError(err, t));
      }
    },
    [email, beginEmailChange, setError, t]
  );

  const handleCodeSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      try {
        await verifyEmailChange(code);
      } catch (err: unknown) {
        setError(localizeApiError(err, t));
      }
    },
    [code, verifyEmailChange, setError, t]
  );

  return (
    <div className="max-w-md mx-auto">
      <Card>
        <Card.Header>
          <h1 className="text-2xl font-bold">{t("account.email.title")}</h1>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          {profile?.email && (
            <p className="text-sm text-muted">
              {t("account.email.currentValue", { value: profile.email })}
            </p>
          )}
          {error && <StatusMessage message={error} />}
          {success && (
            <StatusMessage message={t("account.email.updateSucceeded")} type="success" />
          )}

          {step === "email" ? (
            <form onSubmit={handleEmailSubmit} className="flex flex-col gap-4">
              <TextField name="email" type="email" isRequired>
                <Label>{t("account.email.newEmail")}</Label>
                <Input
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  autoComplete="email"
                />
              </TextField>
              <Button
                type="submit"
                isPending={isLoading}
                className="w-full"
              >
                {t("auth.otp.sendAction")}
              </Button>
            </form>
          ) : (
            <form onSubmit={handleCodeSubmit} className="flex flex-col gap-4">
              <p className="text-sm text-muted">
                {t("auth.otp.codeSentTo", { destination: email })}
              </p>
              <OtpCodeInput
                value={code}
                onChange={setCode}
                isDisabled={isLoading}
              />
              <Button
                type="submit"
                isPending={isLoading}
                isDisabled={code.length !== 6}
                className="w-full"
              >
                {t("auth.otp.verifyAction")}
              </Button>
              <Button
                variant="tertiary"
                onPress={() => {
                  reset();
                }}
                isDisabled={isLoading}
                className="w-full"
              >
                {t("auth.otp.differentEmailAction")}
              </Button>
            </form>
          )}

          <Link href={Routes.Account.Index} className="text-sm">
            {t("common.actions.backToSettings")}
          </Link>
        </Card.Content>
      </Card>
    </div>
  );
}
