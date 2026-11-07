"use client";

import { useCallback, type FormEvent } from "react";
import { Input, Label, Surface, TextField } from "@heroui/react";
import { useAccountEmailChange } from "../hooks/useAccountEmailChange";
import { localizeApiError, useLocalizedText } from "@vitarish/localization";
import { HeaderIcon, StatusAlert } from "@vitarish/ui";
import { ContactActionButton, ContactCodeStep } from "./AccountContactChange";

/** Change and verify the account's email address: send a code to the new address, then enter it. */
export function AccountEmailChange({ userId }: { userId: string }) {
  const { t } = useLocalizedText();
  const { profile, step, email, code, isLoading, error, success, setEmail, setCode, setError, reset, beginEmailChange, verifyEmailChange } =
    useAccountEmailChange(userId);

  const sendCode = useCallback(
    async (event: FormEvent) => {
      event.preventDefault();
      try {
        await beginEmailChange(email);
      } catch (failure: unknown) {
        setError(localizeApiError(failure, t));
      }
    },
    [beginEmailChange, email, setError, t],
  );

  const verifyCode = useCallback(
    async (event: FormEvent) => {
      event.preventDefault();
      try {
        await verifyEmailChange(code);
      } catch (failure: unknown) {
        setError(localizeApiError(failure, t));
      }
    },
    [code, setError, t, verifyEmailChange],
  );

  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex items-center gap-3">
        <HeaderIcon icon="envelope" />
        <h2 className="text-lg font-semibold text-foreground">{t("account.email.title")}</h2>
      </div>
      {profile?.email ? <p className="text-sm text-muted">{t("account.email.currentValue", { value: profile.email })}</p> : null}
      {error ? <StatusAlert message={error} /> : null}
      {success ? <StatusAlert message={t("account.email.updateSucceeded")} type="success" /> : null}
      {step === "email" ? (
        <form onSubmit={sendCode} className="flex flex-col gap-4">
          <TextField name="email" type="email" isRequired>
            <Label>{t("account.email.newEmail")}</Label>
            <Input value={email} onChange={(event) => setEmail(event.target.value)} autoComplete="email" />
          </TextField>
          <ContactActionButton type="submit" icon="paper-plane" label={t("auth.otp.sendAction")} isPending={isLoading} />
        </form>
      ) : (
        <ContactCodeStep
          destination={email}
          code={code}
          isLoading={isLoading}
          differentLabel={t("auth.otp.differentEmailAction")}
          onCodeChange={setCode}
          onSubmit={verifyCode}
          onReset={reset}
        />
      )}
    </Surface>
  );
}
