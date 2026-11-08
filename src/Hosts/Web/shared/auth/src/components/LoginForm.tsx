"use client";

import { useState, useCallback, useEffect } from "react";
import { TextField, Label, Input, Button, Form, Tooltip } from "@heroui/react";
import { usePasswordLogin } from "../hooks/usePasswordLogin";
import { useAuthErrorDisplay } from "../hooks/useAuthErrorDisplay";
import { useLocalizedText } from "@vitarish/localization";
import { getRequestedReturnUrl } from "../hooks/requestedReturnUrl";
import { AuthenticationSearchParams } from "../routes";
import { StatusAlert } from "@vitarish/ui";
import type { AuthNavigationOptions } from "../hooks/navigation";
import { OtpVerificationStep } from "./OtpVerificationStep";

interface LoginFormProps extends AuthNavigationOptions {
  returnUrl?: string;
  forgotPasswordHref?: string;
}

export function LoginForm({
  returnUrl,
  forgotPasswordHref,
  navigate,
  returnUrlSearchParams,
}: LoginFormProps) {
  const {
    login,
    completeOtp,
    resendOtp,
    resetOtp,
    challenge,
    requiresOtp,
    isLoading,
    error,
    errorMessage,
    statusCode,
  } = usePasswordLogin({
    navigate,
    returnUrlSearchParams,
  });
  const { t } = useLocalizedText();
  const displayedError = useAuthErrorDisplay(error, errorMessage);
  const [userNameOrEmail, setUserNameOrEmail] = useState("");
  const [password, setPassword] = useState("");
  const [otpCode, setOtpCode] = useState("");
  const [isHydrated, setIsHydrated] = useState(false);

  useEffect(() => {
    setIsHydrated(true);
  }, []);

  const handleSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault();
      login(userNameOrEmail, password, returnUrl);
    },
    [userNameOrEmail, password, returnUrl, login],
  );

  if (requiresOtp && challenge) {
    return (
      <OtpVerificationStep
        destinationLabel={
          challenge.maskedDestination ?? t("auth.passwordLoginOtp.destinationFallback")
        }
        code={otpCode}
        onCodeChange={setOtpCode}
        onVerify={completeOtp}
        onReset={() => {
          setOtpCode("");
          resetOtp();
        }}
        onResend={resendOtp}
        isLoading={isLoading}
        errorMessage={displayedError}
        errorCode={error?.code}
        statusMessage={statusCode ? t(statusCode) : null}
        remainingAttempts={challenge.remainingAttempts}
        expiresAt={challenge.expiresAt}
        autoSubmitOnCapturedCode
        changeDestinationLabel={t("auth.passwordLoginOtp.returnToPasswordAction")}
        codeLabel={t("auth.login.otpLabel")}
        verifyActionLabel={t("auth.login.otpVerifyAction")}
        stepDescription={
          challenge.usesEmailSecondFactor
            ? t("auth.passwordLoginOtp.emailStepDescription")
            : undefined
        }
        captureCode={!challenge.usesEmailSecondFactor}
      />
    );
  }

  const requestedReturn = returnUrl ?? getRequestedReturnUrl(returnUrlSearchParams);
  const recoveryParams = new URLSearchParams();
  if (requestedReturn) recoveryParams.set(AuthenticationSearchParams.ReturnUrl, requestedReturn);
  const recoveryHref = forgotPasswordHref && requestedReturn
    ? `${forgotPasswordHref}?${recoveryParams}` : forgotPasswordHref;

  return (
    <Form method="post" onSubmit={handleSubmit} className="flex flex-col gap-4">
      {displayedError && <StatusAlert className="mb-4" message={displayedError} />}
      <TextField name="username" isRequired isDisabled={isLoading}>
        <Label>{t("auth.login.usernameOrEmailLabel")}</Label>
        <Input
          placeholder={t("auth.login.usernameOrEmailPlaceholder")}
          value={userNameOrEmail}
          onChange={(e) => setUserNameOrEmail(e.target.value)}
          autoComplete="username"
        />
      </TextField>
      <TextField
        name="password"
        type="password"
        isRequired
        isDisabled={isLoading}
      >
        <Label>{t("auth.login.passwordLabel")}</Label>
        <Input
          placeholder={t("auth.login.passwordPlaceholder")}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          autoComplete="current-password"
        />
      </TextField>
      <Tooltip>
        <Button type="submit" isDisabled={!isHydrated || isLoading} isPending={isLoading} className="w-full">
          <i aria-hidden="true" className="fa-light fa-right-to-bracket" />
          <span>{t("auth.login.signInAction")}</span>
        </Button>
        <Tooltip.Content>{t("auth.login.signInAction")}</Tooltip.Content>
      </Tooltip>
      {forgotPasswordHref && (
        <a className="self-end text-sm text-accent underline-offset-4 hover:underline" href={recoveryHref}>
          {t("auth.recovery.forgotPasswordAction")}
        </a>
      )}
    </Form>
  );
}
