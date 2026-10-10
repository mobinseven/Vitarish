"use client";

import { Button, Form, Tooltip } from "@heroui/react";
import { useCallback, useEffect, useRef } from "react";
import { useOtpCodeCapture } from "../hooks/useOtpCodeCapture";
import { useOtpExpiry } from "../hooks/useOtpExpiry";
import { normalizeOtpCode } from "../auth/otpCode";
import {
  isAccountAccessError,
  isVerificationCodeError,
  type AuthenticationErrorCode,
} from "../auth/errors";
import { useLocalizedText } from "@vitarish/localization";
import { OtpCodeInput } from "./OtpCodeInput";
import { StatusAlert } from "@vitarish/ui";

interface OtpVerificationStepProps {
  destinationLabel: string;
  code: string;
  onCodeChange: (code: string) => void;
  onVerify: (code: string) => void;
  onReset: () => void;
  onResend: () => void;
  isLoading: boolean;
  errorMessage?: string | null;
  /** Decides where `errorMessage` appears: on the code field, as an alert, or as the end of the step. */
  errorCode?: AuthenticationErrorCode | null;
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
  errorCode,
  statusMessage,
  remainingAttempts,
  expiresAt,
  expectedLength = 6,
  autoSubmitOnCapturedCode = true,
  changeDestinationLabel,
}: OtpVerificationStepProps) {
  const { t } = useLocalizedText();
  const resetLabel =
    changeDestinationLabel ?? t("auth.otp.differentDestinationAction");
  const expiry = useOtpExpiry(expiresAt);
  const lastSubmittedCapturedCodeRef = useRef<string | null>(null);
  const normalizedCode = normalizeOtpCode(code, expectedLength);
  const canVerify =
    normalizedCode.length === expectedLength && !expiry.isExpired && !isLoading;
  const isAccountBlocked = Boolean(errorMessage) && isAccountAccessError(errorCode);
  const codeErrorMessage = isVerificationCodeError(errorCode) ? errorMessage : null;
  const alertMessage = codeErrorMessage ? null : errorMessage;

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
    [
      autoSubmitOnCapturedCode,
      expiry.isExpired,
      isLoading,
      onCodeChange,
      onVerify,
    ],
  );

  useOtpCodeCapture({
    enabled: !isLoading && !expiry.isExpired && !isAccountBlocked,
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
    [canVerify, normalizedCode, onVerify],
  );

  const showsAttempts = remainingAttempts !== undefined && remainingAttempts !== null;
  const showsExpiry = expiry.remainingSeconds !== null;

  if (isAccountBlocked) {
    return (
      <div className="flex flex-col gap-6">
        {errorMessage && <StatusAlert message={errorMessage} />}
        <StepAction
          icon="fa-light fa-pen-to-square"
          label={resetLabel}
          variant="secondary"
          onPress={onReset}
        />
      </div>
    );
  }

  return (
    <Form onSubmit={handleSubmit} className="flex flex-col gap-6">
      {alertMessage && <StatusAlert message={alertMessage} />}
      {statusMessage && !errorMessage && (
        <StatusAlert type="success" message={statusMessage} />
      )}
      <div className="flex flex-col gap-2">
        <p className="flex flex-col items-start gap-0.5 text-sm text-muted">
          {t("auth.otp.codeSentLabel")}{" "}
          <bdi dir="ltr" className="max-w-full truncate font-medium text-foreground">
            {destinationLabel}
          </bdi>
        </p>
        {(showsAttempts || showsExpiry) && (
          <p className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-muted">
            {showsAttempts && (
              <span className="inline-flex items-center gap-1.5">
                <i className="fa-light fa-shield-check" aria-hidden="true" />
                {t("auth.otp.attemptsRemaining", { count: remainingAttempts })}
              </span>
            )}
            {showsExpiry && (
              <span className="inline-flex items-center gap-1.5">
                <i className="fa-light fa-clock" aria-hidden="true" />
                {expiry.remaining === null
                  ? t("auth.challenge.expired")
                  : t("auth.challenge.expiresIn", { time: expiry.remaining })}
              </span>
            )}
          </p>
        )}
      </div>
      <OtpCodeInput
        value={normalizedCode}
        onChange={onCodeChange}
        isDisabled={isLoading || expiry.isExpired}
        isInvalid={Boolean(codeErrorMessage) || expiry.isExpired}
        errorMessage={
          expiry.isExpired
            ? t("auth.otp.codeExpired")
            : (codeErrorMessage ?? undefined)
        }
        expectedLength={expectedLength}
      />
      <div className="flex flex-col gap-3">
        <StepAction
          type="submit"
          icon="fa-light fa-check"
          label={t("auth.otp.verifyAction")}
          isPending={isLoading}
          isDisabled={!canVerify}
        />
        <StepAction
          icon="fa-light fa-rotate-right"
          label={t("auth.otp.resendAction")}
          variant="tertiary"
          isDisabled={isLoading}
          onPress={onResend}
        />
        <StepAction
          icon="fa-light fa-pen-to-square"
          label={resetLabel}
          variant="tertiary"
          isDisabled={isLoading}
          onPress={onReset}
        />
      </div>
    </Form>
  );
}

interface StepActionProps {
  icon: string;
  label: string;
  type?: "button" | "submit";
  variant?: "primary" | "secondary" | "tertiary";
  isPending?: boolean;
  isDisabled?: boolean;
  onPress?: () => void;
}

function StepAction({
  icon,
  label,
  type = "button",
  variant = "primary",
  isPending,
  isDisabled,
  onPress,
}: StepActionProps) {
  return (
    <Tooltip>
      <Button
        type={type}
        variant={variant}
        isPending={isPending}
        isDisabled={isDisabled}
        onPress={onPress}
        className="w-full"
      >
        <i className={icon} aria-hidden="true" />
        <span className="truncate">{label}</span>
      </Button>
      <Tooltip.Content>{label}</Tooltip.Content>
    </Tooltip>
  );
}
