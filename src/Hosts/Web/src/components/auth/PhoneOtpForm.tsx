"use client";

import { useState, useCallback } from "react";
import { TextField, Label, Input, Button, Form } from "@heroui/react";
import { usePhoneLogin } from "@/hooks/auth/usePhoneLogin";
import { StatusMessage } from "./StatusMessage";
import { OtpVerificationStep } from "./OtpVerificationStep";

interface PhoneOtpFormProps {
  returnUrl?: string;
}

export function PhoneOtpForm({ returnUrl }: PhoneOtpFormProps) {
  const {
    step,
    phoneNumber,
    begin,
    complete,
    resend,
    reset,
    isLoading,
    errorMessage,
    statusMessage,
    remainingAttempts,
    expiresAt,
  } = usePhoneLogin();
  const [phoneInput, setPhoneInput] = useState("");
  const [code, setCode] = useState("");

  const handlePhoneSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault();
      begin(phoneInput, returnUrl);
    },
    [phoneInput, returnUrl, begin]
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
        statusMessage={statusMessage}
        remainingAttempts={remainingAttempts}
        expiresAt={expiresAt}
        autoSubmitOnCapturedCode
        changeDestinationLabel="Use a different number"
      />
    );
  }

  return (
    <Form onSubmit={handlePhoneSubmit} className="flex flex-col gap-4">
      {errorMessage && <StatusMessage message={errorMessage} />}
      <TextField name="phone" isRequired isDisabled={isLoading}>
        <Label>Phone Number</Label>
        <Input
          placeholder="Enter your phone number"
          value={phoneInput}
          onChange={(e) => setPhoneInput(e.target.value)}
          autoComplete="tel"
          inputMode="tel"
        />
      </TextField>
      <Button
        type="submit"
        isPending={isLoading}
        className="w-full"
      >
        Send Code
      </Button>
    </Form>
  );
}
