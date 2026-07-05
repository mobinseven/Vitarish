"use client";

import { useCallback } from "react";
import { Card, TextField, Label, Input, Button, Link } from "@heroui/react";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { Routes } from "@/app/routes";
import { useAccountPhoneChange } from "@/features/account/hooks/useAccountPhoneChange";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { OtpCodeInput } from "@/components/auth/OtpCodeInput";
import { localizeApiError, useLocalizedText } from "@/localization";

export default function PhoneChangePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const { t } = useLocalizedText();
  const {
    profile,
    step,
    phoneNumber,
    code,
    isLoading,
    error,
    success,
    setPhoneNumber,
    setCode,
    setError,
    reset,
    beginPhoneChange,
    verifyPhoneChange,
  } = useAccountPhoneChange(userId);

  const handlePhoneSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      try {
        await beginPhoneChange(phoneNumber);
      } catch (err: unknown) {
        setError(localizeApiError(err, t));
      }
    },
    [phoneNumber, beginPhoneChange, setError, t]
  );

  const handleCodeSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      try {
        await verifyPhoneChange(code);
      } catch (err: unknown) {
        setError(localizeApiError(err, t));
      }
    },
    [code, verifyPhoneChange, setError, t]
  );

  return (
    <div className="max-w-md mx-auto">
      <Card>
        <Card.Header>
          <h1 className="text-2xl font-bold">{t("account.phone.title")}</h1>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          {profile?.phoneNumber && (
            <p className="text-sm text-muted">
              {t("account.phone.currentValue", { value: profile.phoneNumber })}
            </p>
          )}
          {error && <StatusMessage message={error} />}
          {success && (
            <StatusMessage message={t("account.phone.updateSucceeded")} type="success" />
          )}

          {step === "phone" ? (
            <form onSubmit={handlePhoneSubmit} className="flex flex-col gap-4">
              <TextField name="phone" isRequired>
                <Label>{t("account.phone.newPhone")}</Label>
                <Input
                  value={phoneNumber}
                  onChange={(e) => setPhoneNumber(e.target.value)}
                  autoComplete="tel"
                  inputMode="tel"
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
                {t("auth.otp.codeSentTo", { destination: phoneNumber })}
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
                {t("auth.otp.differentPhoneAction")}
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
