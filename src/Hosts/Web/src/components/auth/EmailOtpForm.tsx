"use client";

import { useState, useCallback } from "react";
import { Input, Button, Form } from "@heroui/react";
import { useEmailLogin } from "@/hooks/auth/useEmailLogin";
import { StatusMessage } from "./StatusMessage";
import { OtpCodeInput } from "./OtpCodeInput";

interface EmailOtpFormProps {
  returnUrl?: string;
}

export function EmailOtpForm({ returnUrl }: EmailOtpFormProps) {
  const {
    step,
    email,
    begin,
    complete,
    reset,
    isLoading,
    errorMessage,
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

  const handleCodeSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault();
      complete(code);
    },
    [code, complete]
  );

  if (step === "code") {
    return (
      <Form onSubmit={handleCodeSubmit} className="flex flex-col gap-4">
        {errorMessage && <StatusMessage message={errorMessage} />}
        <p className="text-sm text-default-500">
          Code sent to {email}
          {remainingAttempts !== undefined && (
            <span className="block text-xs">
              {remainingAttempts} attempts remaining
            </span>
          )}
          {expiresAt && (
            <span className="block text-xs">
              Expires at {new Date(expiresAt).toLocaleTimeString()}
            </span>
          )}
        </p>
        <OtpCodeInput
          value={code}
          onChange={setCode}
          isDisabled={isLoading}
        />
        <Button
          type="submit"
          color="primary"
          isLoading={isLoading}
          isDisabled={code.length !== 6}
          className="w-full"
        >
          Verify Code
        </Button>
        <Button
          variant="flat"
          onPress={reset}
          isDisabled={isLoading}
          className="w-full"
        >
          Use a different email
        </Button>
      </Form>
    );
  }

  return (
    <Form onSubmit={handleEmailSubmit} className="flex flex-col gap-4">
      {errorMessage && <StatusMessage message={errorMessage} />}
      <Input
        label="Email"
        placeholder="Enter your email address"
        type="email"
        value={emailInput}
        onValueChange={setEmailInput}
        isRequired
        autoComplete="email"
        isDisabled={isLoading}
      />
      <Button
        type="submit"
        color="primary"
        isLoading={isLoading}
        className="w-full"
      >
        Send Code
      </Button>
    </Form>
  );
}
