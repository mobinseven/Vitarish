"use client";

import { useCallback, useState, type FormEvent } from "react";
import { Surface } from "@heroui/react";
import { useAccountPhoneChange } from "../hooks/useAccountPhoneChange";
import { localizeApiError, useLocalizedText } from "@vitarish/localization";
import { HeaderIcon, MobileNumberField, StatusAlert } from "@vitarish/ui";
import { ContactActionButton, ContactCodeStep } from "./AccountContactChange";

/** The status the API answers for a number it cannot read as a mobile number. */
const invalidPhoneNumberStatus = 400;

/** Change and verify the account's phone number: send a code to the new number, then enter it. */
export function AccountPhoneChange({ userId }: { userId: string }) {
  const { t } = useLocalizedText();
  const { profile, step, phoneNumber, code, isLoading, error, success, setPhoneNumber, setCode, setError, reset, beginPhoneChange, verifyPhoneChange } =
    useAccountPhoneChange(userId);
  const [phoneFieldError, setPhoneFieldError] = useState<string | undefined>();

  const sendCode = useCallback(
    async (event: FormEvent) => {
      event.preventDefault();
      try {
        await beginPhoneChange(phoneNumber);
      } catch (failure: unknown) {
        if (typeof failure === "object" && failure !== null && "status" in failure && failure.status === invalidPhoneNumberStatus) {
          setPhoneFieldError(t("auth.otp.invalidPhoneNumber"));
          setError(null);
          return;
        }
        setError(localizeApiError(failure, t));
      }
    },
    [beginPhoneChange, phoneNumber, setError, t],
  );

  const verifyCode = useCallback(
    async (event: FormEvent) => {
      event.preventDefault();
      try {
        await verifyPhoneChange(code);
      } catch (failure: unknown) {
        setError(localizeApiError(failure, t));
      }
    },
    [code, setError, t, verifyPhoneChange],
  );

  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex items-center gap-3">
        <HeaderIcon icon="phone" />
        <h2 className="text-lg font-semibold text-foreground">{t("account.phone.title")}</h2>
      </div>
      {profile?.phoneNumber ? (
        <p className="text-sm text-muted">
          {t("account.phone.currentValue", { value: "" })}
          <span dir="ltr">{profile.phoneNumber}</span>
        </p>
      ) : null}
      {error ? <StatusAlert message={error} /> : null}
      {success ? <StatusAlert message={t("account.phone.updateSucceeded")} type="success" /> : null}
      {step === "phone" ? (
        <form onSubmit={sendCode} className="flex flex-col gap-4">
          <MobileNumberField
            name="phone"
            label={t("account.phone.newPhone")}
            hint={t("auth.otp.phoneHint")}
            value={phoneNumber}
            onChange={(value) => {
              setPhoneNumber(value);
              setPhoneFieldError(undefined);
            }}
            error={phoneFieldError}
            isRequired
          />
          <ContactActionButton type="submit" icon="paper-plane" label={t("auth.otp.sendAction")} isPending={isLoading} />
        </form>
      ) : (
        <ContactCodeStep
          destination={phoneNumber}
          code={code}
          isLoading={isLoading}
          differentLabel={t("auth.otp.differentPhoneAction")}
          onCodeChange={setCode}
          onSubmit={verifyCode}
          onReset={reset}
        />
      )}
    </Surface>
  );
}
