"use client";

import { useState, useCallback } from "react";
import { Input, Button, Form } from "@heroui/react";
import { usePhoneLogin } from "@/hooks/auth/usePhoneLogin";
import { StatusMessage } from "./StatusMessage";
import { OtpCodeInput } from "./OtpCodeInput";

interface PhoneOtpFormProps {
  returnUrl?: string;
}

export function PhoneOtpForm({ returnUrl }: PhoneOtpFormProps) {
  const {
    step,
    phoneNumber,
    begin,
    complete,
    reset,
    isLoading,
    errorMessage,
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
          Code sent to {phoneNumber}
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
          Use a different number
        </Button>
      </Form>
    );
  }

  return (
    <Form onSubmit={handlePhoneSubmit} className="flex flex-col gap-4">
      {errorMessage && <StatusMessage message={errorMessage} />}
      <Input
        label="Phone Number"
        placeholder="Enter your phone number"
        value={phoneInput}
        onValueChange={setPhoneInput}
        isRequired
        autoComplete="tel"
        inputMode="tel"
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
