"use client";

import { Button, Form } from "@heroui/react";
import { useCallback, useEffect, useRef } from "react";
import { useOtpCodeCapture } from "@/hooks/auth/useOtpCodeCapture";
import { useOtpExpiry } from "@/hooks/auth/useOtpExpiry";
import { normalizeOtpCode } from "@/lib/auth/otpCode";
import { useLocalizedText } from "@/localization";
import { OtpCodeInput } from "./OtpCodeInput";
import { StatusMessage } from "./StatusMessage";

interface OtpVerificationStepProps {
  destinationLabel: string;
  code: string;
  onCodeChange: (code: string) => void;
  onVerify: (code: string) => void;
  onReset: () => void;
  onResend: () => void;
  isLoading: boolean;
  errorMessage?: string | null;
  statusMessage?: string | null;
  remainingAttempts?: number | string;
  expiresAt?: string | null;
  expectedLength?: number;
  autoSubmitOnCapturedCode?: boolean;
  changeDestinationLabel?: string;
}

export function OtpVerificationStep({
  destinationLabel,
  code,
  onCodeChange,
  onVerify,
  onReset,
  onResend,
  isLoading,
  errorMessage,
  statusMessage,
  remainingAttempts,
  expiresAt,
  expectedLength = 6,
  autoSubmitOnCapturedCode = true,
  changeDestinationLabel,
}: OtpVerificationStepProps) {
  const { t } = useLocalizedText();
  const resetLabel = changeDestinationLabel ?? t("auth.otp.differentDestinationAction");
  const expiry = useOtpExpiry(expiresAt);
  const lastSubmittedCapturedCodeRef = useRef<string | null>(null);
  const normalizedCode = normalizeOtpCode(code, expectedLength);
  const canVerify = normalizedCode.length === expectedLength && !expiry.isExpired && !isLoading;

  const handleCapturedCode = useCallback(
    (capturedCode: string) => {
      onCodeChange(capturedCode);

      if (!autoSubmitOnCapturedCode || expiry.isExpired || isLoading) {
        return;
      }

      if (lastSubmittedCapturedCodeRef.current === capturedCode) {
        return;
      }

      lastSubmittedCapturedCodeRef.current = capturedCode;
      onVerify(capturedCode);
    },
    [autoSubmitOnCapturedCode, expiry.isExpired, isLoading, onCodeChange, onVerify]
  );

  useOtpCodeCapture({
    enabled: !isLoading && !expiry.isExpired,
    expectedLength,
    onCode: handleCapturedCode,
  });

  useEffect(() => {
    lastSubmittedCapturedCodeRef.current = null;
  }, [destinationLabel, expiresAt]);

  const handleSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault();

      if (canVerify) {
        onVerify(normalizedCode);
      }
    },
    [canVerify, normalizedCode, onVerify]
  );

  return (
    <Form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {errorMessage && <StatusMessage message={errorMessage} />}
      {statusMessage && !errorMessage && <StatusMessage message={statusMessage} />}
      <p className="text-sm text-muted">
        {t("auth.otp.codeSentTo", { destination: destinationLabel })}
        {remainingAttempts !== undefined && remainingAttempts !== null && (
          <span className="block text-xs">
            {t("auth.otp.attemptsRemaining", { count: remainingAttempts })}
          </span>
        )}
        {expiry.label && (
          <span className="block text-xs">{expiry.label}</span>
        )}
      </p>
      <OtpCodeInput
        value={normalizedCode}
        onChange={onCodeChange}
        isDisabled={isLoading || expiry.isExpired}
        isInvalid={Boolean(errorMessage) || expiry.isExpired}
        errorMessage={expiry.isExpired ? t("auth.otp.codeExpired") : errorMessage ?? undefined}
        expectedLength={expectedLength}
      />
      <Button
        type="submit"
        isPending={isLoading}
        isDisabled={!canVerify}
        className="w-full"
      >
        {t("auth.otp.verifyAction")}
      </Button>
      <Button
        type="button"
        variant="tertiary"
        onPress={onResend}
        isDisabled={isLoading}
        className="w-full"
      >
        {t("auth.otp.resendAction")}
      </Button>
      <Button
        type="button"
        variant="tertiary"
        onPress={onReset}
        isDisabled={isLoading}
        className="w-full"
      >
        {resetLabel}
      </Button>
    </Form>
  );
}
