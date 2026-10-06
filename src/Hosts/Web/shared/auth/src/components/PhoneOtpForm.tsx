"use client";

import { useState, useCallback } from "react";
import { Button, Form } from "@heroui/react";
import { usePhoneLogin } from "../hooks/usePhoneLogin";
import { useAuthErrorDisplay } from "../hooks/useAuthErrorDisplay";
import { useLocalizedText } from "@vitarish/localization";
import { MobileNumberField, StatusAlert } from "@vitarish/ui";
import type { AuthNavigationOptions } from "../hooks/navigation";
import { OtpVerificationStep } from "./OtpVerificationStep";

interface PhoneOtpFormProps extends AuthNavigationOptions {
  returnUrl?: string;
}

export function PhoneOtpForm({
  returnUrl,
  navigate,
  returnUrlSearchParams,
}: PhoneOtpFormProps) {
  const {
    step,
    phoneNumber,
    begin,
    complete,
    resend,
    reset,
    isLoading,
    error,
    errorMessage,
    statusCode,
    remainingAttempts,
    expiresAt,
  } = usePhoneLogin({ navigate, returnUrlSearchParams });
  const { t } = useLocalizedText();
  const displayedError = useAuthErrorDisplay(error, errorMessage);
  const [phoneInput, setPhoneInput] = useState("");
  const [code, setCode] = useState("");
  const invalidPhoneNumber = error?.status === 400;

  const handlePhoneSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault();
      begin(phoneInput, returnUrl);
    },
    [phoneInput, returnUrl, begin],
  );

  if (step === "code") {
    return (
      <OtpVerificationStep
        destinationLabel={phoneNumber}
        code={code}
        onCodeChange={setCode}
        onVerify={complete}
        onReset={reset}
        onResend={resend}
        isLoading={isLoading}
        errorMessage={displayedError}
        statusMessage={statusCode ? t(statusCode) : null}
        remainingAttempts={remainingAttempts}
        expiresAt={expiresAt}
        autoSubmitOnCapturedCode
        changeDestinationLabel={t("auth.otp.differentPhoneAction")}
      />
    );
  }

  return (
    <Form onSubmit={handlePhoneSubmit} className="flex flex-col gap-4">
      {displayedError && !invalidPhoneNumber && <StatusAlert className="mb-4" message={displayedError} />}
      <MobileNumberField
        name="phone"
        label={t("auth.otp.phoneLabel")}
        hint={t("auth.otp.phoneHint")}
        error={invalidPhoneNumber ? t("auth.otp.invalidPhoneNumber") : undefined}
        placeholder={t("auth.otp.phonePlaceholder")}
        value={phoneInput}
        onChange={setPhoneInput}
        isRequired
        isDisabled={isLoading}
      />
      <Button type="submit" isPending={isLoading} className="w-full">
        {t("auth.otp.sendAction")}
      </Button>
    </Form>
  );
}
