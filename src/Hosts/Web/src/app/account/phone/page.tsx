"use client";

import { useState, useCallback } from "react";
import { Card, TextField, Label, Input, Button, Link } from "@heroui/react";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { Routes } from "@/app/routes";
import { useUsersGetProfile } from "@/api/generated/users/users";
import { apiFetch } from "@/api/client/apiFetch";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { OtpCodeInput } from "@/components/auth/OtpCodeInput";
import { localizeApiError, useLocalizedText } from "@/localization";

export default function PhoneChangePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const { data: profile } = useUsersGetProfile(userId);
  const { t } = useLocalizedText();
  const [step, setStep] = useState<"phone" | "code">("phone");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [code, setCode] = useState("");
  const [challengeId, setChallengeId] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const handlePhoneSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      setError(null);
      setIsLoading(true);
      try {
        const result = await apiFetch<{ challengeId: string }>(
          "/api/modules/authentication/account/phone/begin",
          {
            method: "POST",
            body: JSON.stringify({ phoneNumber }),
          }
        );
        setChallengeId(result.challengeId);
        setStep("code");
      } catch (err: unknown) {
        setError(localizeApiError(err, t));
      } finally {
        setIsLoading(false);
      }
    },
    [phoneNumber, t]
  );

  const handleCodeSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      setError(null);
      setIsLoading(true);
      try {
        await apiFetch<{ succeeded: boolean }>(
          "/api/modules/authentication/account/phone/complete",
          {
            method: "POST",
            body: JSON.stringify({ challengeId, code }),
          }
        );
        setSuccess(true);
        setStep("phone");
        setPhoneNumber("");
        setCode("");
      } catch (err: unknown) {
        setError(localizeApiError(err, t));
      } finally {
        setIsLoading(false);
      }
    },
    [challengeId, code, t]
  );

  return (
    <div className="max-w-md mx-auto">
      <Card>
        <Card.Header>
          <h1 className="text-2xl font-bold">{t("account.phone.title")}</h1>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          {profile?.phoneNumber && (
            <p className="text-sm text-default-500">
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
              <p className="text-sm text-default-500">
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
                  setStep("phone");
                  setCode("");
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
