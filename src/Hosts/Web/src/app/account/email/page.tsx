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

export default function EmailChangePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const { data: profile } = useUsersGetProfile(userId);
  const { t } = useLocalizedText();
  const [step, setStep] = useState<"email" | "code">("email");
  const [email, setEmail] = useState("");
  const [code, setCode] = useState("");
  const [challengeId, setChallengeId] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const handleEmailSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      setError(null);
      setIsLoading(true);
      try {
        const result = await apiFetch<{ challengeId: string }>(
          "/api/modules/authentication/account/email/begin",
          {
            method: "POST",
            body: JSON.stringify({ email }),
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
    [email, t]
  );

  const handleCodeSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      setError(null);
      setIsLoading(true);
      try {
        await apiFetch<{ succeeded: boolean }>(
          "/api/modules/authentication/account/email/complete",
          {
            method: "POST",
            body: JSON.stringify({ challengeId, code }),
          }
        );
        setSuccess(true);
        setStep("email");
        setEmail("");
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
          <h1 className="text-2xl font-bold">{t("account.email.title")}</h1>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          {profile?.email && (
            <p className="text-sm text-default-500">
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
              <p className="text-sm text-default-500">
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
                  setStep("email");
                  setCode("");
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
