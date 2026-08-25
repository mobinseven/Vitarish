"use client";

import { useState, useCallback } from "react";
import { TextField, Label, Input, Button, Form } from "@heroui/react";
import { usePhoneLogin } from "../hooks/usePhoneLogin";
import { useLocalizedText } from "@vitarish/localization";
import { StatusAlert } from "@vitarish/ui";
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
    errorMessage,
    statusCode,
    remainingAttempts,
    expiresAt,
  } = usePhoneLogin({ navigate, returnUrlSearchParams });
  const { t } = useLocalizedText();
  const [phoneInput, setPhoneInput] = useState("");
  const [code, setCode] = useState("");

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
        errorMessage={errorMessage}
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
      {errorMessage && <StatusAlert className="mb-4" message={errorMessage} />}
      <TextField name="phone" isRequired isDisabled={isLoading}>
        <Label>{t("auth.otp.phoneLabel")}</Label>
        <Input
          placeholder={t("auth.otp.phonePlaceholder")}
          value={phoneInput}
          onChange={(e) => setPhoneInput(e.target.value)}
          autoComplete="tel"
          inputMode="tel"
        />
      </TextField>
      <Button type="submit" isPending={isLoading} className="w-full">
        {t("auth.otp.sendAction")}
      </Button>
    </Form>
  );
}
