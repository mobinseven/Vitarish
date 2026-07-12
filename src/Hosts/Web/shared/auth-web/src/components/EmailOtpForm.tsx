"use client";

import { useState, useCallback } from "react";
import { TextField, Label, Input, Button, Form } from "@heroui/react";
import { useEmailLogin } from "../hooks/useEmailLogin";
import { useLocalizedText } from "@vitarish/web-localization";
import { StatusAlert } from "@vitarish/web-ui";
import { OtpVerificationStep } from "./OtpVerificationStep";

interface EmailOtpFormProps {
  returnUrl?: string;
}

export function EmailOtpForm({ returnUrl }: EmailOtpFormProps) {
  const {
    step,
    email,
    begin,
    complete,
    resend,
    reset,
    isLoading,
    errorMessage,
    statusCode,
    remainingAttempts,
    expiresAt,
  } = useEmailLogin();
  const { t } = useLocalizedText();
  const [emailInput, setEmailInput] = useState("");
  const [code, setCode] = useState("");

  const handleEmailSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault();
      begin(emailInput, returnUrl);
    },
    [emailInput, returnUrl, begin]
  );

  if (step === "code") {
    return (
      <OtpVerificationStep
        destinationLabel={email}
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
        changeDestinationLabel={t("auth.otp.differentEmailAction")}
      />
    );
  }

  return (
    <Form onSubmit={handleEmailSubmit} className="flex flex-col gap-4">
      {errorMessage && <StatusAlert className="mb-4" message={errorMessage} />}
      <TextField name="email" type="email" isRequired isDisabled={isLoading}>
        <Label>{t("auth.otp.emailLabel")}</Label>
        <Input
          placeholder={t("auth.otp.emailPlaceholder")}
          value={emailInput}
          onChange={(e) => setEmailInput(e.target.value)}
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
    </Form>
  );
}
