"use client";

import { useState, useCallback } from "react";
import { TextField, Label, Input, Button, Form } from "@heroui/react";
import { useEmailLogin } from "@/hooks/auth/useEmailLogin";
import { StatusMessage } from "./StatusMessage";
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
    statusMessage,
    remainingAttempts,
    expiresAt,
  } = useEmailLogin();
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
        statusMessage={statusMessage}
        remainingAttempts={remainingAttempts}
        expiresAt={expiresAt}
        autoSubmitOnCapturedCode
        changeDestinationLabel="Use a different email"
      />
    );
  }

  return (
    <Form onSubmit={handleEmailSubmit} className="flex flex-col gap-4">
      {errorMessage && <StatusMessage message={errorMessage} />}
      <TextField name="email" type="email" isRequired isDisabled={isLoading}>
        <Label>Email</Label>
        <Input
          placeholder="Enter your email address"
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
        Send Code
      </Button>
    </Form>
  );
}
