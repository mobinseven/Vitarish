"use client";

import { useCallback, useState } from "react";
import { Button } from "@heroui/react";
import { useAuthSession } from "@vitarish/auth";
import {
  AccountBackLink,
  AccountSurface,
  AccountWidget,
} from "@/components/account/AccountSurface";
import { useAccountPhoneChange } from "@vitarish/account";
import { MobileNumberField, StatusAlert } from "@vitarish/ui";
import { OtpCodeInput } from "@vitarish/auth";
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
  const [phoneFieldError, setPhoneFieldError] = useState<string | undefined>();

  const handlePhoneSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      try {
        await beginPhoneChange(phoneNumber);
      } catch (err: unknown) {
        if (typeof err === "object" && err !== null && "status" in err && err.status === 400) {
          setPhoneFieldError(t("auth.otp.invalidPhoneNumber"));
          setError(null);
          return;
        }
        setError(localizeApiError(err, t));
      }
    },
    [phoneNumber, beginPhoneChange, setError, t],
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
    [code, verifyPhoneChange, setError, t],
  );

  return (
    <AccountSurface>
      <AccountWidget title={t("account.phone.title")}>
        {profile?.phoneNumber && (
          <p className="text-sm text-muted">
            {t("account.phone.currentValue", { value: "" })}
            <span dir="ltr">{profile.phoneNumber}</span>
          </p>
        )}
        {error && <StatusAlert className="mb-4" message={error} />}
        {success && (
          <StatusAlert
            className="mb-4"
            message={t("account.phone.updateSucceeded")}
            type="success"
          />
        )}

        {step === "phone" ? (
          <form onSubmit={handlePhoneSubmit} className="flex flex-col gap-4">
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
            <Button type="submit" isPending={isLoading} className="w-full">
              {t("auth.otp.sendAction")}
            </Button>
          </form>
        ) : (
          <form onSubmit={handleCodeSubmit} className="flex flex-col gap-4">
            <p className="text-sm text-muted">
              {t("auth.otp.codeSentToPrefix")}
              <span dir="ltr">{phoneNumber}</span>
              {t("auth.otp.codeSentToSuffix")}
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

        <AccountBackLink />
      </AccountWidget>
    </AccountSurface>
  );
}
